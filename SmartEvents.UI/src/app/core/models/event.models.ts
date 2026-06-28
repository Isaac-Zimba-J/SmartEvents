export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';
export type EventCategory = 'Conference' | 'Workshop' | 'Concert' | 'Exhibition' | 'Sports' | 'Networking' | 'Webinar' | 'Other';

export interface VenueResponse {
  id: string;
  name: string;
  description?: string;
  address: string;
  city: string;
  country: string;
  latitude?: number;
  longitude?: number;
  capacity: number;
  type: string;
  imageUrl?: string;
  amenities?: string;
  pricePerDay?: number;
  isAvailable: boolean;
  companyId: string;
  companyName: string;
  createdAt: string;
}

export interface EventSummary {
  id: string;
  title: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  category: EventCategory;
  status: EventStatus;
  startDate: string;
  endDate: string;
  maxAttendees: number;
  registeredCount: number;
  isTicketed: boolean;
  ticketPrice: number;
  waitlistEnabled: boolean;
  isPublic: boolean;
  tags?: string;
  companyId: string;
  companyName: string;
  venue?: VenueResponse;
  venueText?: string;
  organizerName: string;
  createdAt: string;
}

export interface EventDetail extends EventSummary {
  timezone?: string;
  waitlistedCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateEventRequest {
  title: string;
  description?: string;
  imageUrl?: string;
  category: EventCategory;
  startDate: string;
  endDate: string;
  timezone?: string;
  maxAttendees: number;
  isTicketed: boolean;
  ticketPrice: number;
  waitlistEnabled: boolean;
  isPublic: boolean;
  tags?: string;
  venueId?: string;
  venueText?: string;
  companyId?: string;
}
