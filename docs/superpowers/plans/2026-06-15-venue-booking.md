# Venue Booking + Bug Fixes + Company Members Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three bugs (sticky navbar, event recommendations, venue view-only), add full venue booking, and add company member management so CompanyAdmins can add Organizers and other members to their company.

**Architecture:** New `VenueBooking` EF entity stores date-range bookings. A new `VenueBookingsController` handles create / list-mine / get / cancel. The Angular side gets a `VenueBookingsService`, a `VenueDetailComponent` at `/venues/:id`, and a `MyVenueBookingsComponent` at `/venues/my-bookings`. Payment follows the same mock pattern as event registrations — booking is confirmed immediately on submission. Company member management adds 4 endpoints to `CompaniesController` and a new `CompanyMembersComponent`.

**Tech Stack:** ASP.NET Core .NET 10, EF Core 10 (PostgreSQL, code-first), Angular 18 (standalone components, `@if`/`@for` control flow, Signals), Lucide Angular, global SCSS (`styles.scss`).

---

## Files Changed

| File | Action |
|---|---|
| `SmartEvents.UI/src/styles.scss` | Modify — fix `.dashboard-nav` sticky positioning (Task 9) |
| `SmartEvents.API/Infrastructure/Data/DbSeeder.cs` | Modify — seed 3 more events in overlapping categories (Task 10) |
| `SmartEvents.API/Application/DTOs/CompanyDtos.cs` | Modify — add member DTOs (Task 11) |
| `SmartEvents.API/Controllers/CompaniesController.cs` | Modify — add 4 member endpoints (Task 11) |
| `SmartEvents.UI/src/app/core/services/company-members.service.ts` | Create (Task 12) |
| `SmartEvents.UI/src/app/features/companies/company-members/company-members.component.ts` | Create (Task 12) |
| `SmartEvents.UI/src/app/features/companies/company-members/company-members.component.html` | Create (Task 12) |
| `SmartEvents.UI/src/app/features/companies/companies.routes.ts` | Modify — add members route (Task 12) |
| `SmartEvents.API/Domain/Enums/Enums.cs` | Modify — add `VenueBookingStatus` (Task 1) |
| `SmartEvents.API/Domain/Entities/VenueBooking.cs` | Create |
| `SmartEvents.API/Infrastructure/Data/SmartEventsDbContext.cs` | Modify — `DbSet` + `OnModelCreating` config |
| `SmartEvents.API/Infrastructure/Data/Migrations/` | Create — via `dotnet ef migrations add AddVenueBooking` |
| `SmartEvents.API/Application/DTOs/VenueBookingDtos.cs` | Create |
| `SmartEvents.API/Controllers/VenueBookingsController.cs` | Create |
| `SmartEvents.UI/src/app/core/models/venue.models.ts` | Modify — add booking types |
| `SmartEvents.UI/src/app/core/services/venue-bookings.service.ts` | Create |
| `SmartEvents.UI/src/app/app.config.ts` | Modify — register `BookMarked` icon |
| `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.ts` | Create |
| `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.html` | Create |
| `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.ts` | Create |
| `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.html` | Create |
| `SmartEvents.UI/src/app/features/venues/venues-list/venues-list.component.html` | Modify — add "View Details" link to each card |
| `SmartEvents.UI/src/app/features/venues/venues.routes.ts` | Modify — add `:id` and `my-bookings` routes |
| `SmartEvents.UI/src/app/features/dashboard/dashboard.component.html` | Modify — add "My Bookings" nav link |
| `SmartEvents.UI/src/styles.scss` | Modify — add venue booking styles |
| `CLAUDE.md` | Modify — update change log |

---

## Task 1: VenueBookingStatus enum + VenueBooking entity

**Files:**
- Modify: `SmartEvents.API/Domain/Enums/Enums.cs`
- Create: `SmartEvents.API/Domain/Entities/VenueBooking.cs`

- [ ] **Step 1: Add the enum to Enums.cs**

  Open `SmartEvents.API/Domain/Enums/Enums.cs`. After the last existing enum, append:

  ```csharp
  public enum VenueBookingStatus { Pending, Confirmed, Cancelled }
  ```

- [ ] **Step 2: Create the entity**

  Create `SmartEvents.API/Domain/Entities/VenueBooking.cs`:

  ```csharp
  using SmartEvents.API.Domain.Enums;

  namespace SmartEvents.API.Domain.Entities;

  public class VenueBooking
  {
      public Guid Id { get; set; }
      public Guid VenueId { get; set; }
      public Venue Venue { get; set; } = null!;
      public Guid UserId { get; set; }
      public User User { get; set; } = null!;
      public DateTime StartDate { get; set; }
      public DateTime EndDate { get; set; }
      public string? Notes { get; set; }
      public VenueBookingStatus Status { get; set; } = VenueBookingStatus.Pending;
      public decimal TotalAmount { get; set; }
      public PaymentMethod PaymentMethod { get; set; }
      public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
      public string? TransactionRef { get; set; }
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
  }
  ```

- [ ] **Step 3: Build to check for errors**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -10
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.API/Domain/Enums/Enums.cs SmartEvents.API/Domain/Entities/VenueBooking.cs
  git commit -m "feat: add VenueBooking entity and VenueBookingStatus enum"
  ```

---

## Task 2: DbContext update + EF migration

**Files:**
- Modify: `SmartEvents.API/Infrastructure/Data/SmartEventsDbContext.cs`
- Create: migration via CLI

- [ ] **Step 1: Add the DbSet**

  In `SmartEventsDbContext.cs`, the DbSet declarations start at line 8. After the `Notifications` line, add:

  ```csharp
  public DbSet<VenueBooking> VenueBookings => Set<VenueBooking>();
  ```

- [ ] **Step 2: Add OnModelCreating config**

  Inside `OnModelCreating`, after the `// Venue` config block and before the `// Event` block, insert:

  ```csharp
  // VenueBooking
  modelBuilder.Entity<VenueBooking>(e =>
  {
      e.HasKey(vb => vb.Id);
      e.Property(vb => vb.TotalAmount).HasColumnType("decimal(18,2)");
      e.HasOne(vb => vb.Venue)
          .WithMany()
          .HasForeignKey(vb => vb.VenueId)
          .OnDelete(DeleteBehavior.Cascade);
      e.HasOne(vb => vb.User)
          .WithMany()
          .HasForeignKey(vb => vb.UserId)
          .OnDelete(DeleteBehavior.Cascade);
      e.HasIndex(vb => new { vb.VenueId, vb.StartDate, vb.EndDate });
  });
  ```

- [ ] **Step 3: Build the API**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -10
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Add the EF migration**

  ```bash
  cd SmartEvents.API
  dotnet ef migrations add AddVenueBooking 2>&1 | tail -10
  ```

  Expected: `Build succeeded.` and a new migration file in `Infrastructure/Data/Migrations/`.

- [ ] **Step 5: Commit**

  ```bash
  git add SmartEvents.API/Infrastructure/Data/SmartEventsDbContext.cs
  git add SmartEvents.API/Infrastructure/Data/Migrations/
  git commit -m "feat: add VenueBookings DbSet and AddVenueBooking migration"
  ```

---

## Task 3: VenueBookingDtos + VenueBookingsController

**Files:**
- Create: `SmartEvents.API/Application/DTOs/VenueBookingDtos.cs`
- Create: `SmartEvents.API/Controllers/VenueBookingsController.cs`

