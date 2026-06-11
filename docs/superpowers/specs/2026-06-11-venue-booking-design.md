# Venue Booking — Design Spec

**Date:** 2026-06-11
**Status:** Approved

---

## Overview

Add venue booking to SmartEvents: a venue detail page where any authenticated user can view full venue information and book a venue for a date range, paying via the existing mock-payment system. Confirmed bookings are tracked in a new "My Bookings" page.

---

## User Flow

1. User browses `/venues` (existing list page) and clicks a venue.
2. New `/venues/:id` detail page shows full venue info — description, amenities, type, capacity, price per day.
3. Inline booking form at the bottom of the detail page: user selects start date, end date, optional notes, and a payment method. Total cost is shown automatically (days × PricePerDay).
4. User clicks "Book & Pay". If the venue has no confirmed booking overlapping those dates, the API creates a `VenueBooking` with `Confirmed` status and mocked payment.
5. User can view all their bookings at `/venues/my-bookings`.
6. User can cancel a booking from the My Bookings page.

---

## Backend

### New Entity: `VenueBooking`

File: `SmartEvents.API/Domain/Entities/VenueBooking.cs`

| Field | Type | Notes |
|---|---|---|
| Id | Guid | `Guid.NewGuid()` at creation |
| VenueId | Guid | FK → Venue |
| UserId | Guid | FK → User (the booker) |
| StartDate | DateTime (UTC) | Inclusive start of booking |
| EndDate | DateTime (UTC) | Inclusive end of booking |
| Notes | string? | Optional purpose/notes |
| Status | VenueBookingStatus | `Confirmed` after successful payment |
| TotalAmount | decimal | Calculated server-side: days × PricePerDay |
| PaymentMethod | PaymentMethod | Reuses existing enum |
| PaymentStatus | PaymentStatus | `Completed` after mock payment |
| TransactionRef | string? | Mock transaction reference |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | |

Navigation properties: `Venue`, `User` (for includes).

### New Enum

Add to `SmartEvents.API/Domain/Enums/Enums.cs`:

```csharp
public enum VenueBookingStatus { Pending, Confirmed, Cancelled }
```

### DbContext

- Add `DbSet<VenueBooking> VenueBookings` to `SmartEventsDbContext`.
- Configure in `OnModelCreating`: FK relationships, index on `(VenueId, StartDate, EndDate)`.
- Run EF migration: `dotnet ef migrations add AddVenueBooking`.

### New Controller: `VenueBookingsController`

File: `SmartEvents.API/Controllers/VenueBookingsController.cs`

No service layer — inject `SmartEventsDbContext` directly via primary constructor.

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/venue-bookings` | `[Authorize]` | Create booking + mock payment → returns `VenueBookingResponse` |
| GET | `/api/venue-bookings/my` | `[Authorize]` | Returns current user's bookings (all statuses, ordered by StartDate desc) |
| GET | `/api/venue-bookings/{id:guid}` | `[Authorize]` | Single booking (owner only) |
| DELETE | `/api/venue-bookings/{id:guid}` | `[Authorize]` | Cancel booking (owner only, status → Cancelled) |

**POST logic:**
1. Load venue — 404 if not found or `!IsAvailable`.
2. Validate `StartDate < EndDate` and `StartDate >= DateTime.UtcNow.Date` — 400 if invalid.
3. Check for date conflicts: any existing `Confirmed` booking for the same venue where date ranges overlap → 409 Conflict with `{ message = "Venue is already booked for those dates." }`.
4. Calculate `totalAmount = (venue.PricePerDay ?? 0m) * Math.Max(1, (decimal)(endDate.Date - startDate.Date).TotalDays)` — minimum 1 day, uses `.Date` to strip time component.
5. Create `VenueBooking` with `Status = Confirmed`, `PaymentStatus = Completed`, mock `TransactionRef`.
6. Save and return `201 Created`.

**DELETE logic:** Load booking, verify `UserId == currentUserId` (else 403), set `Status = Cancelled`, `UpdatedAt = UtcNow`, save → `204 No Content`.

### New DTOs

File: `SmartEvents.API/Application/DTOs/VenueBookingDtos.cs`

```csharp
public record CreateVenueBookingRequest(
    [Required] Guid VenueId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    string? Notes,
    PaymentMethod PaymentMethod
);

