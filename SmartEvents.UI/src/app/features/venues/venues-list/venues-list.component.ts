import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { VenuesService } from '../../../core/services/venues.service';
import { AuthService } from '../../../core/services/auth.service';
import { Venue, VenueType } from '../../../core/models/venue.models';
import { SkeletonComponent } from '../../../core/components/skeleton/skeleton.component';

@Component({
  selector: 'app-venues-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, SkeletonComponent],
  templateUrl: './venues-list.component.html'
})
export class VenuesListComponent implements OnInit {
  venues: Venue[] = [];
  loading = true;
  city = '';
  country = '';
  selectedType: VenueType | '' = '';

  readonly types: VenueType[] = ['Indoor', 'Outdoor', 'Virtual', 'Hybrid'];

  constructor(
    private venuesService: VenuesService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.venuesService.getAll({
      city: this.city || undefined,
      country: this.country || undefined,
      type: this.selectedType || undefined
    }).subscribe({
      next: data => { this.venues = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  get isOrganizer(): boolean {
    const role = this.auth.currentUser()?.role;
    return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'Organizer';
  }
}
