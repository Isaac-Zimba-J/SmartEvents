import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UsersService } from '../../../core/services/users.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-email.component.html'
})
export class VerifyEmailComponent implements OnInit {
  loading = true;
  success = false;
  message = '';

  constructor(
    private route: ActivatedRoute,
    private usersService: UsersService
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.message = 'No verification token provided.';
      this.loading = false;
      return;
    }

    this.usersService.verifyEmail(token).subscribe({
      next: (res: any) => {
        this.success = true;
        this.message = res?.message ?? 'Email verified successfully!';
        this.loading = false;
      },
      error: err => {
        this.success = false;
        this.message = err.error?.message ?? 'Verification failed. The link may have expired.';
        this.loading = false;
      }
    });
  }
}
