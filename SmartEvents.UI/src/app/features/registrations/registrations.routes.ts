import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const REGISTRATIONS_ROUTES: Routes = [
  {
    path: 'my',
    canActivate: [authGuard],
    loadComponent: () => import('./my-registrations/my-registrations.component').then(m => m.MyRegistrationsComponent)
  },
  {
    path: 'checkin',
    canActivate: [authGuard],
    loadComponent: () => import('./check-in/check-in.component').then(m => m.CheckInComponent)
  }
];
