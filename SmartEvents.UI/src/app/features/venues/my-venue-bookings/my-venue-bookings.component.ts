import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { VenueBookingsService } from '../../../core/services/venue-bookings.service';
import { ToastService } from '../../../core/services/toast.service';
import { VenueBooking } from '../../../core/models/venue.models';

@Component({
  selector: 'app-my-venue-bookings',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  templateUrl: './my-venue-bookings.component.html',
  styleUrl: './my-venue-bookings.component.scss'
})
export class MyVenueBookingsComponent implements OnInit {
  bookings: VenueBooking[] = [];
  loading = true;
  error = '';

  constructor(private venueBookingsService: VenueBookingsService, private toastService: ToastService) {}

  ngOnInit(): void {
    this.venueBookingsService.getMy().subscribe({
      next: data => {
        this.bookings = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load your bookings';
        this.loading = false;
      }
    });
  }

  cancel(id: string): void {
    this.venueBookingsService.cancel(id).subscribe({
      next: () => {
        this.bookings = this.bookings.map(b =>
          b.id === id ? { ...b, status: 'Cancelled' as const } : b
        );
      },
      error: err => {
        this.toastService.error(err.error?.message ?? 'Could not cancel booking');
      }
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Confirmed': return 'status-confirmed';
      case 'Cancelled': return 'status-cancelled';
      case 'Pending': return 'status-pending';
      default: return '';
    }
  }
}
