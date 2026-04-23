import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const VENUES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./venues-list/venues-list.component').then(m => m.VenuesListComponent)
  },
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () => import('./create-venue/create-venue.component').then(m => m.CreateVenueComponent)
  },
  {
    path: ':id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./edit-venue/edit-venue.component').then(m => m.EditVenueComponent)
  }
];
