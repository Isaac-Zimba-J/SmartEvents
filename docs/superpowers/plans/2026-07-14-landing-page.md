# Landing Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a public marketing landing page at `/` (SmartEvents Angular SPA) with a hero, a scrolling partner/client logo marquee, a features grid, a "how it works" section, a final CTA, and a footer — replacing the current forced redirect to the auth-gated dashboard.

**Architecture:** One new standalone Angular component, `LandingComponent`, loaded directly via `loadComponent` at `path: ''` in `app.routes.ts`, guarded by the existing `guestGuard` (already redirects authenticated users to `/dashboard`, already allows guests through — no new guard needed). The component is a single template built up section-by-section across tasks, styled with a single scoped SCSS file using the app's existing CSS custom properties. 15 partner logo images are copied from the project-root `images/` folder into `SmartEvents.UI/public/images/partners/` with cleaned-up filenames.

**Tech Stack:** Angular 18 standalone components, native `@for` control-flow syntax (this codebase does not use `*ngFor`), `lucide-angular` for icons, plain SCSS (no Tailwind) using CSS custom properties already defined in `src/styles.scss`.

## Global Constraints

- No NgModules — every component is `standalone: true` with explicit `imports: [...]` (per CLAUDE.md).
- Use `@for` / `@if` native control flow in templates, not `*ngFor` / `*ngIf` — confirmed as this codebase's convention (`login.component.html`, `register.component.html`, `verify-email.component.html` all use it).
- Icons: register any new icon in the `LucideAngularModule.pick({...})` call in `SmartEvents.UI/src/app/app.config.ts`, then reference it in templates as `<lucide-icon name="IconName" [size]="n" />` (confirmed pattern from `login.component.html`).
- Styling: raw SCSS only, no Tailwind (CLAUDE.md: "Tailwind is NOT installed"). Reuse the CSS custom properties already defined at the top of `SmartEvents.UI/src/styles.scss` (`--primary`, `--text`, `--radius`, `--shadow`, `--border-light`, etc.) rather than hardcoding colors/spacing. Reuse the existing global `.btn-primary` / `.btn-secondary` / `.btn-ghost-sm` button classes from `styles.scss` instead of redefining button styles.
- Verification method: this codebase has no component-level unit tests except `app.component.spec.ts` (per CLAUDE.md: "Spec file only present for `app.component.spec.ts`") — do not add Jasmine spec files for `LandingComponent`. Verify each task with `npm run build` (catches template/type errors) and a manual browser check via `npm start` (per CLAUDE.md: "For UI or frontend changes, start the dev server and use the feature in a browser before reporting the task as complete").
- All commands below run from `SmartEvents.UI/` (the Angular project root) unless otherwise noted.

---

### Task 1: Copy and clean up partner logo assets

**Files:**
- Create: `SmartEvents.UI/public/images/partners/africafest.jpeg`
- Create: `SmartEvents.UI/public/images/partners/agritech-zambia-expo.jpeg`
- Create: `SmartEvents.UI/public/images/partners/cbu.jpeg`
- Create: `SmartEvents.UI/public/images/partners/elite-deco-events.jpeg`
- Create: `SmartEvents.UI/public/images/partners/fnb.jpeg`
- Create: `SmartEvents.UI/public/images/partners/garden-court.jpeg`
- Create: `SmartEvents.UI/public/images/partners/lusaka-fashion-house.jpeg`
- Create: `SmartEvents.UI/public/images/partners/zambia-tourism-adventure.jpeg`
- Create: `SmartEvents.UI/public/images/partners/zamfilm.jpeg`
- Create: `SmartEvents.UI/public/images/partners/fnb-kopala-run.jpeg`
- Create: `SmartEvents.UI/public/images/partners/copper-eagles-academy.jpeg`
- Create: `SmartEvents.UI/public/images/partners/lusaka-arts-collective.jpeg`
- Create: `SmartEvents.UI/public/images/partners/mukuba-fitness.jpeg`
- Create: `SmartEvents.UI/public/images/partners/zambia-innovation-forum.jpeg`
- Create: `SmartEvents.UI/public/images/partners/blazer-events.jpeg`

**Interfaces:**
- Produces: a stable, clean-filename asset directory at `public/images/partners/<slug>.jpeg`. Task 4 (Partners marquee) hardcodes these exact 15 filenames in `LandingComponent`'s `partners` array and references them as `/images/partners/<slug>.jpeg` (Angular serves everything under `public/` from the site root, same mechanism as the existing `favicon.ico`).

