# SmartEvents — Claude Context

Update this file whenever a meaningful change is made to the application (new feature, schema change, new route, new service, architecture decision). Each chat session reads this first.

---

## What This Project Is

SmartEvents is a full-stack event management platform built for Malawi. It supports multi-company event creation, ticketing with QR code check-in, mock payments (Stripe / Airtel Money / MTN MoMo), and real-time SignalR notifications. Currency is ZMW (Zambian Kwacha). Seeded accounts and events are available for development.

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Backend | ASP.NET Core Web API | .NET 10 |
| Frontend | Angular SPA (standalone components, Signals) | 18 |
| Database | PostgreSQL | 16 |
| ORM | EF Core (code-first, auto-migrations on startup) | 10 |
| Auth | JWT Bearer + Refresh Tokens (BCrypt passwords) | — |
| Real-time | SignalR hub at `/hubs/notifications` | — |
| Icons | Lucide Angular | ^1.0.0 |
| API Docs | Scalar (OpenAPI) at `/scalar` | — |

---

## Solution Structure

```
SmartEvents/
├── SmartEvents.sln
├── SmartEvents.API/          # ASP.NET Core Web API  (port 5148)
│   ├── Application/
│   │   ├── DTOs/             # C# records for requests & responses
│   │   ├── Helpers/          # SlugHelper
│   │   └── Interfaces/       # ITokenService, IEmailService, ISmsService, IQrCodeService, INotificationDispatcher
│   ├── Controllers/          # One controller per resource (no service layer — db injected directly)
│   ├── Domain/
│   │   ├── Entities/         # EF Core POCO entities (Company, User, Event, Venue, Registration, Ticket, Payment, Notification)
│   │   └── Enums/            # Enums.cs (UserRole, EventStatus, EventCategory, RegistrationStatus, PaymentStatus, etc.)
│   └── Infrastructure/
│       ├── Data/             # SmartEventsDbContext, DbSeeder, Migrations/
│       ├── Hubs/             # NotificationHub (SignalR)
│       ├── Middleware/       # ExceptionMiddleware
│       ├── Notifications/    # EmailService, SmsService, NotificationDispatcher, EmailTemplates
│       └── Services/         # TokenService, QrCodeService
│
├── SmartEvents.Shared/       # .NET class library shared between API and future clients
│   ├── Common/               # ApiResponse<T>, PagedResult<T> records
│   └── Enums/                # Duplicates of domain enums (EventEnums, UserRole, PaymentEnums, etc.)
│
└── SmartEvents.UI/           # Angular 18 SPA  (port 4200)
    └── src/app/
        ├── core/
        │   ├── components/   # skeleton, toast (shared UI primitives)
        │   ├── guards/       # authGuard, guestGuard (functional guards)
        │   ├── interceptors/ # authInterceptor (attaches Bearer token), tokenRefreshInterceptor (auto-retry on 401)
        │   ├── models/       # TypeScript interfaces mirroring API DTOs (one file per domain)
        │   └── services/     # One service per API resource + toast + notifications
        └── features/         # Lazy-loaded feature modules
            ├── analytics/    # analytics-dashboard
            ├── auth/         # login, register, verify-email
            ├── companies/    # companies-list, create-company, edit-company
            ├── dashboard/    # main dashboard
            ├── events/       # events-list, event-detail, create-event, edit-event
            ├── organizer/    # organizer-dashboard, attendees (check-in scanner)
            ├── profile/      # profile edit
            ├── registrations/# my-registrations, check-in
            └── venues/       # venues-list, create-venue, edit-venue
```

---

## Architecture Patterns & Conventions

### Backend (ASP.NET Core)

