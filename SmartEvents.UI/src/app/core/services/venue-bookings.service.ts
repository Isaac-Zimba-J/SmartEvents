import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VenueBooking, CreateVenueBookingRequest } from '../models/venue.models';

@Injectable({ providedIn: 'root' })
export class VenueBookingsService {
  private apiUrl = `${environment.apiUrl}/venue-bookings`;

  constructor(private http: HttpClient) {}

  create(request: CreateVenueBookingRequest): Observable<VenueBooking> {
    return this.http.post<VenueBooking>(this.apiUrl, request);
  }

  getMy(): Observable<VenueBooking[]> {
    return this.http.get<VenueBooking[]>(`${this.apiUrl}/my`);
  }

  getById(id: string): Observable<VenueBooking> {
    return this.http.get<VenueBooking>(`${this.apiUrl}/${id}`);
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