The source files live in the project-root `images/` folder (outside the Angular project) and have inconsistent, space-containing names. This task copies them into the Angular `public/` folder with clean slug names so they're safe to reference in `src`/`href` attributes without URL-encoding concerns.

- [ ] **Step 1: Create the destination directory**

```bash
mkdir -p "SmartEvents.UI/public/images/partners"
```

- [ ] **Step 2: Copy each file with its clean destination name**

Run from the repository root (one directory above `SmartEvents.UI/`):

```bash
SRC="images"
DEST="SmartEvents.UI/public/images/partners"

cp "$SRC/image africafest.logo.jpeg" "$DEST/africafest.jpeg"
cp "$SRC/image agritech zambia expo.logo.jpeg" "$DEST/agritech-zambia-expo.jpeg"
cp "$SRC/image cbu.logo.jpeg" "$DEST/cbu.jpeg"
cp "$SRC/image elite deco events and hiring zambia.logo.jpeg" "$DEST/elite-deco-events.jpeg"
cp "$SRC/image fnb.logo.jpeg" "$DEST/fnb.jpeg"
cp "$SRC/image garden court.logo.jpeg" "$DEST/garden-court.jpeg"
cp "$SRC/image lusaka fashion house.logo.jpeg" "$DEST/lusaka-fashion-house.jpeg"
cp "$SRC/image zambia tourism adventure.logo.jpeg" "$DEST/zambia-tourism-adventure.jpeg"
cp "$SRC/image zamfilm.logo.jpeg" "$DEST/zamfilm.jpeg"
cp "$SRC/image. fnb kopala run .jpeg" "$DEST/fnb-kopala-run.jpeg"
cp "$SRC/image.copper eagles football academy.logo.jpeg" "$DEST/copper-eagles-academy.jpeg"
cp "$SRC/image.lusaka arts collective.logo.jpeg" "$DEST/lusaka-arts-collective.jpeg"
cp "$SRC/image.mukuba fitness.logo.jpeg" "$DEST/mukuba-fitness.jpeg"
cp "$SRC/image.zambia innovation forum.logo.jpeg" "$DEST/zambia-innovation-forum.jpeg"
cp "$SRC/images.blazer events.logo.jpeg" "$DEST/blazer-events.jpeg"
```

- [ ] **Step 3: Verify all 15 files copied**

Run: `ls "SmartEvents.UI/public/images/partners" | wc -l`
Expected: `15`

Run: `ls "SmartEvents.UI/public/images/partners"`
Expected: exactly these 15 filenames (any order): `africafest.jpeg`, `agritech-zambia-expo.jpeg`, `blazer-events.jpeg`, `cbu.jpeg`, `copper-eagles-academy.jpeg`, `elite-deco-events.jpeg`, `fnb-kopala-run.jpeg`, `fnb.jpeg`, `garden-court.jpeg`, `lusaka-arts-collective.jpeg`, `lusaka-fashion-house.jpeg`, `mukuba-fitness.jpeg`, `zambia-innovation-forum.jpeg`, `zambia-tourism-adventure.jpeg`, `zamfilm.jpeg`

- [ ] **Step 4: Commit**

```bash
git add SmartEvents.UI/public/images/partners
git commit -m "chore: add partner/client logo assets for landing page"
```

---

### Task 2: Scaffold LandingComponent and wire the root route

**Files:**
- Create: `SmartEvents.UI/src/app/features/landing/landing.component.ts`
- Create: `SmartEvents.UI/src/app/features/landing/landing.component.html`
- Create: `SmartEvents.UI/src/app/features/landing/landing.component.scss`
- Modify: `SmartEvents.UI/src/app/app.routes.ts`

**Interfaces:**
- Consumes: `guestGuard` from `SmartEvents.UI/src/app/core/guards/auth.guard.ts` (already imported at the top of `app.routes.ts` — no new import needed).
- Produces: `LandingComponent` (standalone, selector `app-landing`) at route `path: ''`. Tasks 3–6 add markup inside this component's template and styles inside its stylesheet; none of them touch `app.routes.ts` again.

- [ ] **Step 1: Create the component TypeScript file**

`SmartEvents.UI/src/app/features/landing/landing.component.ts`:

