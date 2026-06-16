import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const COMPANIES_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./companies-list/companies-list.component').then(m => m.CompaniesListComponent)
  },
  {
    path: 'create',
    canActivate: [authGuard],
    loadComponent: () => import('./create-company/create-company.component').then(m => m.CreateCompanyComponent)
  },
  {
    path: ':id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./edit-company/edit-company.component').then(m => m.EditCompanyComponent)
  },
  {
    path: ':id/members',
    canActivate: [authGuard],
    loadComponent: () => import('./company-members/company-members.component').then(m => m.CompanyMembersComponent)
  }
];
