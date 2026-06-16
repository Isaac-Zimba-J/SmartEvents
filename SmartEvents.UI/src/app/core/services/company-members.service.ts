import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CompanyMember {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  createdAt: string;
}

export interface AddMemberRequest {
  email: string;
  role: string;
}

export interface UpdateMemberRoleRequest {
  role: string;
}

@Injectable({ providedIn: 'root' })
export class CompanyMembersService {
  private apiUrl = `${environment.apiUrl}/companies`;

  constructor(private http: HttpClient) {}

  getMembers(companyId: string): Observable<CompanyMember[]> {
    return this.http.get<CompanyMember[]>(`${this.apiUrl}/${companyId}/members`);
  }

  addMember(companyId: string, request: AddMemberRequest): Observable<CompanyMember> {
    return this.http.post<CompanyMember>(`${this.apiUrl}/${companyId}/members`, request);
  }

  updateRole(companyId: string, userId: string, request: UpdateMemberRoleRequest): Observable<CompanyMember> {
    return this.http.put<CompanyMember>(`${this.apiUrl}/${companyId}/members/${userId}`, request);
  }

  removeMember(companyId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${companyId}/members/${userId}`);
  }
}