```ts
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {}
```

- [ ] **Step 2: Create the template with just the page wrapper**

`SmartEvents.UI/src/app/features/landing/landing.component.html`:

```html
<div class="landing">
  <!-- SECTIONS GO HERE -->
</div>
```

- [ ] **Step 3: Create an empty stylesheet**

`SmartEvents.UI/src/app/features/landing/landing.component.scss`:

```scss
:host {
  display: block;
}

.landing {
  overflow-x: hidden;
}
```

- [ ] **Step 4: Wire the root route to LandingComponent**

In `SmartEvents.UI/src/app/app.routes.ts`, find:

```ts
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
```

Replace with:

```ts
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent)
  },
```

- [ ] **Step 5: Verify the build compiles**

Run: `cd SmartEvents.UI && npm run build`
Expected: build succeeds with no errors.

- [ ] **Step 6: Verify in the browser**

Run: `npm start` (from `SmartEvents.UI/`), then open `http://localhost:4200/` in a browser while logged out (clear `se_token`/`se_refresh`/`se_user` from localStorage, or use an incognito window).
Expected: a blank page loads at `/` (no redirect to `/auth/login`, no console errors). Then log in and visit `http://localhost:4200/` again.
Expected: automatically redirected to `/dashboard` (guestGuard behavior).

- [ ] **Step 7: Commit**

```bash
git add SmartEvents.UI/src/app/features/landing SmartEvents.UI/src/app/app.routes.ts
git commit -m "feat: scaffold LandingComponent and wire it to the root route"
```

---

### Task 3: Nav + Hero section

**Files:**
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.html`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.scss`
- Modify: `SmartEvents.UI/src/app/app.config.ts`

**Interfaces:**
- Consumes: the `<!-- SECTIONS GO HERE -->` marker in `landing.component.html` from Task 2.
- Produces: leaves a `<!-- HERO END -->` marker in the template for Task 4 to insert after.

- [ ] **Step 1: Register the two new icons this page needs**

In `SmartEvents.UI/src/app/app.config.ts`, find the icon import list:

```ts
import {
  LucideAngularModule,
  Eye, EyeOff,
  LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
  Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
  Check, X, Shield, Mail, Phone, Globe, Clock,
  Search, ChevronRight, Settings, AlertCircle, CheckCircle,
  ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell
} from 'lucide-angular';
```

Replace with:

```ts
import {
  LucideAngularModule,
  Eye, EyeOff,
  LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
  Plus, Pencil, Trash2, ArrowLeft, ArrowRight, Ticket, QrCode,
  Check, X, Shield, Mail, Phone, Globe, Clock,
  Search, ChevronRight, Settings, AlertCircle, CheckCircle,
  ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell, Sparkles
} from 'lucide-angular';
```

Then find:

```ts
    importProvidersFrom(LucideAngularModule.pick({
      Eye, EyeOff,
      LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
      Plus, Pencil, Trash2, ArrowLeft, Ticket, QrCode,
      Check, X, Shield, Mail, Phone, Globe, Clock,
      Search, ChevronRight, Settings, AlertCircle, CheckCircle,
      ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell
    }))
```

Replace with:

```ts
    importProvidersFrom(LucideAngularModule.pick({
      Eye, EyeOff,
      LayoutDashboard, Calendar, MapPin, Building2, Users, BarChart2, User, LogOut,
      Plus, Pencil, Trash2, ArrowLeft, ArrowRight, Ticket, QrCode,
      Check, X, Shield, Mail, Phone, Globe, Clock,
      Search, ChevronRight, Settings, AlertCircle, CheckCircle,
      ScanLine, Star, Tag, DollarSign, Briefcase, BookMarked, Bell, Sparkles
    }))
```

- [ ] **Step 2: Add the Nav and Hero markup**

In `SmartEvents.UI/src/app/features/landing/landing.component.html`, find:

```html
<div class="landing">
  <!-- SECTIONS GO HERE -->
</div>
```

Replace with:

