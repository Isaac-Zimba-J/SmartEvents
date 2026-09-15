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
        // Mark all as read and reset badge
        this.notificationsService.markAllRead().subscribe({
          next: () => this.notificationsService.unreadCount.set(0),
          error: () => {}
        });
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

  // Email bodies are stored as the full HTML template; show readable text instead.
  preview(n: NotificationRecord): string {
    const text = n.body
      .replace(/<style[\s\S]*?<\/style>/gi, ' ')
      .replace(/<br\s*\/?>|<\/p>|<\/div>|<\/h[1-6]>|<\/li>/gi, ' ')
      .replace(/<[^>]+>/g, '')
      .replace(/&nbsp;/g, ' ')
      .replace(/&amp;/g, '&')
      .replace(/\s+/g, ' ')
      .trim();
    return text.length > 220 ? text.slice(0, 220).trimEnd() + '…' : text;
  }

  eventLabel(event: string): string {
    return event.replace(/([A-Z])/g, ' $1').trim();
  }

  iconFor(type: string): string {
    if (type === 'Email') return 'mail';
    if (type === 'SMS') return 'phone';
    return 'bell';
  }
}
