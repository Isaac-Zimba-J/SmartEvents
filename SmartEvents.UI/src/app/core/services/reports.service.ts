import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ReportFormat } from '../models/report.models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  downloadEventAttendees(eventId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/events/${eventId}/attendees`, format);
  }

  downloadEventSales(eventId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/events/${eventId}/sales`, format);
  }

  downloadVenueBookings(venueId: string, format: ReportFormat): Observable<void> {
    return this.download(`${this.apiUrl}/venues/${venueId}/bookings`, format);
  }

  private download(url: string, format: ReportFormat): Observable<void> {
    return this.http
      .get(url, { params: { format }, responseType: 'blob', observe: 'response' })
      .pipe(
        map(res => this.saveBlob(res, format)),
        catchError(err => this.normalizeBlobError(err))
      );
  }

  private saveBlob(res: HttpResponse<Blob>, format: ReportFormat): void {
    // ASP.NET sends: attachment; filename=x.pdf; filename*=UTF-8''x.pdf
    const disposition = res.headers.get('Content-Disposition') ?? '';
    const match = /filename="?([^";]+)"?/i.exec(disposition);
    const fileName = match?.[1] ?? `report.${format}`;

    const objectUrl = URL.createObjectURL(res.body!);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(objectUrl);
  }

  // Error bodies arrive as a Blob because we asked for one; turn them back into { message }
  // so callers can keep using err.error?.message like everywhere else in the app.
  private normalizeBlobError(err: HttpErrorResponse): Observable<never> {
    if (!(err.error instanceof Blob)) return throwError(() => err);

    return from(err.error.text()).pipe(
      switchMap(text => {
        let message = 'Report download failed.';
        try { message = JSON.parse(text)?.message ?? message; } catch { /* not JSON */ }
        return throwError(() => new HttpErrorResponse({
          error: { message },
          status: err.status,
          statusText: err.statusText,
          url: err.url ?? undefined
        }));
      })
    );
  }
}
