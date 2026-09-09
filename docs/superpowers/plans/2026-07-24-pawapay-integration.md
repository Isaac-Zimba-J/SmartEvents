# PawaPay Sandbox Integration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the mock payment flow with real PawaPay sandbox deposits for AirtelMoney and MTNMoMo, removing Stripe entirely.

**Architecture:** Optimistic registration (registration + ticket created as Pending immediately on checkout), then PawaPay deposit initiated; frontend polls `GET /api/payments/{id}/status` every 3 s (max 40 attempts = 2 min) and renders a live payment-pending screen; a webhook endpoint (`POST /api/pawapay/webhook`) updates the DB for production. Backend has a new `PawaPayService` infrastructure class that owns all HTTP calls to PawaPay.

**Tech Stack:** ASP.NET Core .NET 10, EF Core (Npgsql), `HttpClient`, Angular 18 standalone components, RxJS `interval`/`takeWhile`.

## Global Constraints

- **No service layer in controllers** — controllers inject `SmartEventsDbContext` directly (CLAUDE.md convention). `PawaPayService` is the ONE exception — it is genuine infrastructure (HTTP client to external API).
- **Primary constructor DI** everywhere: `public class Foo(IBar bar, IQux qux) { }`, not field injection.
- **DTOs are C# `record` types** in `Application/DTOs/`.
- **Inline mapping** via private static helpers in controllers — no AutoMapper.
- **Currency:** ZMW. Display as `K` prefix.
- **PawaPay sandbox base URL:** `https://api.sandbox.pawapay.cloud`
- **Correspondent codes:** `AirtelMoney` → `AIRTEL_ZAMBIA`, `MTNMoMo` → `MTN_ZAMBIA`
- **Zambian phone format:** `260XXXXXXXXX` (no leading `+`)
- **Angular:** standalone components, `@if`/`@for` control flow, no NgModules.
- **`User.Phone` already exists** on the entity and in the Angular model — no migration or model change needed for that field.

## File Map

**Backend — create:**
- `SmartEvents.API/Infrastructure/Services/PawaPayService.cs` — HTTP wrapper for PawaPay deposits API
- `SmartEvents.API/Application/Interfaces/IPawaPayService.cs` — interface for DI
- `SmartEvents.API/Application/DTOs/PawaPayDtos.cs` — internal request/response records (not exposed to frontend)
- `SmartEvents.API/Controllers/PawaPayWebhookController.cs` — `POST /api/pawapay/webhook`

**Backend — modify:**
- `SmartEvents.API/Domain/Enums/Enums.cs` — remove `Stripe` from `PaymentMethod`
- `SmartEvents.API/Domain/Entities/Payment.cs` — add `PawaPayDepositId`
- `SmartEvents.API/Application/DTOs/PaymentDtos.cs` — add `PhoneNumber` to checkout request; add `PaymentStatusResponse` record
- `SmartEvents.API/Controllers/PaymentsController.cs` — rewrite `Checkout`, add `GetStatus`
- `SmartEvents.API/Program.cs` — register `PawaPayService`
- `SmartEvents.API/appsettings.json` — add `PawaPay` config section
- `SmartEvents.API/Infrastructure/Data/` — run `dotnet ef migrations add AddPawaPayFields`

**Frontend — modify:**
- `SmartEvents.UI/src/app/core/models/payment.models.ts` — remove `Stripe`, add `phoneNumber`, add `PaymentStatusResponse`
- `SmartEvents.UI/src/app/core/services/payments.service.ts` — add `getStatus()`
- `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts` — rewrite checkout with phone + polling
- `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html` — phone field, pending/completed/failed states

---

### Task 1: Backend — entity, enum, migration, config

**Files:**
- Modify: `SmartEvents.API/Domain/Enums/Enums.cs`
- Modify: `SmartEvents.API/Domain/Entities/Payment.cs`
- Modify: `SmartEvents.API/appsettings.json`
- Create migration: `AddPawaPayFields`

- [ ] **Step 1: Remove Stripe from PaymentMethod enum**

In `SmartEvents.API/Domain/Enums/Enums.cs`, change:
```csharp
public enum PaymentMethod
{
    Stripe,
    AirtelMoney,
    MTNMoMo,
    Free
}
```
to:
```csharp
public enum PaymentMethod
{
    AirtelMoney,
    MTNMoMo,
    Free
}
```

