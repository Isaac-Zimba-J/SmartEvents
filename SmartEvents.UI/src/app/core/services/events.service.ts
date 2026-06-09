import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateEventRequest, EventCategory, EventDetail, EventSummary, PagedResult } from '../models/event.models';

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly apiUrl = `${environment.apiUrl}/events`;

  constructor(private http: HttpClient) {}

  getAll(params?: {
    page?: number;
    pageSize?: number;
    category?: EventCategory;
    search?: string;
    companyId?: string;
  }) {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.category) httpParams = httpParams.set('category', params.category);
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.companyId) httpParams = httpParams.set('companyId', params.companyId);

    return this.http.get<PagedResult<EventSummary>>(this.apiUrl, { params: httpParams });
  }

  getBySlug(slug: string) {
    return this.http.get<EventDetail>(`${this.apiUrl}/${slug}`);
  }

  getByCompany(companyId: string) {
    return this.http.get<EventSummary[]>(`${this.apiUrl}/company/${companyId}`);
  }

  create(request: CreateEventRequest) {
    return this.http.post<EventSummary>(this.apiUrl, request);
  }

  update(id: string, request: Partial<CreateEventRequest> & { status?: string }) {
    return this.http.put<EventSummary>(`${this.apiUrl}/${id}`, request);
  }

  publish(id: string) {
    return this.http.patch(`${this.apiUrl}/${id}/publish`, {});
  }

  cancel(id: string) {
    return this.http.patch(`${this.apiUrl}/${id}/cancel`, {});
  }

  delete(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getRecommended(excludeEventId: string) {
    return this.http.get<EventSummary[]>(
      `${this.apiUrl}/recommended`,
      { params: new HttpParams().set('excludeEventId', excludeEventId) }
    );
  }
}
