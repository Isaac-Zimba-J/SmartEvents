import { RegistrationResponse } from './registration.models';

export type PaymentMethod = 'Stripe' | 'AirtelMoney' | 'MTNMoMo' | 'Free';
export type PaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface PaymentCheckoutRequest {
  eventId: string;
  paymentMethod: PaymentMethod;
  notes?: string;
}

export interface PaymentCheckoutResponse {
  paymentId: string;
  transactionReference: string;
  amount: number;
  status: PaymentStatus;
  method: PaymentMethod;
  registration: RegistrationResponse;
}

export interface PaymentSummaryResponse {
  id: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method: PaymentMethod;
  transactionReference?: string;
  createdAt: string;
  paidAt?: string;
  eventTitle: string;
}