- [ ] **Step 2: Add PawaPayDepositId to Payment entity**

In `SmartEvents.API/Domain/Entities/Payment.cs`, add the field after `GatewayResponse`:
```csharp
public string? GatewayResponse { get; set; }
public string? PawaPayDepositId { get; set; }   // ← add this line
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

- [ ] **Step 3: Add PawaPay config to appsettings.json**

In `SmartEvents.API/appsettings.json`, add after the `Cors` block:
```json
"PawaPay": {
  "ApiToken": "YOUR_SANDBOX_API_TOKEN_HERE",
  "BaseUrl": "https://api.sandbox.pawapay.cloud"
},
```

Full file after edit:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SmartEventsDb;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "SmartEvents.API",
    "Audience": "SmartEvents.UI",
    "ExpiryInMinutes": 60,
    "RefreshExpiryInDays": 7
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "PawaPay": {
    "ApiToken": "YOUR_SANDBOX_API_TOKEN_HERE",
    "BaseUrl": "https://api.sandbox.pawapay.cloud"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Create EF migration for PawaPayDepositId**

Run from `SmartEvents.API/`:
```bash
cd SmartEvents.API
dotnet ef migrations add AddPawaPayFields
```

Expected output ends with: `Done. To undo this action, use 'ef migrations remove'`

Verify the generated migration file in `Infrastructure/Data/Migrations/` has:
```csharp
migrationBuilder.AddColumn<string>(
    name: "PawaPayDepositId",
    table: "Payments",
    type: "text",
    nullable: true);
```

- [ ] **Step 5: Verify build compiles**

```bash
cd SmartEvents.API
dotnet build
```

Expected: Build succeeded, 0 errors.

(The `Stripe` removal may produce warnings if any code references `PaymentMethod.Stripe` — those will be fixed in Task 3.)

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.API/Domain/Enums/Enums.cs \
        SmartEvents.API/Domain/Entities/Payment.cs \
        SmartEvents.API/appsettings.json \
        SmartEvents.API/Infrastructure/Data/Migrations/
git commit -m "feat: remove Stripe enum, add PawaPayDepositId to Payment, migration AddPawaPayFields"
```

---

### Task 2: PawaPayService + interface

**Files:**
- Create: `SmartEvents.API/Application/Interfaces/IPawaPayService.cs`
- Create: `SmartEvents.API/Application/DTOs/PawaPayDtos.cs`
- Create: `SmartEvents.API/Infrastructure/Services/PawaPayService.cs`
- Modify: `SmartEvents.API/Program.cs`

- [ ] **Step 1: Create internal PawaPay DTOs**

Create `SmartEvents.API/Application/DTOs/PawaPayDtos.cs`:
```csharp
namespace SmartEvents.API.Application.DTOs;

// Sent to POST /deposits
public record PawaPayDepositRequest(
    string DepositId,
    decimal Amount,
    string Currency,
    string Correspondent,
    PawaPayPayer Payer,
    string CustomerTimestamp,
    string StatementDescription
);

public record PawaPayPayer(string Type, PawaPayAddress Address);

public record PawaPayAddress(string Value);

// Received from POST /deposits (initiation response)
public record PawaPayInitiateResponse(
    string DepositId,
    string Status,
    string? Created
);

// Received from GET /deposits/{id} (status poll)
public record PawaPayDepositStatusResponse(
    string DepositId,
    string Status,           // ACCEPTED | COMPLETED | FAILED | DUPLICATE_IGNORED
    decimal? Amount,
    string? Currency,
    string? Correspondent,
    PawaPayPayer? Payer,
    string? Created,
    string? RespondedByPayer
);
```

- [ ] **Step 2: Create IPawaPayService interface**

Create `SmartEvents.API/Application/Interfaces/IPawaPayService.cs`:
```csharp
using SmartEvents.API.Application.DTOs;

namespace SmartEvents.API.Application.Interfaces;

public interface IPawaPayService
{
    Task<PawaPayInitiateResponse> InitiateDepositAsync(
        string depositId,
        decimal amount,
        string currency,
        string correspondent,
        string phoneNumber
    );

    Task<PawaPayDepositStatusResponse> GetDepositStatusAsync(string depositId);
}
```

- [ ] **Step 3: Implement PawaPayService**