- **No service layer** — controllers inject `SmartEventsDbContext` directly via primary constructor injection (e.g., `EventsController(SmartEventsDbContext db)`). Keep this pattern; do not introduce a service/repository layer unless explicitly asked.
- **Primary constructor DI** throughout — `public class Foo(IBar bar) { }` not field injection.
- **DTOs are C# `record` types** — positional records for all request/response shapes in `Application/DTOs/`. Validation attributes are placed on record parameters (`[Required]`, `[MaxLength]`, `[Range]`).
- **Mapping happens inline in the controller** via private static `ToSummary` / `ToDetail` helper methods — no AutoMapper.
- **Responses**: Use `Ok()`, `CreatedAtAction()`, `NotFound()`, `Unauthorized()`, `BadRequest()`, `NoContent()`, `Conflict()` directly. Error bodies are anonymous objects with a `message` property: `new { message = "..." }`.
- **Authorization**: Role strings come from `nameof(UserRole.X)` interpolated into `[Authorize(Roles = "...")]`. Four roles: `SuperAdmin`, `CompanyAdmin`, `Organizer`, `Attendee`.
- **Enums**: All domain enums live in `Domain/Enums/Enums.cs`. Serialized as strings via `JsonStringEnumConverter` registered globally in `Program.cs`.
- **Entities**: POCO classes with auto-property initializers. `Id` is `Guid`, set with `Guid.NewGuid()` in the controller at creation time. `CreatedAt`/`UpdatedAt` are `DateTime.UtcNow`.
- **DbContext model config**: All relationships, indexes, and column types are configured in `OnModelCreating` (no data annotations on entities).
- **Migrations**: Run automatically on startup in Development via `db.Database.Migrate()` in `Program.cs`.
- **Global exception handling**: `ExceptionMiddleware` catches unhandled exceptions; maps `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404, `ArgumentException`/`InvalidOperationException` → 400, everything else → 500.
- **SignalR**: `NotificationHub` at `/hubs/notifications`. Auth via JWT access token passed as query param by the client.
- **Slug generation**: `SlugHelper.Generate(title)` / `SlugHelper.GenerateUnique(title, suffix)` in `Application/Helpers/`.

### Frontend (Angular 18)

- **Standalone components everywhere** — no NgModules. Every component declares `standalone: true` with explicit `imports: [...]`.
- **Lazy loading**: All feature routes use `loadChildren` / `loadComponent` for code splitting. Route files export a `*_ROUTES` constant (e.g., `EVENTS_ROUTES`).
- **State via Signals**: `AuthService` uses `signal<User | null>` and `computed()` for `isAuthenticated` and `isAdmin`. Components read signals via `authService.currentUser()`.
- **Services are `providedIn: 'root'`** — no feature-level providers. Each domain has one service file in `core/services/`.
- **HTTP interceptors are functional** (`HttpInterceptorFn`) — `authInterceptor` attaches `Authorization: Bearer <token>`, `tokenRefreshInterceptor` catches 401s and retries after token refresh.
- **Guards are functional** (`CanActivateFn`) — `authGuard` (redirect to `/auth/login`) and `guestGuard` (redirect to `/dashboard`).
- **Models are TypeScript interfaces/types** in `core/models/`. Enums are string union types (`type EventStatus = 'Draft' | 'Published' | ...`), not TS enums.
- **Lucide Angular** for all icons — icons must be registered in `app.config.ts` via `LucideAngularModule.pick({...})` before use.
- **No SCSS utility classes** — styling is inline Tailwind-style class strings or component-scoped SCSS. (Note: Tailwind is NOT installed; raw SCSS is used.)
- **Forms**: Reactive forms (`FormBuilder`, `FormGroup`, `Validators`) for auth/create/edit flows. Template-driven (`ngModel`) for simple filters (e.g., search, category select).
- **Component file convention**: `feature-name.component.ts` + `feature-name.component.html` + optional `feature-name.component.scss`. Spec file only present for `app.component.spec.ts`.
- **Error handling in subscriptions**: Always use `{ next: ..., error: ... }` observer pattern. Error messages extracted via `err.error?.message ?? 'Fallback text'`.
- **Toast system**: `ToastService` (`core/services/toast.service.ts`) with `.success()`, `.error()`, `.info()` methods. `ToastComponent` rendered in `app.component.html`.

---

## Domain Model Summary

| Entity | Key Fields | Relationships |
|---|---|---|
| `Company` | id, name, slug, isActive | has many Users, Venues, Events |
| `User` | id, email, passwordHash, role, companyId, refreshToken, verificationToken | belongs to Company; has many Registrations, OrganizedEvents |
| `Event` | id, title, slug, status, category, startDate, endDate, isTicketed, ticketPrice, maxAttendees, waitlistEnabled | belongs to Company, Venue (nullable), Organizer (User); has many Registrations, Tickets |
| `Venue` | id, name, address, city, country, type, capacity, pricePerDay | belongs to Company; has many Events |
| `Registration` | id, status, registeredAt | unique (eventId + userId); has one Ticket (nullable), one Payment (nullable) |
| `Ticket` | id, ticketNumber (unique), qrCode (base64 PNG), isCheckedIn | belongs to Registration, Event |
| `Payment` | id, amount, method, status, transactionRef | belongs to Registration |
| `Notification` | id, type, event, title, message, isRead | belongs to User |

---

## API Base URLs

- API: `http://localhost:5148/api`
- Scalar docs: `http://localhost:5148/scalar`
- SignalR hub: `http://localhost:5148/hubs/notifications`
- Angular dev: `http://localhost:4200`

