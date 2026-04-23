# SmartEvents

A full-stack event management platform built for Malawi, supporting multi-company event creation, ticketing, QR code check-in, and real-time notifications. Currency is ZMW (Zambian Kwacha).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 18 (standalone components, Signals) |
| Backend | ASP.NET Core (.NET 10) Web API |
| Database | PostgreSQL 16 (EF Core, auto-migrations) |
| Auth | JWT Bearer + Refresh Tokens |
| Real-time | SignalR |
| Icons | Lucide Angular |
| API Docs | Scalar (OpenAPI) |

---

## Project Structure

```
SmartEvents/
├── SmartEvents.API/        # ASP.NET Core Web API
│   ├── Application/        # DTOs, interfaces
│   ├── Controllers/        # REST endpoints
│   ├── Domain/             # Entities, enums
│   └── Infrastructure/     # EF Core, services, SignalR, middleware
│
├── SmartEvents.UI/         # Angular 18 SPA
│   └── src/app/
│       ├── core/           # Auth, services, interceptors, guards, models
│       └── features/       # Page modules (events, venues, auth, etc.)
│
└── SmartEvents.Shared/     # Shared enums and common types (.NET library)
```

---

## Features

### Authentication & Roles
- JWT access tokens + refresh tokens (auto-retry on 401)
- Four role levels with scoped access:

| Role | Access |
|---|---|
| **SuperAdmin** | Full platform access — all companies, users, analytics |
| **CompanyAdmin** | Manage their company's events, venues, and staff |
| **Organizer** | Create and manage events, view attendees, check-in |
| **Attendee** | Browse events, register, view tickets and QR codes |

- Email verification via tokenised link
- Password show/hide toggle on auth forms

### Events
- Create, edit, publish, and cancel events
- Free and paid (ticketed) events
- Waitlist support when event is full
- Event categories: Conference, Workshop, Concert, Exhibition, Sports, Networking, Webinar, Other
- Slug-based public URLs

### Registrations & Ticketing
- One-click free registration or mock checkout for paid events
- Payment methods: Stripe, Airtel Money, MTN MoMo (mock — no real charge)
- QR code generated per ticket (base64 PNG, toggled in My Tickets)
- QR-based check-in scanner for organizers with session history

### Venues
- Multi-venue support per company
- Types: Indoor, Outdoor, Virtual, Hybrid
- Capacity, amenities, pricing per day (ZMW)

### Companies
- Multi-company platform (SuperAdmin manages companies)
- Users are scoped to their company

### Real-time Notifications
- SignalR hub at `/hubs/notifications`
- Auto-connects on login, disconnects on logout
- Toast notifications for registration confirmed, event updated, event cancelled

### Analytics (Admin only)
- Overview stats: total events, registrations, check-ins, revenue
- Per-event breakdown: fill rate, confirmed, waitlisted, checked-in, cancelled, revenue

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [PostgreSQL 16](https://www.postgresql.org/download/)

---

### 1. Configure the database

Edit `SmartEvents.API/appsettings.Development.json` and set your Postgres credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SmartEventsDb;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

---

### 2. Run the API

```bash
cd SmartEvents.API
dotnet run
```

On first startup the API will:
1. Auto-apply all EF Core migrations
2. Seed the database with demo companies, venues, events, and users

The API listens on `http://localhost:5148`.  
Interactive API docs (Scalar) are available at `http://localhost:5148/scalar`.

---

### 3. Run the Angular app

```bash
cd SmartEvents.UI
npm install
ng serve
```

The app runs at `http://localhost:4200`.

---

## Seeded Accounts

All demo accounts share the password: **`Seed1234!`**

| Email | Role |
|---|---|
| `superadmin@smartevents.com` | SuperAdmin |
| `admin@techevents.co` | CompanyAdmin (TechEvents Co.) |
| `organizer@techevents.co` | Organizer (TechEvents Co.) |
| `admin@afrikafest.co` | CompanyAdmin (Afrika Fest Productions) |
| `organizer@afrikafest.co` | Organizer (Afrika Fest Productions) |
| `attendee@example.com` | Attendee |

### Seeded Events

| Event | Type | Price |
|---|---|---|
| Dev Summit Malawi 2026 | Conference | K8,500 |
| Angular 18 Hands-On Workshop | Workshop | Free |
| AfrikaFest Music Night 2026 | Concert | K4,500 |
| Blantyre Tech Networking Brunch | Networking (Draft) | Free |
| AI in Africa Webinar | Webinar | Free |
| Malawi Startup Exhibition 2026 | Exhibition | K2,500 |

> **Note:** If the seeder skips (users already exist from a previous session), clear the database and restart:
> ```bash
> psql -U postgres -d SmartEventsDb -c "TRUNCATE \"Payments\",\"Tickets\",\"Registrations\",\"Events\",\"Venues\",\"Notifications\",\"Users\",\"Companies\" CASCADE;"
> ```

---

## API Endpoints

### Auth — `/api/auth`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Create account |
| POST | `/login` | — | Sign in, returns JWT + refresh token |
| POST | `/refresh` | — | Exchange refresh token for new access token |
| POST | `/request-verification` | ✓ | Send email verification link |
| GET | `/verify-email?token=` | — | Confirm email address |

### Events — `/api/events`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | — | Paginated public event list (filter by category, search) |
| GET | `/:slug` | — | Single event by slug |
| POST | `/` | Organizer+ | Create event |
| PUT | `/:id` | Organizer+ | Update event |
| DELETE | `/:id` | CompanyAdmin+ | Delete event |
| GET | `/company/:companyId` | ✓ | Events for a company |

### Registrations — `/api/registrations`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | ✓ | Register for a free event |
| DELETE | `/:id` | ✓ | Cancel registration |
| GET | `/my` | ✓ | My registrations + tickets |
| GET | `/event/:eventId` | Organizer+ | All registrations for an event |
| POST | `/checkin` | Organizer+ | Check in by ticket number |

### Payments — `/api/payments`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/checkout` | ✓ | Register + pay for a ticketed event (mock) |
| GET | `/my` | ✓ | Payment history |

### Venues — `/api/venues`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | ✓ | All venues (filter by city, country, type) |
| GET | `/:id` | ✓ | Single venue |
| POST | `/` | Organizer+ | Create venue |
| PUT | `/:id` | Organizer+ | Update venue |
| DELETE | `/:id` | CompanyAdmin+ | Delete venue |

### Companies — `/api/companies`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | ✓ | All active companies |
| POST | `/` | SuperAdmin | Create company |
| PUT | `/:id` | CompanyAdmin+ | Update company |
| DELETE | `/:id` | SuperAdmin | Deactivate company |

### Analytics — `/api/analytics`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/overview` | CompanyAdmin+ | Platform-wide or company stats |
| GET | `/events` | Organizer+ | Per-event breakdown |

### Users — `/api/users`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/me` | ✓ | Current user profile |
| PUT | `/me` | ✓ | Update profile |

---

## Environment Configuration

`appsettings.Development.json` — override for local development:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SmartEventsDb;Username=postgres;Password=..."
  },
  "JwtSettings": {
    "SecretKey": "at-least-32-character-random-secret",
    "Issuer": "SmartEvents.API",
    "Audience": "SmartEvents.UI",
    "ExpiryInMinutes": 120,
    "RefreshExpiryInDays": 30
  },
  "AppUrl": "http://localhost:4200",
  "Email": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "noreply@smartevents.com",
    "Password": "...",
    "FromName": "SmartEvents"
  }
}
```

Angular environment (`src/environments/environment.ts`):

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5148/api'
};
```

---

## License

MIT
