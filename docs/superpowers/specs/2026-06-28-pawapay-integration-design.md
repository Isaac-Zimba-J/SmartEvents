# PawaPay Sandbox Integration — Design Spec

**Date:** 2026-06-28
**Scope:** Replace mock mobile money payments with real PawaPay sandbox deposits. Remove Stripe.

---

## Decisions

| Decision | Choice |
|---|---|
| Payment methods | AirtelMoney + MTNMoMo only (Stripe removed) |
| Phone number | Pre-filled from user profile; editable at checkout |
| Ticket/registration timing | Optimistic — created immediately as Pending |
| Async resolution | Hybrid: frontend polls every 3 s; webhook updates DB for production |

---

## Backend Changes

### New: `PawaPayService`
Injectable service (`IInfrastructure/Services/PawaPayService.cs`, registered in `Program.cs`).

Methods:
- `InitiateDeposit(depositId, amount, currency, correspondent, phoneNumber) → PawaPayDepositResponse`
- `GetDepositStatus(depositId) → PawaPayDepositStatus`

Config (appsettings.json):
```json
"PawaPay": {
  "ApiToken": "<sandbox-token>",
  "BaseUrl": "https://api.sandbox.pawapay.cloud"
}
```

PawaPay API calls:
- `POST /deposits` — initiate; auth via `Authorization: Bearer <token>`
- `GET /deposits/{depositId}` — poll status

Correspondent map:
- `AirtelMoney` → `"AIRTEL_ZAMBIA"`
- `MTNMoMo` → `"MTN_ZAMBIA"`

### Updated: `PaymentsController`

`POST /api/payments/checkout`:
- Add `PhoneNumber` to `PaymentCheckoutRequest`
- Call `PawaPayService.InitiateDeposit()`
- Create registration + ticket + payment with `Status = Pending`, store `PawaPayDepositId`
- Return `{ paymentId, status: "Pending", transactionReference, registration }`

`GET /api/payments/{id}/status` (new endpoint):
- Look up payment by id, get `PawaPayDepositId`
- Call `PawaPayService.GetDepositStatus(depositId)`
- Map PawaPay status → `PaymentStatus`, update DB row
- If `COMPLETED`: set `PaidAt`, update registration/ticket status
- If `FAILED`: set registration to Cancelled
- Return current status

### New: `PawaPayWebhookController`

`POST /api/pawapay/webhook` — `[AllowAnonymous]`:
- Parse deposit callback body
- Find payment by `PawaPayDepositId`
- Update status same as polling endpoint
- Fire SignalR notification to user

### Entity Changes

**`Payment`**: add `string? PawaPayDepositId`

**`User`**: add `string? PhoneNumber`

**`PaymentMethod` enum**: remove `Stripe`

**Migration**: `AddPawaPayFields`

---

## Frontend Changes

### Models
- `PaymentMethod` type: remove `'Stripe'`
- `User` interface: add `phoneNumber?: string`
- `PaymentCheckoutRequest`: add `phoneNumber: string`
- New `PaymentStatusResponse` interface

### `PaymentsService`
- Add `getStatus(paymentId: string): Observable<PaymentStatusResponse>`

### `EventDetailComponent` checkout flow
1. Payment method selector: remove Stripe option
2. Add phone number field (pre-filled from `auth.currentUser()?.phoneNumber`)
3. On submit: call checkout → receive pending response
4. Enter polling loop: `getStatus()` every 3 s, max 40 attempts (2 min)
5. States: `idle | pending | completed | failed | timeout`
6. On completed: show success + ticket number
7. On failed/timeout: show error, registration already cancelled by backend

### Profile Page
- Add phone number input field (saved via existing profile update endpoint — needs `PhoneNumber` added to `UpdateProfileRequest`)

---

## Sandbox Test Numbers (PawaPay)

PawaPay sandbox routes based on amount:
- Any valid Zambian number format: `260XXXXXXXXX`
- Sandbox auto-completes deposits — no real phone needed for testing

---

## Out of Scope

- Refunds via PawaPay
- Payout flow
- Real Stripe integration
- Webhook signature verification (add for production)
