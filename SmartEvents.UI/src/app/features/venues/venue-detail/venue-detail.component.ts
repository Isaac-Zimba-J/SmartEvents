import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { VenuesService } from '../../../core/services/venues.service';
import { VenueBookingsService } from '../../../core/services/venue-bookings.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Venue, CreateVenueBookingRequest } from '../../../core/models/venue.models';
import { PaymentMethod } from '../../../core/models/payment.models';

@Component({
  selector: 'app-venue-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LucideAngularModule],
  templateUrl: './venue-detail.component.html',
  styleUrls: ['./venue-detail.component.scss']
})
export class VenueDetailComponent implements OnInit {
  venue: Venue | null = null;
  loading = true;
  error = '';

  // Booking form fields
  startDate = '';
  endDate = '';
  notes = '';
  selectedPaymentMethod: PaymentMethod = 'Free';
  totalDays = 1;
  totalAmount = 0;
  bookingLoading = false;
  successMessage = '';
  bookingError = '';

  paymentMethods: PaymentMethod[] = ['Stripe', 'AirtelMoney', 'MTNMoMo', 'Free'];

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

  get today(): string {
    return new Date().toISOString().split('T')[0];
  }

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
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

  book(): void {
    if (!this.isAuthenticated) {
      this.toast.error('Please log in to book this venue');
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
      paymentMethod: this.selectedPaymentMethod
    };

    this.venueBookingsService.create(request).subscribe({
      next: () => {
        this.successMessage = 'Venue booked successfully!';
        this.startDate = '';
        this.endDate = '';
        this.notes = '';
        this.selectedPaymentMethod = 'Free';
        this.totalDays = 1;
        this.totalAmount = 0;
        this.bookingLoading = false;
      },
      error: err => {
        this.bookingError = err.error?.message ?? 'Booking failed';
        this.bookingLoading = false;
      }
    });
  }
}