```html
<div class="landing">
  <!-- NAV -->
  <nav class="nav">
    <div class="nav__inner">
      <a routerLink="/" class="nav__brand">
        <lucide-icon name="Sparkles" [size]="20" />
        <span>SmartEvents</span>
      </a>
      <div class="nav__actions">
        <a routerLink="/auth/login" class="btn-ghost-sm">Sign In</a>
        <a routerLink="/auth/register" class="btn-primary">Get Started</a>
      </div>
    </div>
  </nav>
  <!-- NAV END -->

  <!-- HERO -->
  <header class="hero">
    <div class="hero__inner">
      <span class="hero__badge">Built for events across Zambia</span>
      <h1 class="hero__title">Run unforgettable events, from RSVP to check-in.</h1>
      <p class="hero__subtitle">
        SmartEvents gives organizers everything they need to publish events, sell tickets,
        and check attendees in with a single scan — all in one platform, priced in Kwacha.
      </p>
      <div class="hero__actions">
        <a routerLink="/auth/register" class="btn-primary hero__cta">
          Get Started Free
          <lucide-icon name="ArrowRight" [size]="18" />
        </a>
        <a routerLink="/events" class="btn-secondary">Browse Events</a>
      </div>
    </div>
  </header>
  <!-- HERO END -->

  <!-- SECTIONS GO HERE -->
</div>
```

- [ ] **Step 3: Add Nav and Hero styles**

In `SmartEvents.UI/src/app/features/landing/landing.component.scss`, find:

```scss
.landing {
  overflow-x: hidden;
}
```

Replace with:

```scss
.landing {
  overflow-x: hidden;
}

/* NAV */
.nav {
  position: sticky;
  top: 0;
  z-index: 50;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--border-light);

  &__inner {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1rem 1.5rem;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  &__brand {
    display: flex;
    align-items: center;
    gap: .5rem;
    font-weight: 700;
    font-size: 1.1rem;
    color: var(--text);

    lucide-icon { color: var(--primary); }

    &:hover { color: var(--text); }
  }

  &__actions {
    display: flex;
    align-items: center;
    gap: .75rem;
  }
}

/* HERO */
.hero {
  position: relative;
  padding: 5rem 1.5rem 6rem;
  background:
    radial-gradient(circle at 15% 20%, var(--primary-light) 0%, transparent 45%),
    radial-gradient(circle at 85% 0%, var(--primary-muted) 0%, transparent 50%),
    var(--bg);
  overflow: hidden;

  &__inner {
    max-width: 760px;
    margin: 0 auto;
    text-align: center;
  }

  &__badge {
    display: inline-block;
    padding: .35rem .9rem;
    border-radius: 999px;
    background: var(--primary-muted);
    color: var(--primary-dark);
    font-size: .8rem;
    font-weight: 600;
    margin-bottom: 1.5rem;
  }

  &__title {
    font-size: 2.75rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    color: var(--text);
    margin-bottom: 1.25rem;

    @media (max-width: 640px) {
      font-size: 2rem;
    }
  }

  &__subtitle {
    font-size: 1.125rem;
    color: var(--text-muted);
    max-width: 560px;
    margin: 0 auto 2.25rem;
  }

  &__actions {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    flex-wrap: wrap;
  }

  &__cta {
    font-size: 1rem;
    padding: .8rem 1.75rem;
  }
}
```

- [ ] **Step 4: Verify the build compiles**

Run: `cd SmartEvents.UI && npm run build`
Expected: build succeeds with no errors (confirms `Sparkles`/`ArrowRight` icons resolve and template syntax is valid).

- [ ] **Step 5: Verify in the browser**

Run: `npm start`, open `http://localhost:4200/` logged out.
Expected: sticky nav with "SmartEvents" brand + "Sign In"/"Get Started" buttons; hero with badge, headline, subheadline, and two CTA buttons. Click "Sign In" → navigates to `/auth/login`. Click "Get Started" (either button) → navigates to `/auth/register`. Click "Browse Events" → navigates to `/events`.

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.UI/src/app/features/landing SmartEvents.UI/src/app/app.config.ts
git commit -m "feat: add landing page nav and hero section"
```

---

### Task 4: Partners marquee section

**Files:**
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.ts`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.html`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.scss`

**Interfaces:**
- Consumes: the 15 files in `SmartEvents.UI/public/images/partners/` from Task 1; the `<!-- HERO END -->` marker from Task 3.
- Produces: a `partners: { name: string; file: string }[]` field on `LandingComponent`; leaves a `<!-- PARTNERS END -->` marker for Task 5 to insert after.

- [ ] **Step 1: Add the partners data array to the component**

In `SmartEvents.UI/src/app/features/landing/landing.component.ts`, find:

