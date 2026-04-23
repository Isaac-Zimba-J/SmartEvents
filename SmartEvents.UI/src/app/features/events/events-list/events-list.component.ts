import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EventsService } from '../../../core/services/events.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventCategory, EventSummary, PagedResult } from '../../../core/models/event.models';
import { SkeletonComponent } from '../../../core/components/skeleton/skeleton.component';

@Component({
  selector: 'app-events-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, SkeletonComponent],
  templateUrl: './events-list.component.html'
})
export class EventsListComponent implements OnInit {
  result: PagedResult<EventSummary> | null = null;
  loading = true;
  search = '';
  selectedCategory: EventCategory | '' = '';
  currentPage = 1;

  readonly categories: EventCategory[] = [
    'Conference', 'Workshop', 'Concert', 'Exhibition',
    'Sports', 'Networking', 'Webinar', 'Other'
  ];

  constructor(
    private eventsService: EventsService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.eventsService.getAll({
      page: this.currentPage,
      pageSize: 12,
      search: this.search || undefined,
      category: this.selectedCategory || undefined
    }).subscribe({
      next: data => { this.result = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.load();
  }

  onCategoryChange(): void {
    this.currentPage = 1;
    this.load();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.load();
  }

  get pages(): number[] {
    if (!this.result) return [];
    return Array.from({ length: this.result.totalPages }, (_, i) => i + 1);
  }

  get isOrganizer(): boolean {
    const role = this.auth.currentUser()?.role;
    return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'Organizer';
  }
}
