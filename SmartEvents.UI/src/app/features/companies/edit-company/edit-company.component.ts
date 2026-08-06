import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CompaniesService } from '../../../core/services/companies.service';
import { Company } from '../../../core/models/company.models';

@Component({
  selector: 'app-edit-company',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './edit-company.component.html'
})
export class EditCompanyComponent implements OnInit {
  form: FormGroup;
  loading = true;
  saving = false;
  error = '';
  company: Company | null = null;
  logoPreview: string | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
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

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.companiesService.getById(id).subscribe({
      next: company => {
        this.company = company;
        this.form.patchValue({
          name: company.name,
          description: company.description ?? '',
          website: company.website ?? '',
          phone: company.phone ?? '',
          email: company.email ?? '',
          address: company.address ?? '',
          logoUrl: company.logoUrl ?? ''
        });
        this.logoPreview = company.logoUrl ?? null;
        this.loading = false;
      },
      error: () => {
        this.error = 'Company not found.';
        this.loading = false;
      }
    });
  }

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.error = 'Please select an image file.';
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.error = 'Image must be under 2 MB.';
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      this.logoPreview = dataUrl;
      this.form.patchValue({ logoUrl: dataUrl });
    };
    reader.readAsDataURL(file);
  }

  removeLogo(): void {
    this.logoPreview = null;
    this.form.patchValue({ logoUrl: '' });
  }

  submit(): void {
    if (this.form.invalid || !this.company) return;
    this.saving = true;
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

    this.companiesService.update(this.company.id, request).subscribe({
      next: () => this.router.navigate(['/companies']),
      error: err => {
        this.error = err.error?.message ?? 'Failed to update company.';
        this.saving = false;
      }
    });
  }
}