Create `SmartEvents.API/Infrastructure/Services/PawaPayService.cs`:
```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;

namespace SmartEvents.API.Infrastructure.Services;

public class PawaPayService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IPawaPayService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("PawaPay");
        var token = configuration["PawaPay:ApiToken"]
            ?? throw new InvalidOperationException("PawaPay:ApiToken not configured.");
        var baseUrl = configuration["PawaPay:BaseUrl"]
            ?? "https://api.sandbox.pawapay.cloud";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<PawaPayInitiateResponse> InitiateDepositAsync(
        string depositId,
        decimal amount,
        string currency,
        string correspondent,
        string phoneNumber)
    {
        var body = new PawaPayDepositRequest(
            DepositId: depositId,
            Amount: amount,
            Currency: currency,
            Correspondent: correspondent,
            Payer: new PawaPayPayer("MSISDN", new PawaPayAddress(phoneNumber)),
            CustomerTimestamp: DateTime.UtcNow.ToString("o"),
            StatementDescription: "SmartEvents ticket purchase"
        );

        var json = JsonSerializer.Serialize(body, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = CreateClient();
        var response = await client.PostAsync("/deposits", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PawaPay deposit initiation failed: {responseBody}");

        return JsonSerializer.Deserialize<PawaPayInitiateResponse>(responseBody, JsonOpts)
            ?? throw new InvalidOperationException("Invalid PawaPay initiation response.");
    }

    public async Task<PawaPayDepositStatusResponse> GetDepositStatusAsync(string depositId)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/deposits/{depositId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PawaPay status check failed: {responseBody}");

        // PawaPay returns an array for GET /deposits/{id}
        var results = JsonSerializer.Deserialize<PawaPayDepositStatusResponse[]>(responseBody, JsonOpts);
        return results?.FirstOrDefault()
            ?? throw new InvalidOperationException("Empty PawaPay status response.");
    }
}
```

- [ ] **Step 4: Register PawaPayService in Program.cs**

In `SmartEvents.API/Program.cs`, after the existing `// Services` block, add:
```csharp
// HTTP clients
builder.Services.AddHttpClient("PawaPay");

// PawaPay
builder.Services.AddScoped<IPawaPayService, PawaPayService>();
```

Also add the using at the top of Program.cs:
```csharp
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Infrastructure.Services;
```

These usings are already present for the other services, so only add what's missing.

Full `// Services` block in Program.cs after edit:
```csharp
// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// HTTP clients
builder.Services.AddHttpClient("PawaPay");

// PawaPay
builder.Services.AddScoped<IPawaPayService, PawaPayService>();
```

- [ ] **Step 5: Build to verify**

```bash
cd SmartEvents.API
dotnet build
```

Expected: 0 errors. `PawaPayService` is injectable.

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.API/Application/DTOs/PawaPayDtos.cs \
        SmartEvents.API/Application/Interfaces/IPawaPayService.cs \
        SmartEvents.API/Infrastructure/Services/PawaPayService.cs \
        SmartEvents.API/Program.cs
git commit -m "feat: add PawaPayService and IPawaPayService for sandbox deposit API"
```

---

### Task 3: PaymentsController — real checkout + status polling endpoint

**Files:**
- Modify: `SmartEvents.API/Application/DTOs/PaymentDtos.cs`
- Modify: `SmartEvents.API/Controllers/PaymentsController.cs`

- [ ] **Step 1: Update PaymentDtos.cs**

Replace the entire content of `SmartEvents.API/Application/DTOs/PaymentDtos.cs` with:
```csharp
using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record PaymentCheckoutRequest(
    [Required] Guid EventId,
    PaymentMethod PaymentMethod,
    [Required, MaxLength(20)] string PhoneNumber,
    string? Notes
);

public record PaymentCheckoutResponse(
    Guid PaymentId,
    string TransactionReference,
    decimal Amount,
    PaymentStatus Status,
    PaymentMethod Method,
    RegistrationResponse Registration
);

public record PaymentStatusResponse(
    Guid PaymentId,
    PaymentStatus Status,
    string? TransactionReference,
    DateTime? PaidAt
);