---

## Role Hierarchy

```
SuperAdmin > CompanyAdmin > Organizer > Attendee
```

- **SuperAdmin**: full platform access — all companies, users, analytics
- **CompanyAdmin**: their company's events, venues, users, analytics
- **Organizer**: create/manage events, view attendees, check-in scanner
- **Attendee**: browse events, register, view tickets & QR codes

---

## Auth Flow

1. POST `/api/auth/login` → returns `{ accessToken, refreshToken, user }`
2. Access token stored in `localStorage` as `se_token`, refresh as `se_refresh`, user as `se_user`
3. `authInterceptor` injects `Authorization: Bearer <token>` on every request
4. `tokenRefreshInterceptor` catches 401, calls `POST /api/auth/refresh`, retries original request
5. On logout: clear all three localStorage keys, set signals to null, navigate to `/auth/login`

---

## Seeded Dev Accounts (password: `Seed1234!`)

| Email | Role |
|---|---|
| superadmin@smartevents.com | SuperAdmin |
| admin@techevents.co | CompanyAdmin (TechEvents Co.) |
| organizer@techevents.co | Organizer (TechEvents Co.) |
| admin@afrikafest.co | CompanyAdmin (Afrika Fest Productions) |
| organizer@afrikafest.co | Organizer (Afrika Fest Productions) |
| attendee@example.com | Attendee |

---

## Key Conventions to Follow

1. **New API endpoint** → add to existing controller or create new controller in `Controllers/`. Add matching DTO records in `Application/DTOs/`. Map inline with a private static helper. Register no new services unless behaviour genuinely requires a separate class.
2. **New entity** → add class in `Domain/Entities/`, register `DbSet` in `SmartEventsDbContext`, configure in `OnModelCreating`, add EF migration (`dotnet ef migrations add <Name>`).
3. **New Angular feature** → create folder under `features/`, add `*.routes.ts` exporting `FEATURE_ROUTES`, wire into `app.routes.ts` with `loadChildren`. Add service in `core/services/` and model file in `core/models/` if needed.
4. **New Lucide icon needed** → import it in `app.config.ts` and add it to `LucideAngularModule.pick({...})`.
5. **Payments are mock** — no real charge occurs. `PaymentMethod` options: `Stripe`, `AirtelMoney`, `MTNMoMo`, `Free`.
6. **Currency**: Always ZMW (Zambian Kwacha), displayed as `K` prefix.
7. **Date handling**: Store and send all dates as UTC. Frontend receives ISO strings and formats locally.
8. **Pagination**: Use the `PagedResult<T>` record from `SmartEvents.Shared` (fields: `Items`, `TotalCount`, `Page`, `PageSize`; computed: `TotalPages`, `HasNext`, `HasPrevious`). Angular model is interface `PagedResult<T>` in `event.models.ts` (fields: camelCase).

---

## Change Log

> Add an entry here whenever a feature is added, a schema is changed, or an architectural decision is made.

| Date | Change |
|---|---|
| 2026-04-23 | Initial project state documented. All core features implemented: auth, events, registrations, tickets, QR check-in, payments (mock), venues, companies, analytics, SignalR notifications. |
| 2026-06-11 | Added "You might also like" recommendations on event detail page. New endpoint GET /api/events/recommended; EventsService.getRecommended; EventDetailComponent updated with LucideAngularModule and recommendations field; recommendation styles added to styles.scss. |
| 2026-06-16 | Added venue booking: VenueBooking entity + EF migration; POST/GET/DELETE /api/venue-bookings endpoints (confirmed+mock-paid immediately); VenueDetailComponent with inline booking form; MyVenueBookingsComponent at /venues/my-bookings; BookMarked icon; My Bookings nav link in dashboard; View Details link on venue cards. Fixed sticky dashboard nav (position:sticky). Seeded 3 more events (Conference×2, Exhibition×1) so recommendations return results. |
| 2026-06-16 | Added company member management: GET/POST/PUT/DELETE /api/companies/{id}/members endpoints (SuperAdmin+CompanyAdmin only); CompanyMembersComponent at /companies/:id/members; CompanyMembersService; "Manage Members" link in edit-company page. Member removal resets role to Attendee. |
