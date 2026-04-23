import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const ORGANIZER_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./organizer-dashboard/organizer-dashboard.component').then(m => m.OrganizerDashboardComponent)
  },
  {
    path: 'events/:eventId/attendees',
    canActivate: [authGuard],
    loadComponent: () => import('./attendees/attendees.component').then(m => m.AttendeesComponent)
  }
];