public record PaymentSummaryResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    PaymentMethod Method,
    string? TransactionReference,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string EventTitle
);
```

- [ ] **Step 2: Rewrite PaymentsController**

Replace the entire content of `SmartEvents.API/Controllers/PaymentsController.cs` with:
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(
    SmartEventsDbContext db,
    IQrCodeService qrCodeService,
    IPawaPayService pawaPayService) : ControllerBase
{
    private static readonly Dictionary<PaymentMethod, string> CorrespondentMap = new()
    {
        [PaymentMethod.AirtelMoney] = "AIRTEL_ZAMBIA",
        [PaymentMethod.MTNMoMo] = "MTN_ZAMBIA"
    };

    [HttpPost("checkout")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Checkout(PaymentCheckoutRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();

        if (request.PaymentMethod == PaymentMethod.Free)
            return BadRequest(new { message = "Use /api/registrations for free events." });

        if (!CorrespondentMap.TryGetValue(request.PaymentMethod, out var correspondent))
            return BadRequest(new { message = "Unsupported payment method." });

        var ev = await db.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.Status != EventStatus.Published)
            return BadRequest(new { message = "Event is not open for registration." });
        if (ev.EndDate < DateTime.UtcNow)
            return BadRequest(new { message = "Event has already ended." });
        if (!ev.IsTicketed)
            return BadRequest(new { message = "This event is free. Use /api/registrations instead." });

        if (await db.Registrations.AnyAsync(r => r.EventId == request.EventId && r.UserId == userId))
            return Conflict(new { message = "You are already registered for this event." });

        var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        if (confirmedCount >= ev.MaxAttendees && !ev.WaitlistEnabled)
            return BadRequest(new { message = "Event is full and waitlist is disabled." });

        // Optimistic: create registration + ticket as Pending
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            UserId = userId,
            Status = RegistrationStatus.Pending,
            WaitlistPosition = 0,
            Notes = request.Notes
        };
        db.Registrations.Add(registration);

        var ticketNumber = GenerateTicketNumber();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            QrCode = qrCodeService.GenerateBase64($"SE:{ticketNumber}:{registration.Id}"),
            RegistrationId = registration.Id,
            EventId = ev.Id
        };
        db.Tickets.Add(ticket);

        var depositId = Guid.NewGuid().ToString();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = ev.TicketPrice,
            Currency = "ZMW",
            Status = PaymentStatus.Pending,
            Method = request.PaymentMethod,
            PawaPayDepositId = depositId,
            RegistrationId = registration.Id
        };
        db.Payments.Add(payment);

        await db.SaveChangesAsync();

        // Call PawaPay — if it fails, we still return pending (user can retry via status poll)
        try
        {
            await pawaPayService.InitiateDepositAsync(
                depositId,
                ev.TicketPrice,
                "ZMW",
                correspondent,
                request.PhoneNumber
            );
        }
        catch (Exception ex)
        {
            // Log but don't fail — sandbox may return ACCEPTED asynchronously
            Console.Error.WriteLine($"PawaPay initiation warning: {ex.Message}");
        }

        return Ok(new PaymentCheckoutResponse(
            payment.Id,
            depositId,
            payment.Amount,
            payment.Status,
            payment.Method,
            new RegistrationResponse(
                registration.Id,
                registration.Status,
                registration.WaitlistPosition,
                registration.Notes,
                registration.RegisteredAt,
                registration.CheckedInAt,
                ev.Id,
                ev.Title,
                userId,
                $"{user.FirstName} {user.LastName}",
                new TicketResponse(ticket.Id, ticket.TicketNumber, ticket.QrCode, ticket.IsUsed, ticket.IssuedAt)
            )
        ));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var payment = await db.Payments
            .Include(p => p.Registration)
            .FirstOrDefaultAsync(p => p.Id == id && p.Registration.UserId == userId);

        if (payment is null) return NotFound(new { message = "Payment not found." });

        // If already terminal, return cached status
        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Failed)
            return Ok(new PaymentStatusResponse(payment.Id, payment.Status, payment.TransactionReference, payment.PaidAt));

        // Poll PawaPay for current status
        if (payment.PawaPayDepositId is not null)
        {
            try
            {
                var remote = await pawaPayService.GetDepositStatusAsync(payment.PawaPayDepositId);

                if (remote.Status == "COMPLETED")
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionReference = payment.PawaPayDepositId;
                    payment.GatewayResponse = remote.Status;
                    payment.PaidAt = DateTime.UtcNow;

                    // Confirm registration and bump count
                    payment.Registration.Status = RegistrationStatus.Confirmed;
                    payment.Registration.CheckedInAt = null;

                    await db.SaveChangesAsync();
                }
                else if (remote.Status == "FAILED")
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.GatewayResponse = remote.Status;
                    payment.Registration.Status = RegistrationStatus.Cancelled;

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PawaPay status poll warning: {ex.Message}");
            }
        }

        return Ok(new PaymentStatusResponse(payment.Id, payment.Status, payment.TransactionReference, payment.PaidAt));
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<PaymentSummaryResponse>>> GetMyPayments()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var payments = await db.Payments
            .Include(p => p.Registration).ThenInclude(r => r.Event)
            .Where(p => p.Registration.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentSummaryResponse(
                p.Id,
                p.Amount,
                p.Currency,
                p.Status,
                p.Method,
                p.TransactionReference,
                p.CreatedAt,
                p.PaidAt,
                p.Registration.Event.Title
            ))
            .ToListAsync();

        return Ok(payments);
    }

    private static string GenerateTicketNumber()
        => $"SE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
```

