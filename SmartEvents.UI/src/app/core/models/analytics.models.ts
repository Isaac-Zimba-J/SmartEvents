export interface OverviewAnalytics {
  totalEvents: number;
  publishedEvents: number;
  totalRegistrations: number;
  totalCheckedIn: number;
  totalRevenue: number;
  totalUsers: number;
  totalVenues: number;
  totalCompanies: number;
}

export interface EventAnalytics {
  eventId: string;
  title: string;
  status: string;
  maxAttendees: number;
  confirmed: number;
  waitlisted: number;
  checkedIn: number;
  cancelled: number;
  revenue: number;
  fillRate: number;
}
