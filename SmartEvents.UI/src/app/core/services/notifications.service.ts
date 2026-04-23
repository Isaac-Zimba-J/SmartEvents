import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

export interface SignalRNotification {
  type: string;
  title: string;
  message: string;
  data?: Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService implements OnDestroy {
  private connection: signalR.HubConnection | null = null;

  constructor(private authService: AuthService, private toast: ToastService) {}

  connect(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', (notification: SignalRNotification) => {
      this.handleNotification(notification);
    });

    this.connection
      .start()
      .catch(err => console.error('SignalR connection error:', err));
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  private handleNotification(notification: SignalRNotification): void {
    const text = notification.title ? `${notification.title}: ${notification.message}` : notification.message;
    switch (notification.type) {
      case 'RegistrationConfirmed':
        this.toast.success(text);
        break;
      case 'EventCancelled':
        this.toast.error(text);
        break;
      case 'EventUpdated':
      case 'EventReminder':
        this.toast.info(text);
        break;
      default:
        this.toast.info(text);
    }
  }
}