- [ ] **Step 3: Build**

```bash
cd SmartEvents.API
dotnet build
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add SmartEvents.API/Application/DTOs/PaymentDtos.cs \
        SmartEvents.API/Controllers/PaymentsController.cs
git commit -m "feat: PaymentsController real PawaPay checkout, status polling endpoint"
```

---

### Task 4: PawaPayWebhookController

**Files:**
- Create: `SmartEvents.API/Controllers/PawaPayWebhookController.cs`

The webhook receives a deposit callback from PawaPay in production. In the sandbox, polling is the primary mechanism, but the webhook handles the production case.

- [ ] **Step 1: Create webhook DTO**

In `SmartEvents.API/Application/DTOs/PawaPayDtos.cs`, append:
```csharp
// Webhook callback body from PawaPay
public record PawaPayWebhookPayload(
    string DepositId,
    string Status,
    decimal? Amount,
    string? Currency
);
```

- [ ] **Step 2: Create PawaPayWebhookController**

Create `SmartEvents.API/Controllers/PawaPayWebhookController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/pawapay")]
[AllowAnonymous]
public class PawaPayWebhookController(SmartEventsDbContext db) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] PawaPayWebhookPayload payload)
    {
        var payment = await db.Payments
            .Include(p => p.Registration)
            .FirstOrDefaultAsync(p => p.PawaPayDepositId == payload.DepositId);

        if (payment is null) return Ok(); // Unknown deposit — ignore safely

        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Failed)
            return Ok(); // Already terminal — idempotent

        if (payload.Status == "COMPLETED")
        {
            payment.Status = PaymentStatus.Completed;
            payment.TransactionReference = payload.DepositId;
            payment.GatewayResponse = payload.Status;
            payment.PaidAt = DateTime.UtcNow;
            payment.Registration.Status = RegistrationStatus.Confirmed;
        }
        else if (payload.Status == "FAILED")
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = payload.Status;
            payment.Registration.Status = RegistrationStatus.Cancelled;
        }

        await db.SaveChangesAsync();
        return Ok();
    }
}
```

- [ ] **Step 3: Build**

```bash
cd SmartEvents.API
dotnet build
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add SmartEvents.API/Application/DTOs/PawaPayDtos.cs \
        SmartEvents.API/Controllers/PawaPayWebhookController.cs
git commit -m "feat: PawaPayWebhookController handles deposit callbacks for production"
```

---

### Task 5: Frontend — models + PaymentsService

**Files:**
- Modify: `SmartEvents.UI/src/app/core/models/payment.models.ts`
- Modify: `SmartEvents.UI/src/app/core/services/payments.service.ts`

- [ ] **Step 1: Update payment.models.ts**

Replace entire content of `SmartEvents.UI/src/app/core/models/payment.models.ts`:
```typescript
import { RegistrationResponse } from './registration.models';

export type PaymentMethod = 'AirtelMoney' | 'MTNMoMo' | 'Free';
export type PaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface PaymentCheckoutRequest {
  eventId: string;
  paymentMethod: PaymentMethod;
  phoneNumber: string;
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

export interface PaymentStatusResponse {
  paymentId: string;
  status: PaymentStatus;
  transactionReference?: string;
  paidAt?: string;
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
```

- [ ] **Step 2: Update PaymentsService**

Replace entire content of `SmartEvents.UI/src/app/core/services/payments.service.ts`:
```typescript
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
```

- [ ] **Step 3: Check for any remaining Stripe references in the frontend**

