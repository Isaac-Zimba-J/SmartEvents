import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VenuesService } from '../../../core/services/venues.service';
import { Venue, VenueType } from '../../../core/models/venue.models';

@Component({
  selector: 'app-edit-venue',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './edit-venue.component.html'
})
export class EditVenueComponent implements OnInit {
  form: FormGroup;
  loading = true;
  saving = false;
  error = '';
  venue: Venue | null = null;

  readonly types: VenueType[] = ['Indoor', 'Outdoor', 'Virtual', 'Hybrid'];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
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
      pricePerDay: [null, Validators.min(0)],
      isAvailable: [true]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.venuesService.getById(id).subscribe({
      next: venue => {
        this.venue = venue;
        this.form.patchValue({
          name: venue.name,
          description: venue.description ?? '',
          address: venue.address,
          city: venue.city,
          country: venue.country,
          latitude: venue.latitude ?? null,
          longitude: venue.longitude ?? null,
          capacity: venue.capacity,
          type: venue.type,
          imageUrl: venue.imageUrl ?? '',
          amenities: venue.amenities ?? '',
          pricePerDay: venue.pricePerDay ?? null,
          isAvailable: venue.isAvailable
        });
        this.loading = false;
      },
      error: () => {
        this.error = 'Venue not found.';
        this.loading = false;
      }
    });
  }

  submit(): void {
    if (this.form.invalid || !this.venue) return;
    this.saving = true;
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

    this.venuesService.update(this.venue.id, request).subscribe({
      next: () => this.router.navigate(['/venues']),
      error: err => {
        this.error = err.error?.message ?? 'Failed to update venue.';
        this.saving = false;
      }
    });
  }
}
