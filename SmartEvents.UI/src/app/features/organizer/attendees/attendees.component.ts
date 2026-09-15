import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { EventsService } from '../../../core/services/events.service';
import { ReportsService } from '../../../core/services/reports.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationResponse } from '../../../core/models/registration.models';
import { EventSummary } from '../../../core/models/event.models';
import { ReportFormat } from '../../../core/models/report.models';
import { ReportDownloadComponent } from '../../../core/components/report-download/report-download.component';

@Component({
  selector: 'app-attendees',
  standalone: true,
  imports: [CommonModule, RouterLink, ReportDownloadComponent],
  templateUrl: './attendees.component.html'
})
export class AttendeesComponent implements OnInit {
  registrations: RegistrationResponse[] = [];
  event: EventSummary | null = null;
  loading = true;
  eventId = '';

  constructor(
    private route: ActivatedRoute,
    private registrationsService: RegistrationsService,
    private eventsService: EventsService,
    private reportsService: ReportsService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('eventId')!;
    this.registrationsService.getByEvent(this.eventId).subscribe({
      next: data => { this.registrations = data; this.loading = false; },
      error: () => {
        this.toast.error('Failed to load attendees.');
        this.loading = false;
      }
    });
    // Only needed to know whether the event is ticketed (sales report button).
    this.eventsService.getManaged().subscribe({
      next: events => { this.event = events.find(e => e.id === this.eventId) ?? null; },
      error: () => {}
    });
  }

  // Arrow properties so `this` survives being passed as an input.
  downloadAttendees = (format: ReportFormat) => this.reportsService.downloadEventAttendees(this.eventId, format);
  downloadSales = (format: ReportFormat) => this.reportsService.downloadEventSales(this.eventId, format);

  get showSalesReport(): boolean { return this.event === null || this.event.isTicketed; }

  get confirmed(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Confirmed'); }
  get checkedIn(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'CheckedIn'); }
  get waitlisted(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Waitlisted'); }
  get cancelled(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Cancelled'); }
}
