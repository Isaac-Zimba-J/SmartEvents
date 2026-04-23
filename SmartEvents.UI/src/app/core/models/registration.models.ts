export type RegistrationStatus = 'Pending' | 'Confirmed' | 'Waitlisted' | 'Cancelled' | 'CheckedIn';

export interface TicketResponse {
  id: string;
  ticketNumber: string;
  qrCode: string;
  isUsed: boolean;
  issuedAt: string;
}

export interface RegistrationResponse {
  id: string;
  status: RegistrationStatus;
  waitlistPosition: number;
  notes?: string;
  registeredAt: string;
  checkedInAt?: string;
  eventId: string;
  eventTitle: string;
  userId: string;
  userName: string;
  ticket?: TicketResponse;
}

export interface CreateRegistrationRequest {
  eventId: string;
  notes?: string;
}
