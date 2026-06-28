import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'events',
    loadChildren: () => import('./features/events/events.routes').then(m => m.EVENTS_ROUTES)
  },
  {
    path: 'venues',
    canActivate: [authGuard],
    loadChildren: () => import('./features/venues/venues.routes').then(m => m.VENUES_ROUTES)
  },
  {
    path: 'registrations',
    canActivate: [authGuard],
    loadChildren: () => import('./features/registrations/registrations.routes').then(m => m.REGISTRATIONS_ROUTES)
  },
  {
    path: 'companies',
    canActivate: [authGuard],
    loadChildren: () => import('./features/companies/companies.routes').then(m => m.COMPANIES_ROUTES)
  },
  {
    path: 'organizer',
    canActivate: [authGuard],
    loadChildren: () => import('./features/organizer/organizer.routes').then(m => m.ORGANIZER_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'analytics',
    canActivate: [authGuard],
    loadChildren: () => import('./features/analytics/analytics.routes').then(m => m.ANALYTICS_ROUTES)
  },
  {
    path: 'notifications',
    canActivate: [authGuard],
    loadChildren: () => import('./features/notifications/notifications.routes').then(m => m.NOTIFICATIONS_ROUTES)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
