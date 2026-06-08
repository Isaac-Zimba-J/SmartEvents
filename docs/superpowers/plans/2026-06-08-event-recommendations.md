# Event Recommendations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "You might also like" section to the event detail page that shows up to 4 personalized event recommendations based on the authenticated user's registration history.

**Architecture:** A new `GET /api/events/recommended?excludeEventId={guid}` endpoint on `EventsController` reads the user's confirmed/waitlisted registration categories and returns matching published events. The Angular `EventsService` gains one method; `EventDetailComponent` calls it inside its existing `getBySlug` callback and conditionally renders a card grid.

**Tech Stack:** ASP.NET Core .NET 10, EF Core 10 (PostgreSQL), Angular 18 (standalone components, `@if`/`@for` control flow), Lucide Angular, global SCSS via `styles.scss`.

---

## Files Changed

| File | Action |
| --- | --- |
| `SmartEvents.API/Controllers/EventsController.cs` | Modify — add `GetRecommended` endpoint |
| `SmartEvents.UI/src/app/core/services/events.service.ts` | Modify — add `getRecommended` method |
| `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts` | Modify — add `recommendations` field, `LucideAngularModule` import, load call |
| `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html` | Modify — add recommendations section |
| `SmartEvents.UI/src/styles.scss` | Modify — add `.recommendations-section`, `.recommendations-grid`, `.rec-card` styles |

---

## Task 1: Add `getRecommended` to `EventsService`

**Files:**
- Modify: `SmartEvents.UI/src/app/core/services/events.service.ts`

- [ ] **Step 1: Open the file and append the new method**

  The file currently ends with the `delete` method. Add `getRecommended` as the last method, before the closing `}`:

  ```ts
  getRecommended(excludeEventId: string) {
    return this.http.get<EventSummary[]>(
      `${this.apiUrl}/recommended`,
      { params: new HttpParams().set('excludeEventId', excludeEventId) }
    );
  }
  ```

  `HttpParams` is already imported at the top of the file (`import { HttpClient, HttpParams } from '@angular/common/http'`). No new imports needed.

