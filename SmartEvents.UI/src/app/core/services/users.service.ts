import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ProfileResponse, UpdateProfileRequest } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getProfile() {
    return this.http.get<ProfileResponse>(`${this.apiUrl}/me`);
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.http.put<ProfileResponse>(`${this.apiUrl}/me`, request);
  }

  requestVerification() {
    return this.http.post(`${environment.apiUrl}/auth/request-verification`, {});
  }

  verifyEmail(token: string) {
    return this.http.get(`${environment.apiUrl}/auth/verify-email`, { params: { token } });
  }
}
