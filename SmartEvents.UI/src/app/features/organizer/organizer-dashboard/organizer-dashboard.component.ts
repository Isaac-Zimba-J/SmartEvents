import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EventsService } from '../../../core/services/events.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventSummary } from '../../../core/models/event.models';

@Component({
  selector: 'app-organizer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './organizer-dashboard.component.html'
})
export class OrganizerDashboardComponent implements OnInit {
  events: EventSummary[] = [];
  loading = true;
  acting: string | null = null;

  constructor(
    private eventsService: EventsService,
    public auth: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.eventsService.getManaged().subscribe({
      next: data => { this.events = data; this.loading = false; },
      error: () => {
        this.toast.error('Failed to load events.');
        this.loading = false;
      }
    });
  }

  publish(event: EventSummary): void {
    this.acting = event.id;
    this.eventsService.publish(event.id).subscribe({
      next: () => {
        event.status = 'Published';
        this.acting = null;
        this.toast.success(`"${event.title}" published.`);
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to publish event.');
        this.acting = null;
      }
    });
  }

  cancel(event: EventSummary): void {
    if (!confirm(`Cancel "${event.title}"? This will notify all registered attendees.`)) return;
    this.acting = event.id;
    this.eventsService.cancel(event.id).subscribe({
      next: () => {
        event.status = 'Cancelled';
        this.acting = null;
        this.toast.success(`"${event.title}" cancelled.`);
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to cancel event.');
        this.acting = null;
      }
    });
  }

  spotsLeft(event: EventSummary): number {
    return event.maxAttendees - event.registeredCount;
  }

  get draftEvents(): EventSummary[] { return this.events.filter(e => e.status === 'Draft'); }
  get publishedEvents(): EventSummary[] { return this.events.filter(e => e.status === 'Published'); }
  get otherEvents(): EventSummary[] { return this.events.filter(e => e.status !== 'Draft' && e.status !== 'Published'); }
}
