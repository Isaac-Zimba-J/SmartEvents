import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { interval, Subscription } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';
import { VenuesService } from '../../../core/services/venues.service';
import { VenueBookingsService } from '../../../core/services/venue-bookings.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Venue, CreateVenueBookingRequest } from '../../../core/models/venue.models';
import { PaymentMethod } from '../../../core/models/payment.models';

type BookingState = 'idle' | 'pending' | 'completed' | 'failed' | 'timeout';

@Component({
  selector: 'app-venue-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LucideAngularModule],
  templateUrl: './venue-detail.component.html',
  styleUrls: ['./venue-detail.component.scss']
})
export class VenueDetailComponent implements OnInit, OnDestroy {
  venue: Venue | null = null;
  loading = true;
  error = '';

  startDate = '';
  endDate = '';
  notes = '';
  selectedPaymentMethod: PaymentMethod = 'Free';
  phoneNumber = '';
  totalDays = 1;
  totalAmount = 0;
  bookingLoading = false;
  successMessage = '';
  bookingError = '';
  bookingState: BookingState = 'idle';

  private pollSub: Subscription | null = null;
  private readonly MAX_POLLS = 40;

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
    { value: 'AirtelMoney', label: 'Airtel Money' },
    { value: 'MTNMoMo', label: 'MTN Mobile Money' },
    { value: 'Free', label: 'Free / Pay on Arrival' }
  ];

  constructor(
    private route: ActivatedRoute,
    private venuesService: VenuesService,
    private venueBookingsService: VenueBookingsService,
    public authService: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.venuesService.getById(id).subscribe({
      next: data => {
        this.venue = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load venue details';
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  get today(): string {
    return new Date().toISOString().split('T')[0];
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get isMobileMoney(): boolean {
    return this.selectedPaymentMethod === 'AirtelMoney' || this.selectedPaymentMethod === 'MTNMoMo';
  }

  onDatesChange(): void {
    if (this.startDate && this.endDate && this.startDate < this.endDate) {
      const ms = new Date(this.endDate).getTime() - new Date(this.startDate).getTime();
      this.totalDays = Math.max(1, Math.floor(ms / 86400000));
      this.totalAmount = this.totalDays * (this.venue?.pricePerDay ?? 0);
    } else {
      this.totalDays = 1;
      this.totalAmount = 0;
    }
  }

  onMethodChange(): void {
    if (this.isMobileMoney && !this.phoneNumber) {
      this.phoneNumber = this.authService.currentUser()?.phone ?? '';
    }
  }

  book(): void {
    if (!this.isAuthenticated) {
      this.toast.error('Please log in to book this venue');
      return;
    }

    if (this.isMobileMoney && !this.phoneNumber.trim()) {
      this.bookingError = 'Please enter your mobile money number.';
      return;
    }

    this.successMessage = '';
    this.bookingError = '';
    this.bookingLoading = true;

    const request: CreateVenueBookingRequest = {
      venueId: this.venue!.id,
      startDate: new Date(this.startDate).toISOString(),
      endDate: new Date(this.endDate).toISOString(),
      notes: this.notes || undefined,
      paymentMethod: this.selectedPaymentMethod,
      phoneNumber: this.isMobileMoney ? this.phoneNumber.trim() : undefined
    };

    this.venueBookingsService.create(request).subscribe({
      next: booking => {
        this.bookingLoading = false;
        if (this.isMobileMoney) {
          this.bookingState = 'pending';
          this.startPolling(booking.id);
        } else {
          this.bookingState = 'completed';
          this.successMessage = 'Venue booked successfully!';
          this.resetForm();
        }
      },
      error: err => {
        this.bookingError = err.error?.message ?? 'Booking failed';
        this.bookingLoading = false;
      }
    });
  }

  retryForm(): void {
    this.bookingState = 'idle';
    this.bookingError = '';
    this.stopPolling();
  }

  private startPolling(bookingId: string): void {
    let pollCount = 0;

    this.pollSub = interval(3000).pipe(
      take(this.MAX_POLLS),
      switchMap(() => this.venueBookingsService.getStatus(bookingId))
    ).subscribe({
      next: status => {
        pollCount++;

        if (status.paymentStatus === 'Completed') {
          this.bookingState = 'completed';
          this.successMessage = 'Venue booked successfully! Payment confirmed.';
          this.resetForm();
          this.stopPolling();
        } else if (status.paymentStatus === 'Failed') {
          this.bookingState = 'failed';
          this.bookingError = 'Payment was declined. Please try again.';
          this.stopPolling();
        } else if (pollCount >= this.MAX_POLLS) {
          this.bookingState = 'timeout';
          this.bookingError = 'Payment timed out. Check your mobile money app and try again.';
        }
      },
      error: () => {}
    });
  }

  private stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = null;
  }

  private resetForm(): void {
    this.startDate = '';
    this.endDate = '';
    this.notes = '';
    this.selectedPaymentMethod = 'Free';
    this.phoneNumber = '';
    this.totalDays = 1;
    this.totalAmount = 0;
  }
}
