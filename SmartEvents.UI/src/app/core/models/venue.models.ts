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
