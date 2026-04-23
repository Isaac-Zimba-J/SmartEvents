import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationResponse } from '../../../core/models/registration.models';

@Component({
  selector: 'app-attendees',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './attendees.component.html'
})
export class AttendeesComponent implements OnInit {
  registrations: RegistrationResponse[] = [];
  loading = true;
  eventId = '';

  constructor(
    private route: ActivatedRoute,
    private registrationsService: RegistrationsService,
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
  }

  get confirmed(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Confirmed'); }
  get checkedIn(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'CheckedIn'); }
  get waitlisted(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Waitlisted'); }
  get cancelled(): RegistrationResponse[] { return this.registrations.filter(r => r.status === 'Cancelled'); }
}