```ts
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {}
```

Replace with:

```ts
interface Partner {
  name: string;
  file: string;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LucideAngularModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  readonly partners: Partner[] = [
    { name: 'AfrikaFest', file: 'africafest.jpeg' },
    { name: 'Agritech Zambia Expo', file: 'agritech-zambia-expo.jpeg' },
    { name: 'Copperbelt University', file: 'cbu.jpeg' },
    { name: 'Elite Deco Events & Hiring Zambia', file: 'elite-deco-events.jpeg' },
    { name: 'FNB', file: 'fnb.jpeg' },
    { name: 'Garden Court', file: 'garden-court.jpeg' },
    { name: 'Lusaka Fashion House', file: 'lusaka-fashion-house.jpeg' },
    { name: 'Zambia Tourism Adventure', file: 'zambia-tourism-adventure.jpeg' },
    { name: 'ZamFilm', file: 'zamfilm.jpeg' },
    { name: 'FNB Kopala Run', file: 'fnb-kopala-run.jpeg' },
    { name: 'Copper Eagles Football Academy', file: 'copper-eagles-academy.jpeg' },
    { name: 'Lusaka Arts Collective', file: 'lusaka-arts-collective.jpeg' },
    { name: 'Mukuba Fitness', file: 'mukuba-fitness.jpeg' },
    { name: 'Zambia Innovation Forum', file: 'zambia-innovation-forum.jpeg' },
    { name: 'Blazer Events', file: 'blazer-events.jpeg' }
  ];
}
```

- [ ] **Step 2: Add the marquee markup**

In `SmartEvents.UI/src/app/features/landing/landing.component.html`, find:

```html
  <!-- HERO END -->

  <!-- SECTIONS GO HERE -->
</div>
```

Replace with:

```html
  <!-- HERO END -->

  <!-- PARTNERS -->
  <section class="partners">
    <div class="partners__inner">
      <p class="partners__heading">Trusted by organizations across Zambia</p>
      <div class="partners__marquee">
        <ul class="partners__track">
          @for (partner of partners; track partner.file) {
            <li class="partners__logo">
              <img [src]="'/images/partners/' + partner.file" [alt]="partner.name" loading="lazy" />
            </li>
          }
          @for (partner of partners; track partner.file + '-dup') {
            <li class="partners__logo" aria-hidden="true">
              <img [src]="'/images/partners/' + partner.file" [alt]="partner.name" loading="lazy" />
            </li>
          }
        </ul>
      </div>
    </div>
  </section>
  <!-- PARTNERS END -->

  <!-- SECTIONS GO HERE -->
</div>
```

- [ ] **Step 3: Add marquee styles**

In `SmartEvents.UI/src/app/features/landing/landing.component.scss`, add this block at the end of the file:

```scss

/* PARTNERS */
.partners {
  padding: 3.5rem 0;
  background: var(--white);
  border-top: 1px solid var(--border-light);
  border-bottom: 1px solid var(--border-light);

  &__inner {
    max-width: 1200px;
    margin: 0 auto;
  }

  &__heading {
    text-align: center;
    font-size: .875rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: .06em;
    color: var(--text-light);
    margin-bottom: 2rem;
    padding: 0 1.5rem;
  }

  &__marquee {
    overflow: hidden;
    -webkit-mask-image: linear-gradient(to right, transparent, black 8%, black 92%, transparent);
    mask-image: linear-gradient(to right, transparent, black 8%, black 92%, transparent);
  }

  &__track {
    display: flex;
    align-items: center;
    gap: 2.5rem;
    width: max-content;
    list-style: none;
    animation: partners-scroll 32s linear infinite;

    .partners__marquee:hover & {
      animation-play-state: paused;
    }
  }

  &__logo {
    flex: 0 0 auto;
    width: 148px;
    height: 88px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--white);
    border: 1px solid var(--border-light);
    border-radius: var(--radius);
    box-shadow: var(--shadow-sm);
    padding: 1rem;

    img {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
      filter: grayscale(1);
      opacity: .75;
      transition: filter var(--transition), opacity var(--transition);
    }

    &:hover img {
      filter: grayscale(0);
      opacity: 1;
    }
  }
}

@keyframes partners-scroll {
  from { transform: translateX(0); }
  to { transform: translateX(-50%); }
}
```

- [ ] **Step 4: Verify the build compiles**

