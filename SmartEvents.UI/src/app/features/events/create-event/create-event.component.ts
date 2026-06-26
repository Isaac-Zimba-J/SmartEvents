import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EventsService } from '../../../core/services/events.service';
import { VenuesService } from '../../../core/services/venues.service';
import { CompaniesService } from '../../../core/services/companies.service';
import { AuthService } from '../../../core/services/auth.service';
import { Venue } from '../../../core/models/venue.models';
import { Company } from '../../../core/models/company.models';
import { EventCategory } from '../../../core/models/event.models';

@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-event.component.html'
})
export class CreateEventComponent implements OnInit {
  form: FormGroup;
  loading = false;
  error = '';
  venues: Venue[] = [];
  companies: Company[] = [];

  readonly categories: EventCategory[] = [
    'Conference', 'Workshop', 'Concert', 'Exhibition',
    'Sports', 'Networking', 'Webinar', 'Other'
  ];

  constructor(
    private fb: FormBuilder,
    private eventsService: EventsService,
    private venuesService: VenuesService,
    private companiesService: CompaniesService,
    public auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(300)]],
      description: [''],
      imageUrl: [''],
      category: ['Conference', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      timezone: ['UTC'],
      maxAttendees: [100, [Validators.required, Validators.min(1)]],
      isTicketed: [false],
      ticketPrice: [0, Validators.min(0)],
      waitlistEnabled: [true],
      isPublic: [true],
      tags: [''],
      venueId: [''],
      companyId: ['']
    });
  }

  get isSuperAdmin(): boolean {
    return this.auth.currentUser()?.role === 'SuperAdmin';
  }

  ngOnInit(): void {
    this.venuesService.getAll().subscribe({
      next: venues => { this.venues = venues; },
      error: () => {}
    });

    if (this.isSuperAdmin) {
      this.companiesService.getAll().subscribe({
        next: companies => { this.companies = companies; },
        error: () => {}
      });
    }
  }

  get isTicketed(): boolean {
    return this.form.get('isTicketed')?.value === true;
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const value = this.form.value;
    const request = {
      ...value,
      ticketPrice: this.isTicketed ? value.ticketPrice : 0,
      venueId: value.venueId || undefined,
      companyId: value.companyId || undefined
    };

    this.eventsService.create(request).subscribe({
      next: event => this.router.navigate(['/events', event.slug]),
      error: err => {
        this.error = err.error?.message ?? 'Failed to create event.';
        this.loading = false;
      }
    });
  }
}
