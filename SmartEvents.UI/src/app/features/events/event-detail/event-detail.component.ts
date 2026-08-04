import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { interval, Subscription } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';
import { EventsService } from '../../../core/services/events.service';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { PaymentsService } from '../../../core/services/payments.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventDetail, EventSummary } from '../../../core/models/event.models';
import { RegistrationResponse } from '../../../core/models/registration.models';
import { PaymentMethod } from '../../../core/models/payment.models';

type CheckoutState = 'idle' | 'pending' | 'completed' | 'failed' | 'timeout';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LucideAngularModule],
  templateUrl: './event-detail.component.html'
})
export class EventDetailComponent implements OnInit, OnDestroy {
  event: EventDetail | null = null;
  loading = true;
  acting = false;
  registration: RegistrationResponse | null = null;
  error = '';
  successMessage = '';
  recommendations: EventSummary[] = [];

  showCheckout = false;
  selectedPaymentMethod: PaymentMethod = 'AirtelMoney';
  phoneNumber = '';
  checkoutState: CheckoutState = 'idle';

  private pollSub: Subscription | null = null;
  private routeSub: Subscription | null = null;
  private readonly MAX_POLLS = 40;

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
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
    this.routeSub = this.route.paramMap.subscribe(params => {
      const slug = params.get('slug')!;
      this.loading = true;
      this.event = null;
      this.registration = null;
      this.successMessage = '';
      this.error = '';
      this.recommendations = [];
      this.showCheckout = false;
      this.checkoutState = 'idle';
      this.stopPolling();

      this.eventsService.getBySlug(slug).subscribe({
        next: data => {
          this.event = data;
          this.loading = false;
          this.eventsService.getRecommended(data.id, data.category).subscribe({
            next: recs => { this.recommendations = recs; },
            error: () => {}
          });
        },
        error: () => { this.loading = false; }
      });
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.routeSub?.unsubscribe();
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
    this.checkoutState = 'idle';
    this.error = '';
    this.phoneNumber = this.auth.currentUser()?.phone ?? '';
  }

  cancelCheckout(): void {
    this.showCheckout = false;
    this.checkoutState = 'idle';
    this.error = '';
    this.stopPolling();
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
    if (!this.event || !this.auth.isAuthenticated() || !this.phoneNumber.trim()) return;
    this.acting = true;
    this.error = '';

    this.paymentsService.checkout({
      eventId: this.event.id,
      paymentMethod: this.selectedPaymentMethod,
      phoneNumber: this.phoneNumber.trim()
    }).subscribe({
      next: result => {
        this.registration = result.registration;
        this.acting = false;
        this.checkoutState = 'pending';
        this.startPolling(result.paymentId);
      },
      error: err => {
        this.error = err.error?.message ?? 'Payment initiation failed. Please try again.';
        this.acting = false;
      }
    });
  }

  private startPolling(paymentId: string): void {
    let pollCount = 0;

    this.pollSub = interval(3000).pipe(
      take(this.MAX_POLLS),
      switchMap(() => this.paymentsService.getStatus(paymentId))
    ).subscribe({
      next: status => {
        pollCount++;

        if (status.status === 'Completed') {
          this.checkoutState = 'completed';
          this.successMessage = `Payment successful! Ticket: ${this.registration?.ticket?.ticketNumber}`;
          if (this.event) this.event.registeredCount++;
          this.stopPolling();
        } else if (status.status === 'Failed') {
          this.checkoutState = 'failed';
          this.error = 'Payment was declined. Please try again with a different number.';
          this.stopPolling();
        } else if (pollCount >= this.MAX_POLLS) {
          this.checkoutState = 'timeout';
          this.error = 'Payment timed out. Check your mobile money app and try again.';
        }
      },
      error: () => {
        // Network errors during polling are non-fatal — keep trying
      }
    });
  }

  private stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = null;
  }
}
