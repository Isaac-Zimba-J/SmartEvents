import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  PaymentCheckoutRequest,
  PaymentCheckoutResponse,
  PaymentStatusResponse,
  PaymentSummaryResponse
} from '../models/payment.models';

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly apiUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  checkout(request: PaymentCheckoutRequest) {
    return this.http.post<PaymentCheckoutResponse>(`${this.apiUrl}/checkout`, request);
  }

  getStatus(paymentId: string) {
    return this.http.get<PaymentStatusResponse>(`${this.apiUrl}/${paymentId}/status`);
  }

  getMyPayments() {
    return this.http.get<PaymentSummaryResponse[]>(`${this.apiUrl}/my`);
  }
}