- [ ] **Step 1: Create the DTOs**

  Create `SmartEvents.API/Application/DTOs/VenueBookingDtos.cs`:

  ```csharp
  using SmartEvents.API.Domain.Enums;
  using System.ComponentModel.DataAnnotations;

  namespace SmartEvents.API.Application.DTOs;

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

- [ ] **Step 2: Create the controller**

  Create `SmartEvents.API/Controllers/VenueBookingsController.cs`:

  ```csharp
  using System.Security.Claims;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;
  using SmartEvents.API.Application.DTOs;
  using SmartEvents.API.Domain.Entities;
  using SmartEvents.API.Domain.Enums;
  using SmartEvents.API.Infrastructure.Data;

  namespace SmartEvents.API.Controllers;

  [ApiController]
  [Route("api/venue-bookings")]
  [Authorize]
  public class VenueBookingsController(SmartEventsDbContext db) : ControllerBase
  {
      [HttpPost]
      public async Task<ActionResult<VenueBookingResponse>> Create(CreateVenueBookingRequest request)
      {
          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

          var venue = await db.Venues.FindAsync(request.VenueId);
          if (venue is null || !venue.IsAvailable)
              return NotFound(new { message = "Venue not found or unavailable." });

          if (request.StartDate >= request.EndDate)
              return BadRequest(new { message = "End date must be after start date." });

          if (request.StartDate.Date < DateTime.UtcNow.Date)
              return BadRequest(new { message = "Start date cannot be in the past." });

          var hasConflict = await db.VenueBookings.AnyAsync(vb =>
              vb.VenueId == request.VenueId &&
              vb.Status == VenueBookingStatus.Confirmed &&
              vb.StartDate < request.EndDate &&
              vb.EndDate > request.StartDate);

          if (hasConflict)
              return Conflict(new { message = "Venue is already booked for those dates." });

          var days = Math.Max(1m, (decimal)(request.EndDate.Date - request.StartDate.Date).TotalDays);
          var totalAmount = (venue.PricePerDay ?? 0m) * days;

          var booking = new VenueBooking
          {
              Id = Guid.NewGuid(),
              VenueId = request.VenueId,
              UserId = userId,
              StartDate = request.StartDate.ToUniversalTime(),
              EndDate = request.EndDate.ToUniversalTime(),
              Notes = request.Notes,
              Status = VenueBookingStatus.Confirmed,
              TotalAmount = totalAmount,
              PaymentMethod = request.PaymentMethod,
              PaymentStatus = PaymentStatus.Completed,
              TransactionRef = $"VB-{Guid.NewGuid().ToString()[..8].ToUpper()}"
          };

          db.VenueBookings.Add(booking);
          await db.SaveChangesAsync();
          await db.Entry(booking).Reference(b => b.Venue).LoadAsync();

          return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ToResponse(booking));
      }

      [HttpGet("my")]
      public async Task<ActionResult<IEnumerable<VenueBookingResponse>>> GetMy()
      {
          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
          var bookings = await db.VenueBookings
              .Include(vb => vb.Venue)
              .Where(vb => vb.UserId == userId)
              .OrderByDescending(vb => vb.StartDate)
              .ToListAsync();
          return Ok(bookings.Select(ToResponse));
      }

      [HttpGet("{id:guid}")]
      public async Task<ActionResult<VenueBookingResponse>> GetById(Guid id)
      {
          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
          var booking = await db.VenueBookings
              .Include(vb => vb.Venue)
              .FirstOrDefaultAsync(vb => vb.Id == id);
          if (booking is null) return NotFound();
          if (booking.UserId != userId) return Forbid();
          return Ok(ToResponse(booking));
      }

      [HttpDelete("{id:guid}")]
      public async Task<IActionResult> Cancel(Guid id)
      {
          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
          var booking = await db.VenueBookings.FindAsync(id);
          if (booking is null) return NotFound();
          if (booking.UserId != userId) return Forbid();
          if (booking.Status == VenueBookingStatus.Cancelled)
              return BadRequest(new { message = "Booking is already cancelled." });

          booking.Status = VenueBookingStatus.Cancelled;
          booking.UpdatedAt = DateTime.UtcNow;
          await db.SaveChangesAsync();
          return NoContent();
      }

      private static VenueBookingResponse ToResponse(VenueBooking b) => new(
          b.Id, b.VenueId,
          b.Venue?.Name ?? string.Empty,
          b.Venue?.Address ?? string.Empty,
          b.Venue?.City ?? string.Empty,
          b.StartDate, b.EndDate, b.Notes,
          b.Status, b.TotalAmount,
          b.PaymentMethod, b.PaymentStatus,
          b.TransactionRef, b.CreatedAt
      );
  }
  ```

- [ ] **Step 3: Build to check for errors**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -10
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.API/Application/DTOs/VenueBookingDtos.cs SmartEvents.API/Controllers/VenueBookingsController.cs
  git commit -m "feat: add VenueBookingDtos and VenueBookingsController"
  ```

---

## Task 4: Frontend models + VenueBookingsService + icon

**Files:**
- Modify: `SmartEvents.UI/src/app/core/models/venue.models.ts`
- Create: `SmartEvents.UI/src/app/core/services/venue-bookings.service.ts`
- Modify: `SmartEvents.UI/src/app/app.config.ts`

- [ ] **Step 1: Add booking types to venue.models.ts**

  Append to the end of `SmartEvents.UI/src/app/core/models/venue.models.ts`:

  ```typescript
  import { PaymentMethod, PaymentStatus } from './payment.models';

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
  }
  ```

- [ ] **Step 2: Create VenueBookingsService**

  Create `SmartEvents.UI/src/app/core/services/venue-bookings.service.ts`:

  ```typescript
  import { Injectable } from '@angular/core';
  import { HttpClient } from '@angular/common/http';
  import { environment } from '../../../environments/environment';
  import { VenueBooking, CreateVenueBookingRequest } from '../models/venue.models';

  @Injectable({ providedIn: 'root' })
  export class VenueBookingsService {
    private readonly apiUrl = `${environment.apiUrl}/venue-bookings`;

    constructor(private http: HttpClient) {}

    create(request: CreateVenueBookingRequest) {
      return this.http.post<VenueBooking>(this.apiUrl, request);
    }

    getMy() {
      return this.http.get<VenueBooking[]>(`${this.apiUrl}/my`);
    }

    getById(id: string) {
      return this.http.get<VenueBooking>(`${this.apiUrl}/${id}`);
    }

    cancel(id: string) {
      return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
  }
  ```

- [ ] **Step 3: Register the BookMarked icon in app.config.ts**

  In `SmartEvents.UI/src/app/app.config.ts`, the import line currently ends with `Briefcase`. Add `BookMarked` to the import:

  ```typescript
  import {
    LucideAngularModule,
    Eye, EyeOff,
    LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
    Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
    Check, X, Shield, Mail, Phone, Globe, Clock,
    Search, ChevronRight, Settings, AlertCircle, CheckCircle,
    ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked
  } from 'lucide-angular';
  ```

  Also add `BookMarked` to the `LucideAngularModule.pick({...})` call:

  ```typescript
  importProvidersFrom(LucideAngularModule.pick({
    Eye, EyeOff,
    LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
    Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
    Check, X, Shield, Mail, Phone, Globe, Clock,
    Search, ChevronRight, Settings, AlertCircle, CheckCircle,
    ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked
  }))
  ```

- [ ] **Step 4: Build to check for TypeScript errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -15
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 5: Commit**

  ```bash
  git add SmartEvents.UI/src/app/core/models/venue.models.ts \
          SmartEvents.UI/src/app/core/services/venue-bookings.service.ts \
          SmartEvents.UI/src/app/app.config.ts
  git commit -m "feat: add VenueBooking models, VenueBookingsService, BookMarked icon"
  ```

---

## Task 5: VenueDetailComponent

