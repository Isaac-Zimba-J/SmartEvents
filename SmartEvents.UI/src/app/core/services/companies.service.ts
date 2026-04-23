import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../models/company.models';

@Injectable({ providedIn: 'root' })
export class CompaniesService {
  private readonly apiUrl = `${environment.apiUrl}/companies`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<Company[]>(this.apiUrl);
  }

  getById(id: string) {
    return this.http.get<Company>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateCompanyRequest) {
    return this.http.post<Company>(this.apiUrl, request);
  }

  update(id: string, request: UpdateCompanyRequest) {
    return this.http.put<Company>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