Run: `cd SmartEvents.UI && npm run build`
Expected: build succeeds with no errors.

- [ ] **Step 5: Verify in the browser**

Run: `npm start`, open `http://localhost:4200/` logged out.
Expected: below the hero, a "Trusted by organizations across Zambia" heading followed by a horizontally auto-scrolling row of 15 logo cards, looping seamlessly with no visible jump or gap. Logos appear grayscale and turn full-color on hover; hovering anywhere in the strip pauses the scroll. No broken image icons (confirms the Task 1 filenames match exactly).

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.UI/src/app/features/landing
git commit -m "feat: add partner logo marquee to landing page"
```

---

### Task 5: Features grid + How It Works sections

**Files:**
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.ts`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.html`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.scss`

**Interfaces:**
- Consumes: the `<!-- PARTNERS END -->` marker from Task 4.
- Produces: `features: { icon: string; title: string; description: string }[]` and `steps: { number: string; title: string; description: string }[]` fields on `LandingComponent`; leaves a `<!-- HOW IT WORKS END -->` marker for Task 6 to insert after.

- [ ] **Step 1: Add features and steps data arrays**

In `SmartEvents.UI/src/app/features/landing/landing.component.ts`, find:

```ts
interface Partner {
  name: string;
  file: string;
}
```

Replace with:

```ts
interface Partner {
  name: string;
  file: string;
}

interface Feature {
  icon: string;
  title: string;
  description: string;
}

interface Step {
  number: string;
  title: string;
  description: string;
}
```

Then find the closing of the `partners` array:

```ts
    { name: 'Blazer Events', file: 'blazer-events.jpeg' }
  ];
}
```

Replace with:

```ts
    { name: 'Blazer Events', file: 'blazer-events.jpeg' }
  ];

  readonly features: Feature[] = [
    { icon: 'Ticket', title: 'Ticketing & QR Check-in', description: 'Sell tickets and check attendees in with a single scan — no paper, no queues.' },
    { icon: 'Building2', title: 'Multi-Company Workspaces', description: 'Run events for multiple organizations from one account, each with its own team.' },
    { icon: 'Bell', title: 'Real-Time Notifications', description: 'Attendees and organizers stay in the loop with instant updates, powered by SignalR.' },
    { icon: 'DollarSign', title: 'Local Payment Methods', description: 'Accept Airtel Money, MTN MoMo, and card payments — priced in Zambian Kwacha.' },
    { icon: 'BookMarked', title: 'Venue Booking', description: 'Browse and book venues directly on the platform, from conference halls to open grounds.' },
    { icon: 'BarChart2', title: 'Analytics Dashboard', description: 'Track registrations, revenue, and attendance trends as they happen.' }
  ];

  readonly steps: Step[] = [
    { number: '01', title: 'Create Your Event', description: 'Set the details, pick a venue, and choose your ticket price in minutes.' },
    { number: '02', title: 'Publish & Sell Tickets', description: 'Share your event and start collecting registrations and payments right away.' },
    { number: '03', title: 'Check Attendees In', description: 'Scan QR codes at the door for instant, fraud-proof check-in.' },
    { number: '04', title: 'Track Performance', description: 'Watch registrations and revenue roll in on your live analytics dashboard.' }
  ];
}
```

- [ ] **Step 2: Add the Features and How It Works markup**

In `SmartEvents.UI/src/app/features/landing/landing.component.html`, find:

```html
  <!-- PARTNERS END -->

  <!-- SECTIONS GO HERE -->
</div>
```

Replace with:

```html
  <!-- PARTNERS END -->

  <!-- FEATURES -->
  <section class="features">
    <div class="features__inner">
      <h2 class="features__title">Everything you need to run an event</h2>
      <p class="features__subtitle">From ticket sales to the door, SmartEvents covers the whole lifecycle.</p>
      <div class="features__grid">
        @for (feature of features; track feature.title) {
          <div class="feature-card">
            <div class="feature-card__icon">
              <lucide-icon [name]="feature.icon" [size]="22" />
            </div>
            <h3 class="feature-card__title">{{ feature.title }}</h3>
            <p class="feature-card__description">{{ feature.description }}</p>
          </div>
        }
      </div>
    </div>
  </section>
  <!-- FEATURES END -->

  <!-- HOW IT WORKS -->
  <section class="how-it-works">
    <div class="how-it-works__inner">
      <h2 class="how-it-works__title">How it works</h2>
      <p class="how-it-works__subtitle">From first draft to final headcount, in four steps.</p>
      <div class="how-it-works__steps">
        @for (step of steps; track step.number) {
          <div class="step">
            <span class="step__number">{{ step.number }}</span>
            <h3 class="step__title">{{ step.title }}</h3>
            <p class="step__description">{{ step.description }}</p>
          </div>
        }
      </div>
    </div>
  </section>
  <!-- HOW IT WORKS END -->

  <!-- SECTIONS GO HERE -->
</div>
```

