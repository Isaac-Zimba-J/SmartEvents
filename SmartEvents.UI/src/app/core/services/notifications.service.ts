import { Injectable, OnDestroy, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

export interface NotificationRecord {
  id: string;
  type: 'Email' | 'SMS' | 'Push';
  event: string;
  subject: string;
  body: string;
  isSent: boolean;
  isRead: boolean;
  createdAt: string;
  sentAt?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService implements OnDestroy {
  private connection: signalR.HubConnection | null = null;
  private readonly apiUrl = `${environment.apiUrl}/notifications`;

  readonly unreadCount = signal<number>(0);

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private toast: ToastService
  ) {}

  getHistory() {
    return this.http.get<NotificationRecord[]>(this.apiUrl);
  }

  fetchUnreadCount() {
    this.http.get<{ count: number }>(`${this.apiUrl}/count`).subscribe({
      next: r => this.unreadCount.set(r.count),
      error: () => {}
    });
  }

  markAllRead() {
    return this.http.post<void>(`${this.apiUrl}/mark-read`, {});
  }

  connect(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Backend sends event name "notification" (not "ReceiveNotification")
    this.connection.on('notification', (payload: { type: string; data: Record<string, unknown>; timestamp: string }) => {
      const message = String(payload.data?.['message'] ?? '');
      switch (payload.type) {
        case 'registration_confirmed':
          this.toast.success(message || 'Registration confirmed!');
          break;
        case 'event_cancelled':
          this.toast.error(message || 'Event cancelled.');
          break;
        case 'waitlisted':
          this.toast.info(message || 'Added to waitlist.');
          break;
        default:
          this.toast.info(message);
      }
      this.unreadCount.update(n => n + 1);
    });

    this.connection
      .start()
      .then(() => this.fetchUnreadCount())
      .catch(err => console.error('SignalR connection error:', err));
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
  }

  ngOnDestroy(): void {
    this.disconnect();
  }
}