```bash
grep -r "Stripe" /Users/zimbadev/Documents/Workspace/Peoples\ Projects/SmartEvents/SmartEvents.UI/src --include="*.ts" --include="*.html" -l
```

If any files are found, remove/replace the Stripe references in them (e.g. any hardcoded `'Stripe'` string in selects or labels).

- [ ] **Step 4: Commit**

```bash
git add SmartEvents.UI/src/app/core/models/payment.models.ts \
        SmartEvents.UI/src/app/core/services/payments.service.ts
git commit -m "feat: update payment models, remove Stripe, add PaymentStatusResponse and getStatus()"
```

---

### Task 6: EventDetailComponent — phone field + polling checkout UI

**Files:**
- Modify: `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts`
- Modify: `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html`

**Flow:**
1. User clicks "Buy Ticket"
2. Checkout panel slides in: payment method selector + phone number (pre-filled from `auth.currentUser()?.phone`)
3. On "Pay Now": POST checkout → receive `{ paymentId, status: 'Pending', ... }`
4. Enter polling state: `checkoutState = 'pending'`; interval every 3 s, max 40 polls
5. On `Completed`: set `checkoutState = 'completed'`, save registration, stop polling
6. On `Failed` or poll exhaustion: set `checkoutState = 'failed'`, stop polling

**States:** `'idle' | 'pending' | 'completed' | 'failed' | 'timeout'`

- [ ] **Step 1: Rewrite event-detail.component.ts**