- [ ] **Step 3: Add Features and How It Works styles**

In `SmartEvents.UI/src/app/features/landing/landing.component.scss`, add this block at the end of the file:

```scss

/* FEATURES */
.features {
  padding: 6rem 1.5rem;

  &__inner {
    max-width: 1100px;
    margin: 0 auto;
    text-align: center;
  }

  &__title {
    font-size: 2rem;
    margin-bottom: .75rem;
  }

  &__subtitle {
    color: var(--text-muted);
    font-size: 1.05rem;
    margin-bottom: 3rem;
  }

  &__grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 1.5rem;
    text-align: left;

    @media (max-width: 900px) {
      grid-template-columns: repeat(2, 1fr);
    }

    @media (max-width: 600px) {
      grid-template-columns: 1fr;
    }
  }
}

.feature-card {
  padding: 1.75rem;
  background: var(--white);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  transition: box-shadow var(--transition), transform var(--transition);

  &:hover {
    box-shadow: var(--shadow);
    transform: translateY(-2px);
  }

  &__icon {
    width: 44px;
    height: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: var(--radius);
    background: var(--primary-muted);
    color: var(--primary);
    margin-bottom: 1rem;
  }

  &__title {
    margin-bottom: .5rem;
  }

  &__description {
    color: var(--text-muted);
    font-size: .9rem;
  }
}

/* HOW IT WORKS */
.how-it-works {
  padding: 6rem 1.5rem;
  background: var(--bg-alt);

  &__inner {
    max-width: 1100px;
    margin: 0 auto;
    text-align: center;
  }

  &__title {
    font-size: 2rem;
    margin-bottom: .75rem;
  }

  &__subtitle {
    color: var(--text-muted);
    font-size: 1.05rem;
    margin-bottom: 3rem;
  }

  &__steps {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 1.5rem;
    text-align: left;

    @media (max-width: 900px) {
      grid-template-columns: repeat(2, 1fr);
    }

    @media (max-width: 560px) {
      grid-template-columns: 1fr;
    }
  }
}

.step {
  padding: 1.5rem;
  background: var(--white);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-light);

  &__number {
    display: inline-block;
    font-size: .85rem;
    font-weight: 700;
    color: var(--primary);
    background: var(--primary-muted);
    padding: .2rem .6rem;
    border-radius: 999px;
    margin-bottom: .9rem;
  }

  &__title {
    margin-bottom: .4rem;
  }

  &__description {
    color: var(--text-muted);
    font-size: .9rem;
  }
}
```

- [ ] **Step 4: Verify the build compiles**

Run: `cd SmartEvents.UI && npm run build`
Expected: build succeeds with no errors.

- [ ] **Step 5: Verify in the browser**

Run: `npm start`, open `http://localhost:4200/` logged out.
Expected: below the partners marquee, a 3-column features grid (6 cards, each with an icon, title, description) and below that a 4-column "How it works" section. Resize the browser to ~500px wide: features grid collapses to 1 column, steps collapse to 1 column, no horizontal scrollbar.

- [ ] **Step 6: Commit**

```bash
git add SmartEvents.UI/src/app/features/landing
git commit -m "feat: add features grid and how-it-works section to landing page"
```

---

### Task 6: Final CTA + Footer, and full-page verification

**Files:**
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.html`
- Modify: `SmartEvents.UI/src/app/features/landing/landing.component.scss`

**Interfaces:**
- Consumes: the `<!-- HOW IT WORKS END -->` marker from Task 5.
- Produces: the completed `LandingComponent` template (closes the `.landing` wrapper div). No further tasks depend on this one.

- [ ] **Step 1: Add the Final CTA and Footer markup**

In `SmartEvents.UI/src/app/features/landing/landing.component.html`, find:

```html
  <!-- HOW IT WORKS END -->

  <!-- SECTIONS GO HERE -->