**Files:**
- Create: `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.ts`
- Create: `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.html`

- [ ] **Step 1: Create the TypeScript component**

  Create directory `SmartEvents.UI/src/app/features/venues/venue-detail/` then create `venue-detail.component.ts`:

  ```typescript
  import { Component, OnInit } from '@angular/core';
  import { CommonModule } from '@angular/common';
  import { FormsModule } from '@angular/forms';
  import { ActivatedRoute, RouterLink } from '@angular/router';
  import { LucideAngularModule } from 'lucide-angular';
  import { VenuesService } from '../../../core/services/venues.service';
  import { VenueBookingsService } from '../../../core/services/venue-bookings.service';
  import { AuthService } from '../../../core/services/auth.service';
  import { Venue } from '../../../core/models/venue.models';
  import { PaymentMethod } from '../../../core/models/payment.models';

  @Component({
    selector: 'app-venue-detail',
    standalone: true,
    imports: [CommonModule, RouterLink, FormsModule, LucideAngularModule],
    templateUrl: './venue-detail.component.html'
  })
  export class VenueDetailComponent implements OnInit {
    venue: Venue | null = null;
    loading = true;
    booking = false;

    startDate = '';
    endDate = '';
    notes = '';
    selectedPaymentMethod: PaymentMethod = 'Stripe';

    totalDays = 0;
    totalAmount = 0;

    successMessage = '';
    error = '';

    readonly paymentMethods: PaymentMethod[] = ['Stripe', 'AirtelMoney', 'MTNMoMo', 'Free'];

    constructor(
      private route: ActivatedRoute,
      private venuesService: VenuesService,
      private venueBookingsService: VenueBookingsService,
      public auth: AuthService
    ) {}

    ngOnInit(): void {
      const id = this.route.snapshot.paramMap.get('id')!;
      this.venuesService.getById(id).subscribe({
        next: data => { this.venue = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
    }

    onDatesChange(): void {
      if (!this.startDate || !this.endDate) { this.totalDays = 0; this.totalAmount = 0; return; }
      const start = new Date(this.startDate);
      const end = new Date(this.endDate);
      this.totalDays = Math.max(1, Math.floor((end.getTime() - start.getTime()) / 86400000));
      this.totalAmount = this.totalDays * (this.venue?.pricePerDay ?? 0);
    }

    book(): void {
      if (!this.venue || !this.startDate || !this.endDate) return;
      this.booking = true;
      this.error = '';
      this.venueBookingsService.create({
        venueId: this.venue.id,
        startDate: new Date(this.startDate).toISOString(),
        endDate: new Date(this.endDate).toISOString(),
        notes: this.notes || undefined,
        paymentMethod: this.selectedPaymentMethod
      }).subscribe({
        next: () => {
          this.booking = false;
          this.successMessage = 'Venue booked successfully!';
          this.startDate = ''; this.endDate = ''; this.notes = '';
          this.totalDays = 0; this.totalAmount = 0;
        },
        error: err => {
          this.booking = false;
          this.error = err.error?.message ?? 'Booking failed. Please try again.';
        }
      });
    }

    get amenitiesList(): string[] {
      return this.venue?.amenities
        ? this.venue.amenities.split(',').map(a => a.trim()).filter(Boolean)
        : [];
    }
  }
  ```

- [ ] **Step 2: Create the HTML template**

  Create `SmartEvents.UI/src/app/features/venues/venue-detail/venue-detail.component.html`:

  ```html
  <div class="venue-detail-page">
    @if (loading) {
      <div class="venue-detail-loading">
        <p>Loading venue...</p>
      </div>
    } @else if (!venue) {
      <div class="empty-state">
        <p>Venue not found.</p>
        <a routerLink="/venues" class="btn-secondary">&larr; Back to Venues</a>
      </div>
    } @else {
      <div class="venue-detail">
        <a routerLink="/venues" class="back-link">&larr; Back to Venues</a>

        @if (venue.imageUrl) {
          <img [src]="venue.imageUrl" [alt]="venue.name" class="venue-hero-image" />
        }

        <div class="venue-detail-header">
          <div>
            <span class="badge">{{ venue.type }}</span>
            <h1>{{ venue.name }}</h1>
            <p class="venue-location">
              <lucide-angular name="MapPin" [size]="14"></lucide-angular>
              {{ venue.city }}, {{ venue.country }} &middot; {{ venue.address }}
            </p>
            <p class="venue-capacity">Capacity: {{ venue.capacity | number }} people</p>
          </div>
          <div class="venue-price-block">
            @if (venue.pricePerDay) {
              <span class="venue-price">{{ venue.pricePerDay | currency:'ZMW':'symbol-narrow' }}<small>/day</small></span>
            } @else {
              <span class="venue-price">Free / On request</span>
            }
          </div>
        </div>

        @if (venue.description) {
          <p class="venue-description">{{ venue.description }}</p>
        }

        @if (amenitiesList.length > 0) {
          <div class="venue-amenities">
            <h3>Amenities</h3>
            <div class="amenities-tags">
              @for (amenity of amenitiesList; track amenity) {
                <span class="badge">{{ amenity }}</span>
              }
            </div>
          </div>
        }

        <div class="venue-booking-section">
          <h3>Book this Venue</h3>

          @if (!auth.isAuthenticated()) {
            <p class="venue-login-prompt">
              <a routerLink="/auth/login">Log in</a> to book this venue.
            </p>
          } @else if (successMessage) {
            <div class="venue-success">
              <lucide-angular name="CheckCircle" [size]="18"></lucide-angular>
              {{ successMessage }}
              <a routerLink="/venues/my-bookings" class="btn-secondary btn-sm">View My Bookings</a>
            </div>
          } @else {
            @if (error) {
              <p class="error">{{ error }}</p>
            }

            <div class="venue-booking-form">
              <div class="venue-booking-dates">
                <div class="booking-field">
                  <label>Start Date</label>
                  <input type="date" [(ngModel)]="startDate" (change)="onDatesChange()" />
                </div>
                <div class="booking-field">
                  <label>End Date</label>
                  <input type="date" [(ngModel)]="endDate" (change)="onDatesChange()" />
                </div>
              </div>

              <div class="booking-field">
                <label>Notes <span class="optional">(optional)</span></label>
                <textarea [(ngModel)]="notes" rows="2" placeholder="Purpose, requirements..."></textarea>
              </div>

              @if (totalDays > 0) {
                <div class="venue-booking-summary">
                  {{ totalDays }} day{{ totalDays > 1 ? 's' : '' }} &times;
                  {{ venue.pricePerDay | currency:'ZMW':'symbol-narrow' }} =
                  <strong>{{ totalAmount | currency:'ZMW':'symbol-narrow' }}</strong>
                </div>
              }

              <div class="booking-field">
                <label>Payment Method</label>
                <div class="checkout-methods">
                  @for (method of paymentMethods; track method) {
                    <button type="button"
                      class="method-btn"
                      [class.selected]="selectedPaymentMethod === method"
                      (click)="selectedPaymentMethod = method">
                      {{ method }}
                    </button>
                  }
                </div>
              </div>

              <button class="btn-primary"
                [disabled]="!startDate || !endDate || booking"
                (click)="book()">
                {{ booking ? 'Processing...' : 'Book & Pay' }}
              </button>
            </div>
          }
        </div>
      </div>
    }
  </div>
  ```

