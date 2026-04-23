import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { EventAnalytics, OverviewAnalytics } from '../../../core/models/analytics.models';

@Component({
  selector: 'app-analytics-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './analytics-dashboard.component.html',
  styleUrl: './analytics-dashboard.component.scss'
})
export class AnalyticsDashboardComponent implements OnInit {
  overview: OverviewAnalytics | null = null;
  eventStats: EventAnalytics[] = [];
  loadingOverview = true;
  loadingEvents = true;

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.analyticsService.getOverview().subscribe({
      next: data => { this.overview = data; this.loadingOverview = false; },
      error: () => { this.loadingOverview = false; }
    });

    this.analyticsService.getEventStats().subscribe({
      next: data => { this.eventStats = data; this.loadingEvents = false; },
      error: () => { this.loadingEvents = false; }
    });
  }
}
