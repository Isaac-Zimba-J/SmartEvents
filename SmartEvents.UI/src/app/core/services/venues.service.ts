import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateVenueRequest, Venue, VenueType } from '../models/venue.models';

@Injectable({ providedIn: 'root' })
export class VenuesService {
  private readonly apiUrl = `${environment.apiUrl}/venues`;

  constructor(private http: HttpClient) {}

  getAll(params?: { city?: string; country?: string; type?: VenueType; minCapacity?: number; companyId?: string }) {
    let httpParams = new HttpParams();
    if (params?.city) httpParams = httpParams.set('city', params.city);
    if (params?.country) httpParams = httpParams.set('country', params.country);
    if (params?.type) httpParams = httpParams.set('type', params.type);
    if (params?.minCapacity) httpParams = httpParams.set('minCapacity', params.minCapacity);
    if (params?.companyId) httpParams = httpParams.set('companyId', params.companyId);

    return this.http.get<Venue[]>(this.apiUrl, { params: httpParams });
  }

  getById(id: string) {
    return this.http.get<Venue>(`${this.apiUrl}/${id}`);
  }

  getByCompany(companyId: string) {
    return this.http.get<Venue[]>(`${this.apiUrl}/company/${companyId}`);
  }

  create(request: CreateVenueRequest) {
    return this.http.post<Venue>(this.apiUrl, request);
  }

  update(id: string, request: Partial<CreateVenueRequest> & { isAvailable?: boolean }) {
    return this.http.put<Venue>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
