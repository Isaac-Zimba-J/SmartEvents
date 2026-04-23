import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { EventAnalytics, OverviewAnalytics } from '../models/analytics.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getOverview() {
    return this.http.get<OverviewAnalytics>(`${this.apiUrl}/overview`);
  }

  getEventStats() {
    return this.http.get<EventAnalytics[]>(`${this.apiUrl}/events`);
  }
}