Replace entire content of `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts`:
```typescript
import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { interval, Subscription } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';
import { EventsService } from '../../../core/services/events.service';
import { RegistrationsService } from '../../../core/services/registrations.service';
import { PaymentsService } from '../../../core/services/payments.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventDetail, EventSummary } from '../../../core/models/event.models';
import { RegistrationResponse } from '../../../core/models/registration.models';
import { PaymentMethod } from '../../../core/models/payment.models';

type CheckoutState = 'idle' | 'pending' | 'completed' | 'failed' | 'timeout';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, LucideAngularModule],
  templateUrl: './event-detail.component.html'
})
export class EventDetailComponent implements OnInit, OnDestroy {
  event: EventDetail | null = null;
  loading = true;
  acting = false;
  registration: RegistrationResponse | null = null;
  error = '';
  successMessage = '';
  recommendations: EventSummary[] = [];

  showCheckout = false;
  selectedPaymentMethod: PaymentMethod = 'AirtelMoney';
  phoneNumber = '';
  checkoutState: CheckoutState = 'idle';
  pendingPaymentId: string | null = null;

  private pollSub: Subscription | null = null;
  private readonly MAX_POLLS = 40;

  readonly paymentMethods: { value: PaymentMethod; label: string }[] = [
    { value: 'AirtelMoney', label: 'Airtel Money' },
    { value: 'MTNMoMo', label: 'MTN Mobile Money' }
  ];

  constructor(
    private route: ActivatedRoute,
    private eventsService: EventsService,
    private registrationsService: RegistrationsService,
    private paymentsService: PaymentsService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.eventsService.getBySlug(slug).subscribe({
      next: data => {
        this.event = data;
        this.loading = false;
        this.eventsService.getRecommended(data.id, data.category).subscribe({
          next: recs => { this.recommendations = recs; },
          error: () => {}
        });
      },
      error: () => { this.loading = false; }
    });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  get spotsLeft(): number {
    if (!this.event) return 0;
    return this.event.maxAttendees - this.event.registeredCount;
  }

  get isFull(): boolean {
    return this.spotsLeft <= 0;
  }

  get isOrganizer(): boolean {
    const role = this.auth.currentUser()?.role;
    return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'Organizer';
  }

  openCheckout(): void {
    this.showCheckout = true;
    this.checkoutState = 'idle';
    this.error = '';
    this.phoneNumber = this.auth.currentUser()?.phone ?? '';
  }

  cancelCheckout(): void {
    this.showCheckout = false;
    this.checkoutState = 'idle';
    this.error = '';
    this.stopPolling();
  }

  registerFree(): void {
    if (!this.event || !this.auth.isAuthenticated()) return;
    this.acting = true;
    this.error = '';

    this.registrationsService.register({ eventId: this.event.id }).subscribe({
      next: reg => {
        this.registration = reg;
        this.successMessage = reg.status === 'Waitlisted'
          ? `You're on the waitlist — position #${reg.waitlistPosition}.`
          : `Registered! Your ticket: ${reg.ticket?.ticketNumber}`;
        this.acting = false;
        if (this.event) this.event.registeredCount++;
      },
      error: err => {
        this.error = err.error?.message ?? 'Registration failed.';
        this.acting = false;
      }
    });
  }

  completePurchase(): void {
    if (!this.event || !this.auth.isAuthenticated() || !this.phoneNumber.trim()) return;
    this.acting = true;
    this.error = '';

    this.paymentsService.checkout({
      eventId: this.event.id,
      paymentMethod: this.selectedPaymentMethod,
      phoneNumber: this.phoneNumber.trim()
    }).subscribe({
      next: result => {
        this.pendingPaymentId = result.paymentId;
        this.registration = result.registration;
        this.acting = false;
        this.checkoutState = 'pending';
        this.startPolling(result.paymentId);
      },
      error: err => {
        this.error = err.error?.message ?? 'Payment initiation failed. Please try again.';
        this.acting = false;
      }
    });
  }

  private startPolling(paymentId: string): void {
    let pollCount = 0;

    this.pollSub = interval(3000).pipe(
      take(this.MAX_POLLS),
      switchMap(() => this.paymentsService.getStatus(paymentId))
    ).subscribe({
      next: status => {
        pollCount++;

        if (status.status === 'Completed') {
          this.checkoutState = 'completed';
          this.successMessage = `Payment successful! Ticket: ${this.registration?.ticket?.ticketNumber}`;
          if (this.event) this.event.registeredCount++;
          this.stopPolling();
        } else if (status.status === 'Failed') {
          this.checkoutState = 'failed';
          this.error = 'Payment was declined. Please try again with a different number.';
          this.stopPolling();
        } else if (pollCount >= this.MAX_POLLS) {
          this.checkoutState = 'timeout';
          this.error = 'Payment timed out. Check your mobile money app and try again.';
        }
      },
      error: () => {
        // Network error during polling — keep trying
      }
    });
  }

  private stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = null;
  }
}
```

- [ ] **Step 2: Update event-detail.component.html checkout section**

Open `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html` and find the checkout panel (the section that shows `showCheckout` and the payment method selector). Replace **only** that block with the following.

The exact block to find and replace starts where `showCheckout` is checked and ends after the payment form closes. Replace the existing checkout section with:

```html
<!-- Checkout panel -->
@if (showCheckout) {
  <div class="checkout-overlay">
    <div class="checkout-panel">

      <!-- IDLE / FORM STATE -->
      @if (checkoutState === 'idle') {
        <h3>Complete Purchase</h3>
        <p class="checkout-amount">K {{ event?.ticketPrice | number:'1.2-2' }}</p>

        <div class="form-group">
          <label>Payment Method</label>
          <select [(ngModel)]="selectedPaymentMethod" class="form-control">
            @for (m of paymentMethods; track m.value) {
              <option [value]="m.value">{{ m.label }}</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label>Mobile Money Number</label>
          <input
            type="tel"
            [(ngModel)]="phoneNumber"
            placeholder="260XXXXXXXXX"
            class="form-control"
            maxlength="20"
          />
          <small class="form-hint">Zambian format: 260XXXXXXXXX</small>
        </div>

        @if (error) {
          <div class="alert alert-danger">{{ error }}</div>
        }

        <div class="checkout-actions">
          <button class="btn btn-secondary" (click)="cancelCheckout()" [disabled]="acting">Cancel</button>
          <button class="btn btn-primary" (click)="completePurchase()" [disabled]="acting || !phoneNumber.trim()">
            @if (acting) { Processing... } @else { Pay Now }
          </button>
        </div>
      }

      <!-- PENDING STATE -->
      @if (checkoutState === 'pending') {
        <div class="checkout-pending">
          <div class="spinner"></div>
          <h3>Waiting for payment…</h3>
          <p>A push notification has been sent to <strong>{{ phoneNumber }}</strong>. Approve it in your mobile money app.</p>
          <p class="checkout-hint">This page will update automatically.</p>
          <button class="btn btn-secondary btn-sm" (click)="cancelCheckout()">Cancel</button>
        </div>
      }

      <!-- COMPLETED STATE -->
      @if (checkoutState === 'completed') {
        <div class="checkout-success">
          <lucide-icon name="check-circle" [size]="48"></lucide-icon>
          <h3>Payment Successful!</h3>
          <p>Your ticket number: <strong>{{ registration?.ticket?.ticketNumber }}</strong></p>
          <button class="btn btn-primary" (click)="cancelCheckout()">Done</button>
        </div>
      }

      <!-- FAILED STATE -->
      @if (checkoutState === 'failed' || checkoutState === 'timeout') {
        <div class="checkout-failed">
          <lucide-icon name="alert-circle" [size]="48"></lucide-icon>
          <h3>Payment {{ checkoutState === 'timeout' ? 'Timed Out' : 'Failed' }}</h3>
          <p>{{ error }}</p>
          <div class="checkout-actions">
            <button class="btn btn-secondary" (click)="cancelCheckout()">Close</button>
            <button class="btn btn-primary" (click)="checkoutState = 'idle'; error = ''">Try Again</button>
          </div>
        </div>
      }

    </div>
  </div>
}
```

**Note:** If the existing HTML uses `*ngIf` instead of `@if`, adapt the above to match the existing template's style. The component imports `CommonModule` so both syntaxes work, but use `@if` per CLAUDE.md convention.

- [ ] **Step 3: Add checkout styles to event-detail.component.scss (if it exists) or styles.scss**

Check if `event-detail.component.scss` exists:
```bash
ls "/Users/zimbadev/Documents/Workspace/Peoples Projects/SmartEvents/SmartEvents.UI/src/app/features/events/event-detail/"
```

If `event-detail.component.scss` exists, append these styles to it. Otherwise, append to `SmartEvents.UI/src/styles.scss`:

```scss
/* Checkout overlay */
.checkout-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
  padding: 1rem;
}

