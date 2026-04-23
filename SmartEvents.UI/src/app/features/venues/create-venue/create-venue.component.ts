import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VenuesService } from '../../../core/services/venues.service';
import { VenueType } from '../../../core/models/venue.models';

@Component({
  selector: 'app-create-venue',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-venue.component.html'
})
export class CreateVenueComponent {
  form: FormGroup;
  loading = false;
  error = '';

  readonly types: VenueType[] = ['Indoor', 'Outdoor', 'Virtual', 'Hybrid'];

  constructor(
    private fb: FormBuilder,
    private venuesService: VenuesService,
    private router: Router
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      address: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
      latitude: [null],
      longitude: [null],
      capacity: [100, [Validators.required, Validators.min(1)]],
      type: ['Indoor', Validators.required],
      imageUrl: [''],
      amenities: [''],
      pricePerDay: [null, Validators.min(0)]
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const value = this.form.value;
    const request = {
      ...value,
      latitude: value.latitude || undefined,
      longitude: value.longitude || undefined,
      pricePerDay: value.pricePerDay || undefined,
      imageUrl: value.imageUrl || undefined,
      amenities: value.amenities || undefined,
      description: value.description || undefined
    };

    this.venuesService.create(request).subscribe({
      next: () => this.router.navigate(['/venues']),
      error: err => {
        this.error = err.error?.message ?? 'Failed to create venue.';
        this.loading = false;
      }
    });
  }
}
