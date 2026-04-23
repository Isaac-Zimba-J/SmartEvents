import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CompaniesService } from '../../../core/services/companies.service';

@Component({
  selector: 'app-create-company',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-company.component.html'
})
export class CreateCompanyComponent {
  form: FormGroup;
  loading = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private companiesService: CompaniesService,
    private router: Router
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      website: [''],
      phone: [''],
      email: ['', Validators.email],
      address: [''],
      logoUrl: ['']
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const value = this.form.value;
    const request = {
      name: value.name,
      description: value.description || undefined,
      website: value.website || undefined,
      phone: value.phone || undefined,
      email: value.email || undefined,
      address: value.address || undefined,
      logoUrl: value.logoUrl || undefined
    };

    this.companiesService.create(request).subscribe({
      next: () => this.router.navigate(['/companies']),
      error: err => {
        this.error = err.error?.message ?? 'Failed to create company.';
        this.loading = false;
      }
    });
  }
}
