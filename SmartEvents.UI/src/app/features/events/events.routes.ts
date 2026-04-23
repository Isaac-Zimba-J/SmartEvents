import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const EVENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./events-list/events-list.component').then(m => m.EventsListComponent)
  },
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () => import('./create-event/create-event.component').then(m => m.CreateEventComponent)
  },
  {
    path: ':slug/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./edit-event/edit-event.component').then(m => m.EditEventComponent)
  },
  {
    path: ':slug',
    loadComponent: () => import('./event-detail/event-detail.component').then(m => m.EventDetailComponent)
  }
];