</div>
```

Replace with:

```html
  <!-- HOW IT WORKS END -->

  <!-- FINAL CTA -->
  <section class="final-cta">
    <div class="final-cta__inner">
      <h2 class="final-cta__title">Ready to run your next event?</h2>
      <p class="final-cta__subtitle">Join organizers across Zambia already using SmartEvents.</p>
      <a routerLink="/auth/register" class="btn-primary final-cta__button">
        Get Started Free
        <lucide-icon name="ArrowRight" [size]="18" />
      </a>
    </div>
  </section>
  <!-- FINAL CTA END -->

  <!-- FOOTER -->
  <footer class="footer">
    <div class="footer__inner">
      <div class="footer__brand">
        <lucide-icon name="Sparkles" [size]="18" />
        <span>SmartEvents</span>
      </div>
      <nav class="footer__links">
        <a routerLink="/events">Browse Events</a>
        <a routerLink="/auth/login">Sign In</a>
        <a routerLink="/auth/register">Get Started</a>
      </nav>
      <p class="footer__copyright">&copy; 2026 SmartEvents. Built for Zambia.</p>
    </div>
  </footer>
  <!-- FOOTER END -->
</div>
```

- [ ] **Step 2: Add Final CTA and Footer styles**

In `SmartEvents.UI/src/app/features/landing/landing.component.scss`, add this block at the end of the file:

```scss

/* FINAL CTA */
.final-cta {
  padding: 5rem 1.5rem;
  text-align: center;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  color: var(--white);

  &__title {
    font-size: 2rem;
    color: var(--white);
    margin-bottom: .6rem;
  }

  &__subtitle {
    color: var(--primary-light);
    margin-bottom: 2rem;
    font-size: 1.05rem;
  }

  &__button {
    background: var(--white);
    color: var(--primary-dark);

    &:hover {
      background: var(--primary-light);
      color: var(--primary-dark);
    }
  }
}

/* FOOTER */
.footer {
  padding: 2.5rem 1.5rem;
  background: var(--text);

  &__inner {
    max-width: 1100px;
    margin: 0 auto;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 1.5rem;
  }

  &__brand {
    display: flex;
    align-items: center;
    gap: .5rem;
    color: var(--white);
    font-weight: 700;

    lucide-icon { color: var(--primary-light); }
  }

  &__links {
    display: flex;
    gap: 1.5rem;

    a {
      color: var(--border-light);
      font-size: .9rem;

      &:hover { color: var(--white); }
    }
  }

  &__copyright {
    color: var(--text-light);
    font-size: .8rem;
    width: 100%;
    text-align: center;
    margin-top: .5rem;
  }
}
```

- [ ] **Step 3: Verify the production build compiles**

Run: `cd SmartEvents.UI && npm run build`
Expected: build succeeds with no errors or warnings about unclosed tags (confirms the `.landing` div closes correctly).

- [ ] **Step 4: Full-page browser verification**

Run: `npm start`, open `http://localhost:4200/` logged out. Walk through the entire page top to bottom:
- Nav sticks to the top on scroll.
- Hero renders with both CTAs working.
- Partners marquee scrolls smoothly and loops.
- Features grid shows all 6 cards.
- How it works shows all 4 steps in order (01–04).
- Final CTA banner renders with contrasting background and a working "Get Started Free" button.
- Footer shows brand, 3 links (all navigate correctly), and the copyright line.

Then resize to a mobile width (~375px) and re-check: no horizontal scrollbar anywhere on the page, nav actions remain usable, all sections stack to single/double columns as designed.

Then log in with a seeded account (e.g. `attendee@example.com` / `Seed1234!`) and visit `http://localhost:4200/`.
Expected: immediately redirected to `/dashboard` (landing page is never shown to authenticated users).

- [ ] **Step 5: Commit**

```bash
git add SmartEvents.UI/src/app/features/landing
git commit -m "feat: add final CTA and footer to landing page"
```

---

## Post-Implementation

Update the Change Log table in `/Users/zimbadev/Documents/Workspace/Peoples Projects/SmartEvents/CLAUDE.md` with a new row documenting this change (new public landing page at `/`, root route behavior change, partner logo assets added under `public/images/partners/`), per the file's own instruction: "Update this file whenever a meaningful change is made to the application."