public record VenueBookingResponse(
    Guid Id,
    Guid VenueId,
    string VenueName,
    string VenueAddress,
    string VenueCity,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes,
    VenueBookingStatus Status,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    string? TransactionRef,
    DateTime CreatedAt
);
```

---

## Frontend

### New Model Fields

Add to `SmartEvents.UI/src/app/core/models/venue.models.ts`:

```ts
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
  paymentStatus: string;
  transactionRef?: string;
  createdAt: string;
}

export interface CreateVenueBookingRequest {
  venueId: string;
  startDate: string;
  endDate: string;
  notes?: string;
  paymentMethod: PaymentMethod;
}
```

(`PaymentMethod` imported from `payment.models.ts`)

### New Service

File: `SmartEvents.UI/src/app/core/services/venue-bookings.service.ts`

Methods:
- `create(request: CreateVenueBookingRequest): Observable<VenueBooking>`
- `getMy(): Observable<VenueBooking[]>`
- `getById(id: string): Observable<VenueBooking>`
- `cancel(id: string): Observable<void>`

### New Routes (venues.routes.ts)

```ts
{ path: ':id', loadComponent: ... VenueDetailComponent }
{ path: 'my-bookings', canActivate: [authGuard], loadComponent: ... MyVenueBookingsComponent }
```

`my-bookings` must be declared **before** `:id` to avoid the slug route swallowing it.

### New Components

**`venue-detail` (`/venues/:id`)**

- Loads venue by ID via `VenuesService.getById(id)`.
- Shows: name, type badge, city/country, capacity, price per day, description, amenities (as tags).
- Inline booking form at the bottom (Layout B — below a divider):
  - Start date, end date inputs.
  - Notes textarea (optional).
  - Auto-calculated total (days × pricePerDay); shown as `K 0` if venue has no price.
  - Payment method selector (same 4 options as event checkout: Stripe, Airtel Money, MTN MoMo, Free).
  - "Book & Pay" button — disabled while submitting.
  - Shows success message on confirmation; shows error message (including 409 conflict) on failure.
- Booking form hidden and replaced with a "Log in to book" note when user is not authenticated.

**`my-venue-bookings` (`/venues/my-bookings`)**

- Loads current user's bookings via `VenueBookingsService.getMy()`.
- Lists bookings with: venue name, dates, total amount, status badge (Confirmed = green, Cancelled = grey).
- "Cancel" button on Confirmed bookings — calls `cancel(id)` with optimistic UI update.
- Empty state when no bookings exist.

### Nav Update

Add "My Bookings" link to the venues section in the nav/sidebar, visible only when authenticated.

---

## Scope Boundaries

- No admin view for incoming bookings (out of scope — venue company admins do not need a management UI in this iteration).
- No calendar/availability preview showing blocked dates on the detail page (out of scope).
- No email/SMS notification on booking confirmation (out of scope).
- Venue price of `null` / `0` results in `K 0` total — booking still proceeds (free venue).

---

## Files Changed

| File | Action |
|---|---|
| `SmartEvents.API/Domain/Entities/VenueBooking.cs` | Create |
| `SmartEvents.API/Domain/Enums/Enums.cs` | Modify — add `VenueBookingStatus` enum |
| `SmartEvents.API/Infrastructure/Data/SmartEventsDbContext.cs` | Modify — add `DbSet`, configure in `OnModelCreating` |
| `SmartEvents.API/Infrastructure/Data/Migrations/` | Create — `AddVenueBooking` migration |
| `SmartEvents.API/Application/DTOs/VenueBookingDtos.cs` | Create |
| `SmartEvents.API/Controllers/VenueBookingsController.cs` | Create |
| `SmartEvents.UI/src/app/core/models/venue.models.ts` | Modify — add booking types |
| `SmartEvents.UI/src/app/core/services/venue-bookings.service.ts` | Create |
| `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.ts` | Create |
| `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.html` | Create |
| `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.ts` | Create |
| `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.html` | Create |
| `SmartEvents.UI/src/app/features/venues/venues.routes.ts` | Modify — add `:id` and `my-bookings` routes |
| `SmartEvents.UI/src/styles.scss` | Modify — add venue booking styles |
| `CLAUDE.md` | Modify — update change log |
