import { Component, OnInit, effect } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './core/components/toast/toast.component';
import { AuthService } from './core/services/auth.service';
import { NotificationsService } from './core/services/notifications.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'SmartEvents';

  constructor(
    private authService: AuthService,
    private notificationsService: NotificationsService
  ) {
    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.notificationsService.connect();
      } else {
        this.notificationsService.disconnect();
      }
    });
  }

  ngOnInit(): void {}
}
