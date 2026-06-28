import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { NotificationsService, NotificationRecord } from '../../core/services/notifications.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  templateUrl: './notifications.component.html'
})
export class NotificationsComponent implements OnInit {
  notifications: NotificationRecord[] = [];
  loading = true;
  error = '';

  constructor(private notificationsService: NotificationsService) {}

  ngOnInit(): void {
    this.notificationsService.getHistory().subscribe({
      next: data => {
        this.notifications = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load notifications.';
        this.loading = false;
      }
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  eventLabel(event: string): string {
    return event.replace(/([A-Z])/g, ' $1').trim();
  }

  iconFor(type: string): string {
    if (type === 'Email') return 'Mail';
    if (type === 'SMS') return 'Phone';
    return 'Bell';
  }
}
