import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EventsService } from '../../../core/services/events.service';
import { VenuesService } from '../../../core/services/venues.service';
import { Venue } from '../../../core/models/venue.models';
import { EventCategory, EventDetail, EventStatus } from '../../../core/models/event.models';

@Component({
  selector: 'app-edit-event',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './edit-event.component.html'
})
export class EditEventComponent implements OnInit {
  form: FormGroup;
  loading = true;
  saving = false;
  error = '';
  event: EventDetail | null = null;
  venues: Venue[] = [];

  readonly categories: EventCategory[] = [
    'Conference', 'Workshop', 'Concert', 'Exhibition',
    'Sports', 'Networking', 'Webinar', 'Other'
  ];

  readonly statuses: EventStatus[] = ['Draft', 'Published', 'Cancelled', 'Completed'];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private eventsService: EventsService,
    private venuesService: VenuesService,
    private router: Router
  ) {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(300)]],
      description: [''],
      imageUrl: [''],
      category: ['Conference', Validators.required],
      status: ['Draft', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      timezone: ['UTC'],
      maxAttendees: [100, [Validators.required, Validators.min(1)]],
      isTicketed: [false],
      ticketPrice: [0, Validators.min(0)],
      waitlistEnabled: [true],
      isPublic: [true],
      tags: [''],
      venueId: ['']
    });
  }

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;

    this.venuesService.getAll().subscribe({ next: v => { this.venues = v; }, error: () => {} });

    this.eventsService.getBySlug(slug).subscribe({
      next: event => {
        this.event = event;
        this.form.patchValue({
          title: event.title,
          description: event.description ?? '',
          imageUrl: event.imageUrl ?? '',
          category: event.category,
          status: event.status,
          startDate: this.toLocalDatetimeString(event.startDate),
          endDate: this.toLocalDatetimeString(event.endDate),
          timezone: event.timezone ?? 'UTC',
          maxAttendees: event.maxAttendees,
          isTicketed: event.isTicketed,
          ticketPrice: event.ticketPrice,
          waitlistEnabled: event.waitlistEnabled,
          isPublic: event.isPublic,
          tags: event.tags ?? '',
          venueId: event.venue?.id ?? ''
        });
        this.loading = false;
      },
      error: () => {
        this.error = 'Event not found.';
        this.loading = false;
      }
    });
  }

  get isTicketed(): boolean {
    return this.form.get('isTicketed')?.value === true;
  }

  submit(): void {
    if (this.form.invalid || !this.event) return;
    this.saving = true;
    this.error = '';

    const value = this.form.value;
    const request = {
      ...value,
      ticketPrice: this.isTicketed ? value.ticketPrice : 0,
      venueId: value.venueId || undefined
    };

    this.eventsService.update(this.event.id, request).subscribe({
      next: updated => this.router.navigate(['/events', updated.slug]),
      error: err => {
        this.error = err.error?.message ?? 'Failed to update event.';
        this.saving = false;
      }
    });
  }

  private toLocalDatetimeString(iso: string): string {
    const d = new Date(iso);
    return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);
  }
}