.checkout-panel {
  background: var(--white);
  border-radius: var(--radius-lg);
  padding: 2rem;
  width: 100%;
  max-width: 420px;
  box-shadow: var(--shadow-lg);

  h3 {
    font-size: 1.2rem;
    font-weight: 700;
    margin-bottom: .5rem;
  }
}

.checkout-amount {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--primary);
  margin-bottom: 1.5rem;
}

.checkout-actions {
  display: flex;
  gap: .75rem;
  justify-content: flex-end;
  margin-top: 1.25rem;
}

.checkout-hint {
  font-size: .85rem;
  color: var(--text-muted);
  margin-top: .5rem;
}

.checkout-pending,
.checkout-success,
.checkout-failed {
  text-align: center;

  lucide-icon {
    display: block;
    margin: 0 auto 1rem;
  }

  h3 { margin-bottom: .5rem; }
  p { color: var(--text-muted); margin-bottom: .5rem; }
}

.checkout-success lucide-icon { color: var(--success, #22c55e); }
.checkout-failed lucide-icon { color: var(--danger, #ef4444); }

/* Spinner */
.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--border-light);
  border-top-color: var(--primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

- [ ] **Step 4: Build the Angular project**

```bash
cd "/Users/zimbadev/Documents/Workspace/Peoples Projects/SmartEvents/SmartEvents.UI"
npx ng build --configuration development 2>&1 | tail -30
```

Expected: `Application bundle generation complete.` No errors.

If there are TypeScript errors about `phone` not existing on `User`, note that `User.phone` already exists in `core/models/auth.models.ts` — the `currentUser()?.phone` call is valid.

- [ ] **Step 5: Commit**

```bash
git add "SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts" \
        "SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html" \
        "SmartEvents.UI/src/app/core/models/payment.models.ts" \
        "SmartEvents.UI/src/app/core/services/payments.service.ts"
git commit -m "feat: checkout flow with PawaPay phone field and pending/polling UI"
```

---

### Task 7: Update CLAUDE.md changelog

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add changelog entry**

In `CLAUDE.md`, add to the Change Log table:
```
| 2026-07-24 | PawaPay sandbox integration: removed Stripe, integrated AirtelMoney (AIRTEL_ZAMBIA) + MTNMoMo (MTN_ZAMBIA) via PawaPay HTTP API. New PawaPayService + IPawaPayService. PaymentsController.Checkout now creates Pending registration/ticket optimistically and initiates real PawaPay deposit. New GET /api/payments/{id}/status polling endpoint. New POST /api/pawapay/webhook for production callbacks. Frontend: phone field pre-filled from profile, 3 s polling loop (max 40 polls), pending/completed/failed/timeout UI states. EF migration: AddPawaPayFields (PawaPayDepositId on Payment). |
```

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md changelog for PawaPay integration"
```