- [ ] **Step 3: Build to check for TypeScript errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -15
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.UI/src/app/features/venues/venue-detail/
  git commit -m "feat: add VenueDetailComponent with inline booking form"
  ```

---

## Task 6: MyVenueBookingsComponent

**Files:**
- Create: `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.ts`
- Create: `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.html`

- [ ] **Step 1: Create the TypeScript component**

  Create `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.ts`:

  ```typescript
  import { Component, OnInit } from '@angular/core';
  import { CommonModule } from '@angular/common';
  import { RouterLink } from '@angular/router';
  import { VenueBookingsService } from '../../../core/services/venue-bookings.service';
  import { VenueBooking } from '../../../core/models/venue.models';
  import { SkeletonComponent } from '../../../core/components/skeleton/skeleton.component';

  @Component({
    selector: 'app-my-venue-bookings',
    standalone: true,
    imports: [CommonModule, RouterLink, SkeletonComponent],
    templateUrl: './my-venue-bookings.component.html'
  })
  export class MyVenueBookingsComponent implements OnInit {
    bookings: VenueBooking[] = [];
    loading = true;
    cancelling: string | null = null;
    error = '';

    constructor(private venueBookingsService: VenueBookingsService) {}

    ngOnInit(): void {
      this.load();
    }

    load(): void {
      this.loading = true;
      this.venueBookingsService.getMy().subscribe({
        next: data => { this.bookings = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
    }

    cancel(id: string): void {
      this.cancelling = id;
      this.error = '';
      this.venueBookingsService.cancel(id).subscribe({
        next: () => {
          this.cancelling = null;
          const b = this.bookings.find(b => b.id === id);
          if (b) b.status = 'Cancelled';
        },
        error: err => {
          this.cancelling = null;
          this.error = err.error?.message ?? 'Cancel failed. Please try again.';
        }
      });
    }
  }
  ```

- [ ] **Step 2: Create the HTML template**

  Create `SmartEvents.UI/src/app/features/venues/my-venue-bookings/my-venue-bookings.component.html`:

  ```html
  <div class="my-bookings-page">
    <div class="my-bookings-header">
      <h1>My Venue Bookings</h1>
      <a routerLink="/venues" class="btn-secondary">&larr; Browse Venues</a>
    </div>

    @if (error) {
      <p class="error">{{ error }}</p>
    }

    @if (loading) {
      <app-skeleton [count]="4" />
    } @else if (bookings.length === 0) {
      <div class="empty-state">
        <p>You haven't booked any venues yet.</p>
        <a routerLink="/venues" class="btn-primary">Browse Venues</a>
      </div>
    } @else {
      <div class="bookings-list">
        @for (booking of bookings; track booking.id) {
          <div class="booking-card" [class.booking-cancelled]="booking.status === 'Cancelled'">
            <div class="booking-card-info">
              <h3>{{ booking.venueName }}</h3>
              <p class="booking-location">{{ booking.venueCity }} &middot; {{ booking.venueAddress }}</p>
              <p class="booking-dates-range">
                {{ booking.startDate | date:'mediumDate' }} &rarr; {{ booking.endDate | date:'mediumDate' }}
              </p>
              @if (booking.notes) {
                <p class="booking-notes">{{ booking.notes }}</p>
              }
            </div>
            <div class="booking-card-aside">
              <span class="badge"
                [class.badge-confirmed]="booking.status === 'Confirmed'"
                [class.badge-cancelled]="booking.status === 'Cancelled'">
                {{ booking.status }}
              </span>
              <p class="booking-amount">{{ booking.totalAmount | currency:'ZMW':'symbol-narrow' }}</p>
              @if (booking.status === 'Confirmed') {
                <button class="btn-danger btn-sm"
                  [disabled]="cancelling === booking.id"
                  (click)="cancel(booking.id)">
                  {{ cancelling === booking.id ? 'Cancelling...' : 'Cancel' }}
                </button>
              }
            </div>
          </div>
        }
      </div>
    }
  </div>
  ```

- [ ] **Step 3: Build to check for TypeScript errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -15
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.UI/src/app/features/venues/my-venue-bookings/
  git commit -m "feat: add MyVenueBookingsComponent"
  ```

---

## Task 7: Routes + nav + venues-list link + styles

**Files:**
- Modify: `SmartEvents.UI/src/app/features/venues/venues.routes.ts`
- Modify: `SmartEvents.UI/src/app/features/venues/venues-list/venues-list.component.html`
- Modify: `SmartEvents.UI/src/app/features/dashboard/dashboard.component.html`
- Modify: `SmartEvents.UI/src/styles.scss`

- [ ] **Step 1: Update venues.routes.ts**

  Replace the full contents of `SmartEvents.UI/src/app/features/venues/venues.routes.ts`:

  ```typescript
  import { Routes } from '@angular/router';
  import { authGuard } from '../../core/guards/auth.guard';

  export const VENUES_ROUTES: Routes = [
    {
      path: '',
      loadComponent: () => import('./venues-list/venues-list.component').then(m => m.VenuesListComponent)
    },
    {
      path: 'create',
      canActivate: [authGuard],
      loadComponent: () => import('./create-venue/create-venue.component').then(m => m.CreateVenueComponent)
    },
    {
      path: 'my-bookings',
      canActivate: [authGuard],
      loadComponent: () => import('./my-venue-bookings/my-venue-bookings.component').then(m => m.MyVenueBookingsComponent)
    },
    {
      path: ':id',
      loadComponent: () => import('./venue-detail/venue-detail.component').then(m => m.VenueDetailComponent)
    },
    {
      path: ':id/edit',
      canActivate: [authGuard],
      loadComponent: () => import('./edit-venue/edit-venue.component').then(m => m.EditVenueComponent)
    }
  ];
  ```

  **Critical:** `my-bookings` must appear before `:id` — Angular matches routes top-down, and `:id` would capture the literal string "my-bookings" if it came first.

- [ ] **Step 2: Add "View Details" link in venues-list.component.html**

  In `venues-list.component.html`, find the closing `</div>` of `.venue-card-body` (around line 47) and the existing `@if (isOrganizer)` block. Add a "View Details" link so the card always has a detail link:

  ```html
          <p class="company">Listed by {{ venue.companyName }}</p>
          @if (venue.amenities) {
            <p class="amenities">{{ venue.amenities }}</p>
          }
        </div>
        <div class="venue-card-footer">
          <a [routerLink]="['/venues', venue.id]" class="btn-secondary btn-sm">View Details</a>
          @if (isOrganizer) {
            <a [routerLink]="['/venues', venue.id, 'edit']" class="btn-secondary btn-sm">Edit</a>
          }
        </div>
  ```

  This replaces the existing `@if (isOrganizer)` block:
  ```html
          @if (isOrganizer) {
            <div class="venue-actions">
              <a [routerLink]="['/venues', venue.id, 'edit']" class="btn-secondary">Edit</a>
            </div>
          }
  ```

- [ ] **Step 3: Add "My Bookings" link to dashboard nav**

  In `SmartEvents.UI/src/app/features/dashboard/dashboard.component.html`, find the nav `<a routerLink="/venues"` line and add the My Bookings link immediately after it:

  ```html
      <a routerLink="/venues" routerLinkActive="active">
        <lucide-icon name="MapPin" [size]="16" /> Venues
      </a>
      <a routerLink="/venues/my-bookings" routerLinkActive="active">
        <lucide-icon name="BookMarked" [size]="16" /> My Bookings
      </a>
  ```

- [ ] **Step 4: Add venue booking styles to styles.scss**

  Find the line `/* ─── Recommendations ───` in `styles.scss` (around line 513). Insert the following block **before** it:

  ```scss
  /* ─── Venue Detail ───────────────────────────────────────── */
  .venue-detail-page { max-width: 860px; margin: 0 auto; padding: 1.5rem; }

  .venue-detail { display: flex; flex-direction: column; gap: 1.25rem; }

  .back-link { font-size: .85rem; color: var(--text-muted); text-decoration: none; &:hover { color: var(--primary); } }

  .venue-hero-image { width: 100%; max-height: 320px; object-fit: cover; border-radius: var(--radius-lg); }

  .venue-detail-header {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;
    h1 { margin: .3rem 0 .5rem; }
  }

  .venue-location { font-size: .85rem; color: var(--text-muted); display: flex; align-items: center; gap: .3rem; margin: 0; }
  .venue-capacity { font-size: .85rem; color: var(--text-muted); margin: .25rem 0 0; }

  .venue-price-block { text-align: right; flex-shrink: 0; }
  .venue-price { font-size: 1.4rem; font-weight: 700; color: var(--primary); small { font-size: .75rem; font-weight: 400; color: var(--text-muted); } }

  .venue-description { font-size: .9rem; color: var(--text-muted); line-height: 1.6; margin: 0; }

  .venue-amenities {
    h3 { margin: 0 0 .6rem; font-size: .95rem; }
  }
  .amenities-tags { display: flex; flex-wrap: wrap; gap: .4rem; }

  /* ─── Venue Booking Form ─────────────────────────────────── */
  .venue-booking-section {
    border-top: 1px solid var(--border); padding-top: 1.5rem;
    h3 { margin: 0 0 1rem; }
  }

  .venue-login-prompt { font-size: .9rem; a { color: var(--primary); } }

  .venue-success {
    display: flex; align-items: center; gap: .6rem; flex-wrap: wrap;
    background: var(--success-light); border-radius: var(--radius); padding: .75rem 1rem;
    font-size: .9rem; color: #166534;
  }

  .venue-booking-form { display: flex; flex-direction: column; gap: 1rem; max-width: 520px; }

  .venue-booking-dates { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }

  .booking-field {
    display: flex; flex-direction: column; gap: .3rem;
    label { font-size: .85rem; font-weight: 500; }
    .optional { font-weight: 400; color: var(--text-muted); }
    input, textarea { padding: .5rem .75rem; border: 1px solid var(--border); border-radius: var(--radius); font-size: .9rem; background: var(--white); color: var(--text); width: 100%; box-sizing: border-box; }
    textarea { resize: vertical; }
  }

  .venue-booking-summary {
    background: var(--surface); border-radius: var(--radius); padding: .6rem .9rem;
    font-size: .9rem; color: var(--text-muted);
    strong { color: var(--primary); }
  }

  .checkout-methods { display: flex; gap: .5rem; flex-wrap: wrap; }

  .method-btn {
    padding: .4rem .9rem; border-radius: var(--radius); border: 1.5px solid var(--border);
    background: var(--white); color: var(--text); font-size: .8rem; cursor: pointer;
    transition: border-color var(--transition), background var(--transition);
    &.selected { border-color: var(--primary); background: var(--primary-light); color: var(--primary-dark); }
    &:hover:not(.selected) { border-color: var(--primary); }
  }

  /* ─── My Venue Bookings ──────────────────────────────────── */
  .my-bookings-page { max-width: 860px; margin: 0 auto; padding: 1.5rem; }

  .my-bookings-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; h1 { margin: 0; } }

  .bookings-list { display: flex; flex-direction: column; gap: 1rem; }

  .booking-card {
    display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;
    padding: 1.1rem 1.25rem; border-radius: var(--radius-lg);
    border: 1.5px solid var(--border); background: var(--white); box-shadow: var(--shadow-sm);
    transition: box-shadow var(--transition);
    &:hover { box-shadow: var(--shadow); }
    &.booking-cancelled { opacity: .65; }
  }

  .booking-card-info {
    flex: 1;
    h3 { margin: 0 0 .25rem; font-size: 1rem; }
  }
  .booking-location { font-size: .8rem; color: var(--text-muted); margin: 0 0 .25rem; }
  .booking-dates-range { font-size: .85rem; margin: 0 0 .25rem; }
  .booking-notes { font-size: .8rem; color: var(--text-muted); margin: .25rem 0 0; font-style: italic; }

  .booking-card-aside { display: flex; flex-direction: column; align-items: flex-end; gap: .5rem; flex-shrink: 0; }
  .booking-amount { font-weight: 700; color: var(--primary); margin: 0; font-size: .95rem; }

  .venue-card-footer { display: flex; gap: .5rem; padding: .75rem 1rem; border-top: 1px solid var(--border); }

  ```

  Then inside the `@media (max-width: 640px)` block near the bottom of `styles.scss`, add:

  ```scss
    .venue-booking-dates { grid-template-columns: 1fr; }
    .venue-detail-header { flex-direction: column; }
    .booking-card { flex-direction: column; }
    .booking-card-aside { align-items: flex-start; }
  ```

- [ ] **Step 5: Build to check for errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -15
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 6: Commit**

  ```bash
  git add SmartEvents.UI/src/app/features/venues/venues.routes.ts \
          SmartEvents.UI/src/app/features/venues/venues-list/venues-list.component.html \
          SmartEvents.UI/src/app/features/dashboard/dashboard.component.html \
          SmartEvents.UI/src/styles.scss
  git commit -m "feat: add venue detail route, nav link, card link, and booking styles"
  ```

---

## Task 8: End-to-end verification + CLAUDE.md

- [ ] **Step 1: Start the API**

  ```bash
  cd SmartEvents.API && dotnet run &
  sleep 8
  ```

- [ ] **Step 2: Smoke-test the booking endpoint**

  ```bash
  TOKEN=$(curl -s -X POST http://localhost:5148/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"attendee@example.com","password":"Seed1234!"}' \
    | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

  # Get a venue ID
  VENUE_ID=$(curl -s http://localhost:5148/api/venues \
    | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
  echo "Venue ID: $VENUE_ID"

  # Book the venue
  curl -s -X POST http://localhost:5148/api/venue-bookings \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{\"venueId\":\"$VENUE_ID\",\"startDate\":\"2026-08-01T00:00:00Z\",\"endDate\":\"2026-08-03T00:00:00Z\",\"paymentMethod\":\"Stripe\"}" \
    | head -c 400

  echo ""

  # List my bookings
  curl -s http://localhost:5148/api/venue-bookings/my \
    -H "Authorization: Bearer $TOKEN" | head -c 400
  ```

  Expected: first call returns a `VenueBookingResponse` with `"status":"Confirmed"`, second returns an array with that booking.

  ```bash
  # Test conflict detection — same dates, same venue
  curl -s -X POST http://localhost:5148/api/venue-bookings \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{\"venueId\":\"$VENUE_ID\",\"startDate\":\"2026-08-01T00:00:00Z\",\"endDate\":\"2026-08-03T00:00:00Z\",\"paymentMethod\":\"Stripe\"}" \
    | head -c 200
  ```

  Expected: `409 Conflict` with `"message":"Venue is already booked for those dates."`.

  ```bash
  kill $(lsof -ti:5148) 2>/dev/null || true
  ```

- [ ] **Step 3: Update CLAUDE.md change log**

  Open `CLAUDE.md` at the repo root. In the Change Log table, add:

  ```markdown
  | 2026-06-15 | Fixed sticky navbar (dashboard-nav position:sticky). Fixed event recommendations by seeding 3 more events in overlapping categories. Added venue booking: VenueBooking entity, VenueBookingsController, VenueDetailComponent, MyVenueBookingsComponent. Added company member management: GET/POST/PUT/DELETE /api/companies/{id}/members, CompanyMembersComponent at /companies/:id/members. |
  ```

- [ ] **Step 4: Final commit**

  ```bash
  git add CLAUDE.md
  git commit -m "docs: update CLAUDE.md with all 2026-06-15 changes"
  ```

- [ ] **Step 5: Final git log**

  ```bash
  git log --oneline -16
  ```

---

## Task 9: Fix sticky navbar

**Files:**
- Modify: `SmartEvents.UI/src/styles.scss`

The `dashboard-nav` disappears when the user scrolls because it has no sticky positioning. The fix is two lines of CSS.

- [ ] **Step 1: Add sticky positioning to .dashboard-nav**

  In `styles.scss`, find the `.dashboard-nav` rule (around line 329). It currently starts with:

  ```scss
  .dashboard-nav {
    background: var(--white); border-bottom: 1px solid var(--border);
    padding: 0 1.75rem; display: flex; gap: .1rem; overflow-x: auto;
  ```

  Replace that opening with:

  ```scss
  .dashboard-nav {
    background: var(--white); border-bottom: 1px solid var(--border);
    padding: 0 1.75rem; display: flex; gap: .1rem; overflow-x: auto;
    position: sticky; top: 0; z-index: 100;
  ```

- [ ] **Step 2: Build to verify no errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -10
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 3: Commit**

  ```bash
  git add SmartEvents.UI/src/styles.scss
  git commit -m "fix: make dashboard nav sticky on scroll"
  ```

---

## Task 10: Fix event recommendations — seed more events

**Files:**
- Modify: `SmartEvents.API/Infrastructure/Data/DbSeeder.cs`

The recommendations endpoint works correctly, but the seeded data has only one event per category, so there is nothing to recommend after registering. The fix is to seed 3 additional events in categories that already exist (`Conference`, `Exhibition`, `Workshop`) so recommendations have candidates to return.

- [ ] **Step 1: Open the seeder and locate the events block**

  Open `SmartEvents.API/Infrastructure/Data/DbSeeder.cs`. Find the line:

  ```csharp
  db.Events.AddRange(devSummit, angularWorkshop, afrikaFestConcert, networkingBrunch, aiWebinar, startupExpo);
  ```

- [ ] **Step 2: Add three new seeded events before that AddRange call**

  Insert the following three events immediately before the `db.Events.AddRange(...)` line. The existing company and organizer variables (`techCo`, `afrikaFest`, `organizer1`, `organizer2`) and venue variables (`bicc`, `sunbird`, `blantyreSports`) are already declared earlier in the seeder.

  ```csharp
  var womenInTechSummit = new Event
  {
      Id           = Guid.NewGuid(),
      Title        = "Women in Tech Malawi Summit",
      Slug         = "women-in-tech-malawi-summit-2026",
      Description  = "A one-day conference celebrating and empowering women in technology across Malawi. Talks, panels, and networking with industry leaders.",
      Category     = EventCategory.Conference,
      Status       = EventStatus.Published,
      StartDate    = now.AddDays(50),
      EndDate      = now.AddDays(50).AddHours(8),
      Timezone     = "Africa/Blantyre",
      MaxAttendees = 300,
      IsTicketed   = true,
      TicketPrice  = 5000,
      WaitlistEnabled = true,
      IsPublic     = true,
      Tags         = "women,tech,diversity,conference",
      CompanyId    = afrikaFest.Id,
      VenueId      = sunbird.Id,
      OrganizerId  = organizer2.Id
  };

  var cloudEastAfricaConf = new Event
  {
      Id           = Guid.NewGuid(),
      Title        = "Cloud Computing East Africa 2026",
      Slug         = "cloud-computing-east-africa-2026",
      Description  = "Regional conference covering AWS, Azure, and GCP adoption across East and Central Africa. Case studies, workshops, and certification prep sessions.",
      Category     = EventCategory.Conference,
      Status       = EventStatus.Published,
      StartDate    = now.AddDays(80),
      EndDate      = now.AddDays(81),
      Timezone     = "Africa/Blantyre",
      MaxAttendees = 250,
      IsTicketed   = true,
      TicketPrice  = 9500,
      WaitlistEnabled = false,
      IsPublic     = true,
      Tags         = "cloud,aws,azure,gcp,conference",
      CompanyId    = techCo.Id,
      VenueId      = bicc.Id,
      OrganizerId  = organizer1.Id
  };

  var afrikaFestExhibition = new Event
  {
      Id           = Guid.NewGuid(),
      Title        = "AfrikaFest Arts & Culture Exhibition",
      Slug         = "afrikafest-arts-culture-exhibition-2026",
      Description  = "A vibrant exhibition showcasing Malawian and African art, crafts, photography, and heritage. Open to all ages and free to attend.",
      Category     = EventCategory.Exhibition,
      Status       = EventStatus.Published,
      StartDate    = now.AddDays(95),
      EndDate      = now.AddDays(97),
      Timezone     = "Africa/Blantyre",
      MaxAttendees = 2000,
      IsTicketed   = false,
      WaitlistEnabled = false,
      IsPublic     = true,
      Tags         = "art,culture,heritage,exhibition,free",
      CompanyId    = afrikaFest.Id,
      VenueId      = blantyreSports.Id,
      OrganizerId  = organizer2.Id
  };
  ```

- [ ] **Step 3: Add the new events to the AddRange call**

  Replace:

  ```csharp
  db.Events.AddRange(devSummit, angularWorkshop, afrikaFestConcert, networkingBrunch, aiWebinar, startupExpo);
  ```

  With:

  ```csharp
  db.Events.AddRange(devSummit, angularWorkshop, afrikaFestConcert, networkingBrunch, aiWebinar, startupExpo,
      womenInTechSummit, cloudEastAfricaConf, afrikaFestExhibition);
  ```

- [ ] **Step 4: Build the API**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -10
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Verify recommendations work**

  ```bash
  cd SmartEvents.API && dotnet run &
  sleep 8

  # Login as attendee
  TOKEN=$(curl -s -X POST http://localhost:5148/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"attendee@example.com","password":"Seed1234!"}' \
    | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

  # Get the devSummit event ID (Conference category)
  SUMMIT_ID=$(curl -s "http://localhost:5148/api/events?page=1&pageSize=20" \
    | grep -o '"slug":"dev-summit[^"]*"' | head -1 | cut -d'"' -f4)
  echo "Dev Summit slug: $SUMMIT_ID"

  # Actually get the ID from the slug
  SUMMIT_ID=$(curl -s "http://localhost:5148/api/events/dev-summit-malawi-2026" \
    | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
  echo "Dev Summit ID: $SUMMIT_ID"

  # Register for devSummit
  curl -s -X POST http://localhost:5148/api/registrations \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{\"eventId\":\"$SUMMIT_ID\"}" | grep -o '"status":"[^"]*"'

  # Get recommendations excluding devSummit — should return Conference events
  curl -s "http://localhost:5148/api/events/recommended?excludeEventId=$SUMMIT_ID" \
    -H "Authorization: Bearer $TOKEN" | grep -o '"title":"[^"]*"'
  ```

  Expected: at least `"title":"Women in Tech Malawi Summit"` and `"title":"Cloud Computing East Africa 2026"` in the response.

  ```bash
  kill $(lsof -ti:5148) 2>/dev/null || true
  ```

- [ ] **Step 6: Commit**

  ```bash
  git add SmartEvents.API/Infrastructure/Data/DbSeeder.cs
  git commit -m "fix: seed more events in overlapping categories for recommendations"
  ```

---

## Task 11: Company member management — backend

**Files:**
- Modify: `SmartEvents.API/Application/DTOs/CompanyDtos.cs`
- Modify: `SmartEvents.API/Controllers/CompaniesController.cs`

CompanyAdmins and SuperAdmins need to add, change the role of, and remove users from their company. Members are existing users found by email.

- [ ] **Step 1: Add member DTOs to CompanyDtos.cs**

  Open `SmartEvents.API/Application/DTOs/CompanyDtos.cs`. Add `using SmartEvents.API.Domain.Enums;` at the top if not already present, then append to the end of the file:

  ```csharp
  public record AddCompanyMemberRequest(
      [Required, EmailAddress] string Email,
      UserRole Role
  );

  public record UpdateMemberRoleRequest(UserRole Role);

  public record CompanyMemberResponse(
      Guid Id,
      string Email,
      string FirstName,
      string LastName,
      UserRole Role,
      bool IsActive
  );
  ```

- [ ] **Step 2: Add 4 member endpoints to CompaniesController**

  Open `SmartEvents.API/Controllers/CompaniesController.cs`. Before the closing `}` of the class, add:

  ```csharp
  [HttpGet("{id:guid}/members")]
  [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
  public async Task<ActionResult<IEnumerable<CompanyMemberResponse>>> GetMembers(Guid id)
  {
      var company = await db.Companies.FindAsync(id);
      if (company is null) return NotFound();

      var members = await db.Users
          .Where(u => u.CompanyId == id)
          .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
          .ToListAsync();

      return Ok(members.Select(ToMemberResponse));
  }

  [HttpPost("{id:guid}/members")]
  [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
  public async Task<ActionResult<CompanyMemberResponse>> AddMember(Guid id, AddCompanyMemberRequest request)
  {
      var company = await db.Companies.FindAsync(id);
      if (company is null) return NotFound(new { message = "Company not found." });

      var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());
      if (user is null)
          return NotFound(new { message = "No account found with that email address." });

      if (user.CompanyId.HasValue && user.CompanyId != id)
          return Conflict(new { message = "User already belongs to another company." });

      user.CompanyId = id;
      user.Role = request.Role;
      await db.SaveChangesAsync();

      return Ok(ToMemberResponse(user));
  }

  [HttpPut("{id:guid}/members/{userId:guid}")]
  [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
  public async Task<ActionResult<CompanyMemberResponse>> UpdateMemberRole(Guid id, Guid userId, UpdateMemberRoleRequest request)
  {
      var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == id);
      if (user is null) return NotFound();

      user.Role = request.Role;
      await db.SaveChangesAsync();

      return Ok(ToMemberResponse(user));
  }

  [HttpDelete("{id:guid}/members/{userId:guid}")]
  [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
  public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
  {
      var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

      var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == id);
      if (user is null) return NotFound();
      if (user.Id == callerId)
          return BadRequest(new { message = "You cannot remove yourself from the company." });

      user.CompanyId = null;
      user.Role = UserRole.Attendee;
      await db.SaveChangesAsync();

      return NoContent();
  }

  private static CompanyMemberResponse ToMemberResponse(User u) =>
      new(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive);
  ```

- [ ] **Step 3: Build the API**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -10
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Smoke-test**

  ```bash
  cd SmartEvents.API && dotnet run &
  sleep 8

  # Login as CompanyAdmin
  ADMIN_TOKEN=$(curl -s -X POST http://localhost:5148/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@techevents.co","password":"Seed1234!"}' \
    | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

  # Get company ID from the token claims via profile
  COMPANY_ID=$(curl -s http://localhost:5148/api/auth/me \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    | grep -o '"companyId":"[^"]*"' | cut -d'"' -f4)
  echo "Company ID: $COMPANY_ID"

  # List members
  curl -s "http://localhost:5148/api/companies/$COMPANY_ID/members" \
    -H "Authorization: Bearer $ADMIN_TOKEN" | grep -o '"email":"[^"]*"'

  # Add attendee to the company as Organizer
  curl -s -X POST "http://localhost:5148/api/companies/$COMPANY_ID/members" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    -d '{"email":"attendee@example.com","role":"Organizer"}' | grep -o '"role":"[^"]*"'

  kill $(lsof -ti:5148) 2>/dev/null || true
  ```

  Expected: list returns existing members, add call returns `"role":"Organizer"`.

- [ ] **Step 5: Commit**

  ```bash
  git add SmartEvents.API/Application/DTOs/CompanyDtos.cs \
          SmartEvents.API/Controllers/CompaniesController.cs
  git commit -m "feat: add company member management endpoints"
  ```

---

## Task 12: Company member management — frontend

**Files:**
- Create: `SmartEvents.UI/src/app/core/services/company-members.service.ts`
- Create: `SmartEvents.UI/src/app/features/companies/company-members/company-members.component.ts`
- Create: `SmartEvents.UI/src/app/features/companies/company-members/company-members.component.html`
- Modify: `SmartEvents.UI/src/app/features/companies/companies.routes.ts`
- Modify: `SmartEvents.UI/src/styles.scss`

- [ ] **Step 1: Create CompanyMembersService**

  Create `SmartEvents.UI/src/app/core/services/company-members.service.ts`:

  ```typescript
  import { Injectable } from '@angular/core';
  import { HttpClient } from '@angular/common/http';
  import { environment } from '../../../environments/environment';
  import { UserRole } from '../models/user.models';

  export interface CompanyMember {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: UserRole;
    isActive: boolean;
  }

  export interface AddMemberRequest {
    email: string;
    role: UserRole;
  }

  @Injectable({ providedIn: 'root' })
  export class CompanyMembersService {
    private url(companyId: string) {
      return `${environment.apiUrl}/companies/${companyId}/members`;
    }

    constructor(private http: HttpClient) {}

    getMembers(companyId: string) {
      return this.http.get<CompanyMember[]>(this.url(companyId));
    }

    addMember(companyId: string, request: AddMemberRequest) {
      return this.http.post<CompanyMember>(this.url(companyId), request);
    }

    updateRole(companyId: string, userId: string, role: UserRole) {
      return this.http.put<CompanyMember>(`${this.url(companyId)}/${userId}`, { role });
    }

    removeMember(companyId: string, userId: string) {
      return this.http.delete<void>(`${this.url(companyId)}/${userId}`);
    }
  }
  ```

  **Note:** `UserRole` is imported from `user.models.ts`. Check that file first. If `UserRole` is not already exported as a type there, add:
  ```typescript
  export type UserRole = 'SuperAdmin' | 'CompanyAdmin' | 'Organizer' | 'Attendee';
  ```

- [ ] **Step 2: Check user.models.ts for UserRole**

  ```bash
  grep -n "UserRole" SmartEvents.UI/src/app/core/models/user.models.ts
  ```

  If `UserRole` is already defined, use the import path shown. If not, add the type to `user.models.ts`.

- [ ] **Step 3: Create CompanyMembersComponent TypeScript**

  Create directory `SmartEvents.UI/src/app/features/companies/company-members/` then create `company-members.component.ts`:

  ```typescript
  import { Component, OnInit } from '@angular/core';
  import { CommonModule } from '@angular/common';
  import { FormsModule } from '@angular/forms';
  import { ActivatedRoute, RouterLink } from '@angular/router';
  import { CompanyMembersService, CompanyMember, AddMemberRequest } from '../../../core/services/company-members.service';
  import { UserRole } from '../../../core/models/user.models';
  import { SkeletonComponent } from '../../../core/components/skeleton/skeleton.component';

  @Component({
    selector: 'app-company-members',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink, SkeletonComponent],
    templateUrl: './company-members.component.html'
  })
  export class CompanyMembersComponent implements OnInit {
    companyId = '';
    members: CompanyMember[] = [];
    loading = true;
    adding = false;
    removing: string | null = null;

    newEmail = '';
    newRole: UserRole = 'Organizer';
    error = '';
    success = '';

    readonly assignableRoles: UserRole[] = ['CompanyAdmin', 'Organizer', 'Attendee'];

    constructor(
      private route: ActivatedRoute,
      private membersService: CompanyMembersService
    ) {}

    ngOnInit(): void {
      this.companyId = this.route.snapshot.paramMap.get('id')!;
      this.load();
    }

    load(): void {
      this.loading = true;
      this.membersService.getMembers(this.companyId).subscribe({
        next: data => { this.members = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
    }

    addMember(): void {
      if (!this.newEmail) return;
      this.adding = true;
      this.error = '';
      this.success = '';
      this.membersService.addMember(this.companyId, { email: this.newEmail, role: this.newRole }).subscribe({
        next: member => {
          this.adding = false;
          this.success = `${member.firstName} ${member.lastName} added as ${member.role}.`;
          this.newEmail = '';
          const existing = this.members.findIndex(m => m.id === member.id);
          if (existing >= 0) this.members[existing] = member;
          else this.members = [...this.members, member];
        },
        error: err => {
          this.adding = false;
          this.error = err.error?.message ?? 'Failed to add member.';
        }
      });
    }

    changeRole(member: CompanyMember, role: UserRole): void {
      this.membersService.updateRole(this.companyId, member.id, role).subscribe({
        next: updated => {
          const idx = this.members.findIndex(m => m.id === updated.id);
          if (idx >= 0) this.members[idx] = updated;
        },
        error: err => { this.error = err.error?.message ?? 'Failed to update role.'; }
      });
    }

    remove(id: string): void {
      this.removing = id;
      this.error = '';
      this.membersService.removeMember(this.companyId, id).subscribe({
        next: () => {
          this.removing = null;
          this.members = this.members.filter(m => m.id !== id);
        },
        error: err => {
          this.removing = null;
          this.error = err.error?.message ?? 'Failed to remove member.';
        }
      });
    }
  }
  ```

- [ ] **Step 4: Create CompanyMembersComponent HTML**

  Create `SmartEvents.UI/src/app/features/companies/company-members/company-members.component.html`:

  ```html
  <div class="members-page">
    <div class="members-header">
      <h1>Company Members</h1>
      <a [routerLink]="['/companies', companyId, 'edit']" class="btn-secondary">&larr; Back to Company</a>
    </div>

    @if (error) { <p class="error">{{ error }}</p> }
    @if (success) { <p class="success-msg">{{ success }}</p> }

    <div class="add-member-form">
      <h3>Add Member</h3>
      <div class="add-member-fields">
        <input type="email" [(ngModel)]="newEmail" placeholder="member@example.com" />
        <select [(ngModel)]="newRole">
          @for (role of assignableRoles; track role) {
            <option [value]="role">{{ role }}</option>
          }
        </select>
        <button class="btn-primary" [disabled]="!newEmail || adding" (click)="addMember()">
          {{ adding ? 'Adding...' : 'Add Member' }}
        </button>
      </div>
    </div>

    @if (loading) {
      <app-skeleton [count]="4" />
    } @else if (members.length === 0) {
      <div class="empty-state">
        <p>No members yet. Add the first one above.</p>
      </div>
    } @else {
      <div class="members-list">
        @for (member of members; track member.id) {
          <div class="member-card">
            <div class="member-avatar">{{ member.firstName[0] }}{{ member.lastName[0] }}</div>
            <div class="member-info">
              <p class="member-name">{{ member.firstName }} {{ member.lastName }}</p>
              <p class="member-email">{{ member.email }}</p>
            </div>
            <div class="member-actions">
              <select [ngModel]="member.role" (ngModelChange)="changeRole(member, $event)">
                @for (role of assignableRoles; track role) {
                  <option [value]="role">{{ role }}</option>
                }
              </select>
              <button class="btn-danger btn-sm"
                [disabled]="removing === member.id"
                (click)="remove(member.id)">
                {{ removing === member.id ? '...' : 'Remove' }}
              </button>
            </div>
          </div>
        }
      </div>
    }
  </div>
  ```

- [ ] **Step 5: Add the members route to companies.routes.ts**

  Open `SmartEvents.UI/src/app/features/companies/companies.routes.ts`. Add a new route for `/:id/members` (before the catch-all or at the end of the routes array):

  ```typescript
  {
    path: ':id/members',
    canActivate: [authGuard],
    loadComponent: () => import('./company-members/company-members.component').then(m => m.CompanyMembersComponent)
  },
  ```

- [ ] **Step 6: Add a "Manage Members" link in edit-company**

  Open `SmartEvents.UI/src/app/features/companies/edit-company/edit-company.component.html`. Find the page header or action buttons area and add a link to the members page:

  ```html
  <a [routerLink]="['/companies', companyId, 'members']" class="btn-secondary">Manage Members</a>
  ```

  The exact placement depends on the current template — add it near the top alongside any existing action buttons or after the form heading.

- [ ] **Step 7: Add member management styles to styles.scss**

  Find the `/* ─── Venue Detail */` comment in `styles.scss` and insert the following block **before** it:

  ```scss
  /* ─── Company Members ────────────────────────────────────── */
  .members-page { max-width: 720px; margin: 0 auto; padding: 1.5rem; }

  .members-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; h1 { margin: 0; } }

  .add-member-form {
    background: var(--surface); border-radius: var(--radius-lg); padding: 1.25rem;
    margin-bottom: 1.5rem; border: 1px solid var(--border);
    h3 { margin: 0 0 .75rem; font-size: .95rem; }
  }

  .add-member-fields {
    display: flex; gap: .6rem; flex-wrap: wrap;
    input, select { padding: .5rem .75rem; border: 1px solid var(--border); border-radius: var(--radius); font-size: .875rem; background: var(--white); color: var(--text); }
    input { flex: 1; min-width: 200px; }
  }

  .members-list { display: flex; flex-direction: column; gap: .6rem; }

  .member-card {
    display: flex; align-items: center; gap: 1rem;
    padding: .85rem 1rem; border-radius: var(--radius-lg);
    border: 1px solid var(--border); background: var(--white); box-shadow: var(--shadow-sm);
  }

  .member-avatar {
    width: 38px; height: 38px; border-radius: 50%; background: var(--primary-light);
    color: var(--primary-dark); font-weight: 700; font-size: .85rem;
    display: flex; align-items: center; justify-content: center; flex-shrink: 0;
  }

  .member-info { flex: 1; }
  .member-name { font-weight: 600; font-size: .9rem; margin: 0; }
  .member-email { font-size: .8rem; color: var(--text-muted); margin: 0; }

  .member-actions { display: flex; align-items: center; gap: .5rem; flex-shrink: 0;
    select { padding: .35rem .6rem; border: 1px solid var(--border); border-radius: var(--radius); font-size: .8rem; background: var(--white); color: var(--text); }
  }

  @media (max-width: 640px) {
    .member-card { flex-wrap: wrap; }
    .member-actions { width: 100%; justify-content: flex-end; }
    .add-member-fields { flex-direction: column; input { min-width: unset; } }
  }
  ```

- [ ] **Step 8: Build to check for TypeScript errors**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -15
  ```

  Expected: `Application bundle generation complete`, 0 errors.

- [ ] **Step 9: Commit**

  ```bash
  git add SmartEvents.UI/src/app/core/services/company-members.service.ts \
          SmartEvents.UI/src/app/features/companies/company-members/ \
          SmartEvents.UI/src/app/features/companies/companies.routes.ts \
          SmartEvents.UI/src/app/features/companies/edit-company/edit-company.component.html \
          SmartEvents.UI/src/styles.scss
  git commit -m "feat: add company member management UI"
  ```
