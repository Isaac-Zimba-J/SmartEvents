import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationResponse } from '../../../core/models/registration.models';

@Component({
  selector: 'app-my-registrations',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-registrations.component.html'
})
export class MyRegistrationsComponent implements OnInit {
  registrations: RegistrationResponse[] = [];
  loading = true;
  cancelling: string | null = null;
  expandedQr: string | null = null;

  constructor(
    private registrationsService: RegistrationsService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.registrationsService.getMyRegistrations().subscribe({
      next: data => { this.registrations = data; this.loading = false; },
      error: () => {
        this.toast.error('Failed to load registrations.');
        this.loading = false;
      }
    });
  }

  cancel(id: string): void {
    this.cancelling = id;
    this.registrationsService.cancel(id).subscribe({
      next: () => {
        this.registrations = this.registrations.map(r =>
          r.id === id ? { ...r, status: 'Cancelled' as const } : r
        );
        this.cancelling = null;
        this.toast.success('Registration cancelled.');
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Could not cancel registration.');
        this.cancelling = null;
      }
    });
  }

  toggleQr(regId: string): void {
    this.expandedQr = this.expandedQr === regId ? null : regId;
  }

  canCancel(reg: RegistrationResponse): boolean {
    return reg.status === 'Confirmed' || reg.status === 'Waitlisted' || reg.status === 'Pending';
  }

  qrSrc(base64: string): string {
    return `data:image/png;base64,${base64}`;
  }
}
