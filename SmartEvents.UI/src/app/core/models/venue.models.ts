import { PaymentMethod, PaymentStatus } from './payment.models';

export type VenueType = 'Indoor' | 'Outdoor' | 'Virtual' | 'Hybrid';

export interface Venue {
  id: string;
  name: string;
  description?: string;
  address: string;
  city: string;
  country: string;
  latitude?: number;
  longitude?: number;
  capacity: number;
  type: VenueType;
  imageUrl?: string;
  amenities?: string;
  pricePerDay?: number;
  isAvailable: boolean;
  companyId: string;
  companyName: string;
  createdAt: string;
}

export interface CreateVenueRequest {
  name: string;
  description?: string;
  address: string;
  city: string;
  country: string;
  latitude?: number;
  longitude?: number;
  capacity: number;
  type: VenueType;
  imageUrl?: string;
  amenities?: string;
  pricePerDay?: number;
}

export type VenueBookingStatus = 'Pending' | 'Confirmed' | 'Cancelled';

export interface VenueBooking {
  id: string;
  venueId: string;
  venueName: string;
  venueAddress: string;
  venueCity: string;
  startDate: string;
  endDate: string;
  notes?: string;
  status: VenueBookingStatus;
  totalAmount: number;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  transactionRef?: string;
  createdAt: string;
}

export interface CreateVenueBookingRequest {
  venueId: string;
  startDate: string;
  endDate: string;
  notes?: string;
  paymentMethod: PaymentMethod;
  phoneNumber?: string;
}

export interface VenueBookingStatusResponse {
  bookingId: string;
  status: VenueBookingStatus;
  paymentStatus: PaymentStatus;
  transactionRef?: string;
}
