import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CompaniesService } from '../../../core/services/companies.service';
import { AuthService } from '../../../core/services/auth.service';
import { Company } from '../../../core/models/company.models';

@Component({
  selector: 'app-companies-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './companies-list.component.html'
})
export class CompaniesListComponent implements OnInit {
  companies: Company[] = [];
  loading = true;
  error = '';
  deleting: string | null = null;

  constructor(
    private companiesService: CompaniesService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.companiesService.getAll().subscribe({
      next: data => { this.companies = data; this.loading = false; },
      error: () => { this.error = 'Failed to load companies.'; this.loading = false; }
    });
  }

  delete(id: string): void {
    if (!confirm('Deactivate this company?')) return;
    this.deleting = id;
    this.companiesService.delete(id).subscribe({
      next: () => {
        this.companies = this.companies.filter(c => c.id !== id);
        this.deleting = null;
      },
      error: err => {
        this.error = err.error?.message ?? 'Failed to deactivate company.';
        this.deleting = null;
      }
    });
  }

  get isSuperAdmin(): boolean {
    return this.auth.currentUser()?.role === 'SuperAdmin';
  }
}
