# Landing Page — Design Spec

Date: 2026-07-14

## Goal

SmartEvents currently has no public marketing page — the root path (`''`) redirects straight to `/dashboard`, which is auth-guarded, so any unauthenticated visitor is immediately bounced to `/auth/login`. This adds a proper public landing page at `/` that showcases the product and lists partner/client organizations, while preserving existing behavior for logged-in users (they still land on `/dashboard`).

## Routing Change

`SmartEvents.UI/src/app/app.routes.ts`:

- Replace the current `{ path: '', redirectTo: 'dashboard', pathMatch: 'full' }` entry with:
  ```ts
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent)
  }
  ```
- `guestGuard` (existing, in `core/guards/auth.guard.ts`) already redirects authenticated users to `/dashboard` and allows unauthenticated users through — this is exactly the behavior needed, so no new guard is introduced.

## New Feature: `features/landing/`

Single standalone component (no sub-components — one cohesive page, not complex enough to warrant splitting):

- `landing.component.ts`
- `landing.component.html`
- `landing.component.scss`

No routes file needed since it's a single `loadComponent` entry directly in `app.routes.ts` (consistent with how `profile` is wired).

### Sections (top to bottom)

1. **Nav bar** — sticky, transparent over hero, becomes solid on scroll. "SmartEvents" wordmark on the left; "Login" (ghost/secondary button) and "Get Started" (primary button) on the right, linking to `/auth/login` and `/auth/register` respectively.

2. **Hero** — Headline + subheadline focused on running events in Zambia (ticketing, QR check-in, multi-company support). Primary CTA "Get Started" → `/auth/register`. Secondary CTA "Sign In" → `/auth/login`. Background: gradient/blob treatment built from existing CSS custom properties (`--primary`, `--primary-dark`, `--primary-light`) — no product screenshot exists to embed, so the hero relies on typography + color + icon accents instead of a mockup image.

3. **Partners marquee** — Heading: "Trusted by organizations across Zambia." Infinite auto-scrolling horizontal strip of all 15 logos from the project's `images/` folder. Implementation: pure CSS `@keyframes` translateX loop (the logo `<li>` list is duplicated once in the template so the loop is seamless — no JS/animation library). Each logo sits in a uniform fixed-size white rounded card using `object-fit: contain` (prevents cropping/distortion given inconsistent source aspect ratios); logos render grayscale (`filter: grayscale(1)`) by default and switch to full color on hover for polish.

4. **Features grid** — 6 cards, each with a Lucide icon + short title + one-line description:
   - Ticketing & QR Check-in (`Ticket` / `QrCode`)
   - Multi-Company Events (`Building2`)
   - Real-time Notifications (`Bell`)
   - Secure Payments — Stripe/Airtel Money/MTN MoMo (`DollarSign`)
   - Venue Booking (`BookMarked`)
   - Analytics Dashboard (`BarChart2`)

5. **How it works** — 4 numbered horizontal steps: Create Your Event → Publish & Sell Tickets → Check Attendees In via QR → Track Performance.

6. **Final CTA banner** — "Ready to run your next event?" heading + "Get Started" button → `/auth/register`.

7. **Footer** — SmartEvents brand mark, quick links (Login, Get Started, Browse Events → `/events`), copyright line.

## Assets

- Copy all 15 JPEGs from the project-root `images/` folder into `SmartEvents.UI/public/images/partners/`, preserved as-is (filenames untouched). Angular serves everything under `public/` from the site root (same mechanism already serving `favicon.ico`), so templates reference them as `/images/partners/<filename>.jpeg`.
- The 15 files: africafest, agritech zambia expo, cbu, elite deco events and hiring zambia, fnb, garden court, lusaka fashion house, zambia tourism adventure, zamfilm, fnb kopala run, copper eagles football academy, lusaka arts collective, mukuba fitness, zambia innovation forum, blazer events.

## Icons

Add to the `LucideAngularModule.pick({...})` call in `app.config.ts`:
- `ArrowRight` (CTA buttons)
- `Sparkles` (hero accent)

(`Ticket`, `QrCode`, `Building2`, `Bell`, `DollarSign`, `BookMarked`, `BarChart2`, `Check`, `Star` are already registered and reused here.)

## Styling

- Follows existing conventions: inline Tailwind-style-ish class strings are NOT used in this codebase (per CLAUDE.md, raw SCSS is used) — `landing.component.scss` will be a component-scoped SCSS file using the existing CSS custom properties (`--primary`, `--radius`, `--shadow`, `--font`, etc.) from `styles.scss` rather than hardcoded values, so the page stays visually consistent with the rest of the app.
- Fully responsive: nav collapses CTAs appropriately on mobile, features grid reflows to 1 column, marquee remains horizontally scrolling at all widths.

## Explicitly Out of Scope

- No backend/API changes.
- No new Angular services.
- No product screenshots/mockups (none exist to source).
- No testimonials or stats-band section (declined during design).
- No CMS/dynamic content — all copy and the partner logo list are static in the template.
