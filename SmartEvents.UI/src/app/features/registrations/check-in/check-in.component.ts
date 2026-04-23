import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { ToastService } from '../../../core/services/toast.service';

interface CheckInResult {
  ticketNumber: string;
  success: boolean;
  message: string;
}

@Component({
  selector: 'app-check-in',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './check-in.component.html'
})
export class CheckInComponent {
  ticketNumber = '';
  loading = false;
  lastResult: CheckInResult | null = null;
  history: CheckInResult[] = [];

  constructor(
    private registrationsService: RegistrationsService,
    private toast: ToastService
  ) {}

  submit(): void {
    const ticket = this.ticketNumber.trim().toUpperCase();
    if (!ticket) return;

    this.loading = true;
    this.lastResult = null;

    this.registrationsService.checkIn(ticket).subscribe({
      next: () => {
        const result: CheckInResult = { ticketNumber: ticket, success: true, message: 'Check-in successful!' };
        this.lastResult = result;
        this.history.unshift(result);
        this.toast.success(`${ticket} checked in.`);
        this.ticketNumber = '';
        this.loading = false;
      },
      error: err => {
        const message = err.error?.message ?? 'Check-in failed.';
        const result: CheckInResult = { ticketNumber: ticket, success: false, message };
        this.lastResult = result;
        this.history.unshift(result);
        this.toast.error(message);
        this.loading = false;
      }
    });
  }

  clearHistory(): void {
    this.history = [];
    this.lastResult = null;
  }
}
