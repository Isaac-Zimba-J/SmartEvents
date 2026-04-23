import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateRegistrationRequest, RegistrationResponse } from '../models/registration.models';

@Injectable({ providedIn: 'root' })
export class RegistrationsService {
  private readonly apiUrl = `${environment.apiUrl}/registrations`;

  constructor(private http: HttpClient) {}

  register(request: CreateRegistrationRequest) {
    return this.http.post<RegistrationResponse>(this.apiUrl, request);
  }

  getById(id: string) {
    return this.http.get<RegistrationResponse>(`${this.apiUrl}/${id}`);
  }

  getMyRegistrations() {
    return this.http.get<RegistrationResponse[]>(`${this.apiUrl}/my`);
  }

  getByEvent(eventId: string) {
    return this.http.get<RegistrationResponse[]>(`${this.apiUrl}/event/${eventId}`);
  }

  cancel(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  checkIn(ticketNumber: string) {
    return this.http.post(`${this.apiUrl}/check-in`, { ticketNumber });
  }
}
