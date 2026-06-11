import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { EventsService } from '../../../core/services/events.service';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { PaymentsService } from '../../../core/services/payments.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventDetail, EventSummary } from '../../../core/models/event.models';
import { RegistrationResponse } from '../../../core/models/registration.models';
import { PaymentMethod } from '../../../core/models/payment.models';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LucideAngularModule],
  templateUrl: './event-detail.component.html'
})
export class EventDetailComponent implements OnInit {
  event: EventDetail | null = null;
  loading = true;
  acting = false;
  registration: RegistrationResponse | null = null;
  error = '';
  successMessage = '';
  recommendations: EventSummary[] = [];

  showCheckout = false;
  selectedPaymentMethod: PaymentMethod = 'Stripe';

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
    { value: 'Stripe', label: 'Credit / Debit Card' },
    { value: 'AirtelMoney', label: 'Airtel Money' },
    { value: 'MTNMoMo', label: 'MTN Mobile Money' }
  ];

  constructor(
    private route: ActivatedRoute,
    private eventsService: EventsService,
    private registrationsService: RegistrationsService,
    private paymentsService: PaymentsService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.eventsService.getBySlug(slug).subscribe({
      next: data => {
        this.event = data;
        this.loading = false;
        if (this.auth.isAuthenticated()) {
          this.eventsService.getRecommended(data.id).subscribe({
            next: recs => { this.recommendations = recs; },
            error: () => {}
          });
        }
      },
      error: () => { this.loading = false; }
    });
  }

  get spotsLeft(): number {
    if (!this.event) return 0;
    return this.event.maxAttendees - this.event.registeredCount;
  }

  get isFull(): boolean {
    return this.spotsLeft <= 0;
  }

  get isOrganizer(): boolean {
    const role = this.auth.currentUser()?.role;
    return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'Organizer';
  }

  openCheckout(): void {
    this.showCheckout = true;
    this.error = '';
  }

  cancelCheckout(): void {
    this.showCheckout = false;
    this.error = '';
  }

  registerFree(): void {
    if (!this.event || !this.auth.isAuthenticated()) return;
    this.acting = true;
    this.error = '';

    this.registrationsService.register({ eventId: this.event.id }).subscribe({
      next: reg => {
        this.registration = reg;
        this.successMessage = reg.status === 'Waitlisted'
          ? `You're on the waitlist — position #${reg.waitlistPosition}.`
          : `Registered! Your ticket: ${reg.ticket?.ticketNumber}`;
        this.acting = false;
        if (this.event) this.event.registeredCount++;
      },
      error: err => {
        this.error = err.error?.message ?? 'Registration failed.';
        this.acting = false;
      }
    });
  }

  completePurchase(): void {
    if (!this.event || !this.auth.isAuthenticated()) return;
    this.acting = true;
    this.error = '';

    this.paymentsService.checkout({
      eventId: this.event.id,
      paymentMethod: this.selectedPaymentMethod
    }).subscribe({
      next: result => {
        this.registration = result.registration;
        this.showCheckout = false;
        this.successMessage = `Payment successful! Ticket: ${result.registration.ticket?.ticketNumber} — Ref: ${result.transactionReference}`;
        this.acting = false;
        if (this.event) this.event.registeredCount++;
      },
      error: err => {
        this.error = err.error?.message ?? 'Payment failed. Please try again.';
        this.acting = false;
      }
    });
  }
}