- [ ] **Step 2: Verify TypeScript compiles**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -20
  ```

  Expected: build succeeds with no errors (warnings about bundle size are fine).

- [ ] **Step 3: Commit**

  ```bash
  git add SmartEvents.UI/src/app/core/services/events.service.ts
  git commit -m "feat: add getRecommended to EventsService"
  ```

---

## Task 2: Add `GetRecommended` endpoint to `EventsController`

**Files:**
- Modify: `SmartEvents.API/Controllers/EventsController.cs`

- [ ] **Step 1: Add the method after `GetByCompany` (before the `Create` method)**

  Insert this block after the closing `}` of `GetByCompany` and before `[HttpPost]`:

  ```csharp
  [HttpGet("recommended")]
  [Authorize]
  public async Task<ActionResult<IEnumerable<EventSummaryResponse>>> GetRecommended(
      [FromQuery] Guid? excludeEventId = null)
  {
      var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

      var categories = await db.Registrations
          .Where(r => r.UserId == userId &&
                      (r.Status == RegistrationStatus.Confirmed ||
                       r.Status == RegistrationStatus.Waitlisted))
          .Select(r => r.Event.Category)
          .Distinct()
          .ToListAsync();

      if (categories.Count == 0)
          return Ok(Array.Empty<EventSummaryResponse>());

      var registeredEventIds = await db.Registrations
          .Where(r => r.UserId == userId)
          .Select(r => r.EventId)
          .ToListAsync();

      var events = await db.Events
          .Include(e => e.Company)
          .Include(e => e.Venue)
          .Include(e => e.Organizer)
          .Include(e => e.Registrations)
          .Where(e => e.Status == EventStatus.Published
                   && e.IsPublic
                   && categories.Contains(e.Category)
                   && !registeredEventIds.Contains(e.Id)
                   && (excludeEventId == null || e.Id != excludeEventId.Value))
          .OrderBy(e => e.StartDate)
          .Take(4)
          .ToListAsync();

      return Ok(events.Select(ToSummary));
  }
  ```

  All types used (`RegistrationStatus`, `EventStatus`, `EventSummaryResponse`) are already imported at the top of the file. No new usings needed.

- [ ] **Step 2: Build the API to check for errors**

  ```bash
  cd SmartEvents.API
  dotnet build 2>&1 | tail -20
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Run the API and smoke-test the endpoint**

  ```bash
  dotnet run
  ```

  Open `http://localhost:5148/scalar`. Find `GET /api/events/recommended`. Authenticate using the lock icon with the `attendee@example.com` token (login first via `POST /api/auth/login` with `{ "email": "attendee@example.com", "password": "Seed1234!" }`).

  - Call with no query params → should return `[]` (attendee has no registrations in a fresh DB).
  - Register the attendee for an event first via `POST /api/registrations` `{ "eventId": "<any published event id>" }`, then call `GET /api/events/recommended` again → should return up to 4 events in the same category.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.API/Controllers/EventsController.cs
  git commit -m "feat: add GET /api/events/recommended endpoint"
  ```

---

## Task 3: Update `EventDetailComponent` to load recommendations

**Files:**
- Modify: `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts`

- [ ] **Step 1: Add the `recommendations` field and `LucideAngularModule` import**

  Replace the existing `@Component` decorator and class opening:

  ```ts
  import { Component, OnInit } from '@angular/core';
  import { CommonModule } from '@angular/common';
  import { FormsModule } from '@angular/forms';
  import { ActivatedRoute, RouterLink } from '@angular/router';
  import { LucideAngularModule } from 'lucide-angular';
  import { EventsService } from '../../../core/services/events.service';
  import { RegistrationsService } from '../../../core/services/registrations.service';
  import { PaymentsService } from '../../../core/services/payments.service';
  import { AuthService } from '../../../core/services/auth.service';
  import { EventDetail, EventSummary } from '../../../core/models/event.models';
  import { RegistrationResponse } from '../../../core/models/registration.models';
  import { PaymentMethod } from '../../../core/models/payment.models';

  @Component({
    selector: 'app-event-detail',
    standalone: true,
    imports: [CommonModule, RouterLink, FormsModule, LucideAngularModule],
    templateUrl: './event-detail.component.html'
  })
  export class EventDetailComponent implements OnInit {
    event: EventDetail | null = null;
    loading = true;
    acting = false;
    registration: RegistrationResponse | null = null;
    error = '';
    successMessage = '';
    recommendations: EventSummary[] = [];

    showCheckout = false;
    selectedPaymentMethod: PaymentMethod = 'Stripe';
  ```

  Leave the rest of the class (`paymentMethods`, `constructor`, all methods) unchanged.

- [ ] **Step 2: Update `ngOnInit` to load recommendations after the event loads**

  Replace the existing `ngOnInit` method:

  ```ts
  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.eventsService.getBySlug(slug).subscribe({
      next: data => {
        this.event = data;
        this.loading = false;
        if (this.auth.isAuthenticated()) {
          this.eventsService.getRecommended(data.id).subscribe({
            next: recs => { this.recommendations = recs; },
            error: () => {}
          });
        }
      },
      error: () => { this.loading = false; }
    });
  }
  ```

- [ ] **Step 3: Verify TypeScript compiles**

  ```bash
  cd SmartEvents.UI
  npx ng build --configuration development 2>&1 | tail -20
  ```

  Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

  ```bash
  git add SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.ts
  git commit -m "feat: load recommendations in EventDetailComponent"
  ```

---

## Task 4: Add "You might also like" section to the template

**Files:**
- Modify: `SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html`

- [ ] **Step 1: Add the recommendations section**

  Inside the `@else if (event)` block, find the back-link line:

  ```html
      <a routerLink="/events">&larr; Back to Events</a>
    </div>
  ```

  Replace it with:

  ```html
      <a routerLink="/events">&larr; Back to Events</a>

      @if (recommendations.length > 0) {
        <div class="recommendations-section">
          <div class="recommendations-header">
            <lucide-angular name="star" [size]="18"></lucide-angular>
            <h3>You might also like</h3>
            <span class="badge">Based on your registrations</span>
          </div>
          <div class="recommendations-grid">
            @for (rec of recommendations; track rec.id) {
              <a [routerLink]="['/events', rec.slug]" class="rec-card">
                <span class="badge">{{ rec.category }}</span>
                <p class="rec-title">{{ rec.title }}</p>
                <p class="rec-meta">{{ rec.startDate | date:'mediumDate' }} · {{ rec.companyName }}</p>
                <p class="rec-price">{{ rec.isTicketed ? (rec.ticketPrice | currency:'ZMW':'symbol-narrow') : 'Free' }}</p>
              </a>
            }
          </div>
        </div>
      }
    </div>
  ```

  Note: the loop variable is `rec` (not `event`) to avoid shadowing the component's `event` property.

- [ ] **Step 2: Commit**

  ```bash
  git add SmartEvents.UI/src/app/features/events/event-detail/event-detail.component.html
  git commit -m "feat: add You might also like section to event detail template"
  ```

---

## Task 5: Add styles for recommendation cards

**Files:**
- Modify: `SmartEvents.UI/src/styles.scss`

- [ ] **Step 1: Add the recommendations styles**

  Find the comment `/* ─── Event detail ───────────────────────────────────────── */` (around line 463). After the entire event-detail block (after the `.checkout-actions` and `.mock-card-fields` rules, before `/* ─── My Registrations */`), insert:

  ```scss
  /* ─── Recommendations ────────────────────────────────────── */
  .recommendations-section {
    margin-top: 2.5rem; padding-top: 2rem; border-top: 1px solid var(--border);
  }

  .recommendations-header {
    display: flex; align-items: center; gap: .6rem; margin-bottom: 1.25rem;
    h3 { margin: 0; }
    .badge { font-size: .65rem; }
  }

  .recommendations-grid {
    display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem;
  }

  .rec-card {
    display: flex; flex-direction: column; gap: .35rem;
    padding: 1rem 1.1rem; border-radius: var(--radius-lg);
    border: 1.5px solid var(--border); background: var(--white);
    box-shadow: var(--shadow-sm); color: var(--text);
    transition: box-shadow var(--transition), border-color var(--transition), transform var(--transition);

    &:hover {
      box-shadow: var(--shadow); border-color: var(--primary);
      transform: translateY(-2px); color: var(--text);
    }

    .rec-title { font-size: .9rem; font-weight: 600; margin-top: .15rem; }
    .rec-meta  { font-size: .75rem; color: var(--text-muted); }
    .rec-price { font-size: .85rem; font-weight: 700; color: var(--primary); margin-top: auto; padding-top: .35rem; }
  }
  ```

- [ ] **Step 2: Add the mobile breakpoint rule**

  Find the `@media (max-width: 640px)` block near the bottom of `styles.scss`. Inside it, add `.recommendations-grid` to the list of overrides:

  ```scss
  @media (max-width: 640px) {
    /* ...existing rules... */
    .recommendations-grid { grid-template-columns: 1fr; }
  }
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add SmartEvents.UI/src/styles.scss
  git commit -m "feat: add recommendation card styles"
  ```

---

## Task 6: End-to-end verification

- [ ] **Step 1: Run both servers**

  Terminal 1 (API):
  ```bash
  cd SmartEvents.API && dotnet run
  ```

  Terminal 2 (UI):
  ```bash
  cd SmartEvents.UI && npm start
  ```

- [ ] **Step 2: Test as attendee with no registrations**

  - Log in as `attendee@example.com` / `Seed1234!`
  - Navigate to any event detail page (`http://localhost:4200/events/<slug>`)
  - Expected: no "You might also like" section visible

- [ ] **Step 3: Register for an event and verify recommendations appear**

  - On any published event detail page, click "Register Now" or "Buy Ticket"
  - After successful registration, navigate to a **different** event detail page in the same category (or another category to see no results)
  - Expected: "You might also like" section appears with up to 4 cards matching the registered category

- [ ] **Step 4: Verify clicking a recommendation card works**

  - Click one of the recommendation cards
  - Expected: navigates to that event's detail page; that page may or may not show its own recommendations depending on whether the user has registrations in that event's category

- [ ] **Step 5: Test as guest (logged out)**

  - Log out, navigate to any event detail page
  - Expected: no "You might also like" section

- [ ] **Step 6: Update CLAUDE.md change log**

  Open `CLAUDE.md` at the repo root and append a row to the Change Log table:

  ```markdown
  | 2026-06-08 | Added "You might also like" recommendations on event detail page. New endpoint GET /api/events/recommended; EventsService.getRecommended; EventDetailComponent updated. |
  ```

- [ ] **Step 7: Final commit**

  ```bash
  git add CLAUDE.md
  git commit -m "docs: update CLAUDE.md with recommendations feature"
  ```
