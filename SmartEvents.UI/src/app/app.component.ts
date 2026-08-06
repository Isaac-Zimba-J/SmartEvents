import { Component, effect } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ToastComponent } from './core/components/toast/toast.component';
import { NavbarComponent } from './core/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';
import { NotificationsService } from './core/services/notifications.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, ToastComponent, NavbarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'SmartEvents';

  constructor(
    public authService: AuthService,
    private notificationsService: NotificationsService
  ) {
    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.notificationsService.connect();
        this.notificationsService.fetchUnreadCount();
      } else {
        this.notificationsService.disconnect();
      }
    });
  }
}
