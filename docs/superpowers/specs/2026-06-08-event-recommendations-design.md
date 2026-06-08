# Event Recommendations — Design Spec

**Date:** 2026-06-08
**Feature:** "You might also like" personalized event recommendations on the event detail page

---

## Overview

When an authenticated user has at least one confirmed or waitlisted event registration, a "You might also like" section appears at the bottom of every event detail page. It shows up to 4 published events drawn from the categories the user has previously registered for, excluding the current event and any events they are already registered for.

Users with no registrations, or unauthenticated guests, see nothing — the section is absent entirely.

---

## Decisions Made

| Question | Answer |
| --- | --- |
| What triggers recommendations? | Any confirmed or waitlisted registration |
| Where are recommendations shown? | Bottom of the event detail page |
| Similarity logic | User's registration history (categories they've registered for) |
| Fallback when no history | Section hidden entirely |
| Max results | 4 events |
| Ordering | Ascending by `StartDate` (soonest first) |

---

## Backend

### New endpoint

```text
GET /api/events/recommended?excludeEventId={guid}
Authorization: Bearer <token>  (required)
```

**Response:** `200 OK` with `IEnumerable<EventSummaryResponse>` (0–4 items). Returns empty array when the user has no qualifying registrations — the frontend hides the section on empty response.

### Logic (in `EventsController`)

```text
1. Extract userId from JWT claims (ClaimTypes.NameIdentifier)
2. Query Registrations WHERE UserId == userId
                         AND Status IN (Confirmed, Waitlisted)
   → collect distinct EventCategory values
3. If categories is empty → return Ok([])
4. Query Events WHERE Status == Published
                  AND IsPublic == true
                  AND Category IN categories
                  AND Id != excludeEventId
                  AND Id NOT IN (user's registered EventIds)
   → Include Company, Venue, Organizer, Registrations
   → OrderBy StartDate ascending
   → Take 4
5. Return Ok(events.Select(ToSummary))
```

**Authorization:** `[Authorize]` — no role restriction, all authenticated users.

**Includes needed:** `Company`, `Organizer`, `Registrations` (for `RegisteredCount` in `ToSummary`), `Venue` (nullable).

**Reuses:** Existing `ToSummary` and `ToVenueResponse` private static helpers already on `EventsController`. No new DTOs needed.

**No schema changes, no migration needed.**

---

## Frontend

### Service

Add one method to `EventsService` (`core/services/events.service.ts`):

```ts
getRecommended(excludeEventId: string) {
  return this.http.get<EventSummary[]>(
    `${this.apiUrl}/recommended`,
    { params: new HttpParams().set('excludeEventId', excludeEventId) }
  );
}
```

### Component changes (`event-detail.component.ts`)

- Add `recommendations: EventSummary[] = []` field
- `AuthService` is already injected as `public auth` — no change needed
- Inside the existing `getBySlug` success callback, after `this.event = data`, if `auth.isAuthenticated()` call `eventsService.getRecommended(event.id)` and assign result to `recommendations`
- Add `LucideAngularModule` to the component's `imports` array (needed for the `Star` icon in the template)
- No error handling beyond swallowing failures silently — recommendations are non-critical

### Template changes (`event-detail.component.html`)

At the bottom of the template, after the main event content and before the closing `</div>` of `event-detail`:

```html
@if (recommendations.length > 0) {
  <div class="recommendations-section">
    <div class="section-header">
      <lucide-angular name="star" />
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
```

Note: loop variable renamed from `event` to `rec` to avoid shadowing the component's `event` property.

Cards use `routerLink` to `/events/:slug` — navigating to a recommended event reloads that event's detail page, which in turn loads its own recommendations.

---

## Data Flow

```text
User lands on /events/:slug
  → event-detail loads event (existing)
  → if isAuthenticated()
      → GET /api/events/recommended?excludeEventId={id}
          → controller reads user's registration categories
          → returns 0–4 EventSummaryResponse
      → if response.length > 0: render recommendation cards
      → else: section stays hidden
```

---

## Edge Cases

| Scenario | Behaviour |
| --- | --- |
| User has registrations but all matching events are full / cancelled | Returns empty array, section hidden |
| User is registered for only one category and current event is in it | Returns other events in that category, excluding current |
| User has registrations in 3+ categories | Returns up to 4 events mixed across all categories (DB ordering by date, not by category weight) |
| The `excludeEventId` param is omitted | Endpoint still works — just doesn't exclude any event |
| Token expires mid-session | `tokenRefreshInterceptor` handles retry transparently |

---

## Out of Scope

- Recommendation weighting by frequency (e.g., registered for 5 Sports events → rank Sports higher)
- Collaborative filtering ("users like you also liked…")
- A dedicated recommendations page or dashboard section
- Recommendations for guests based on current event category
