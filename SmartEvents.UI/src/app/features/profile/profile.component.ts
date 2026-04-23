import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UsersService } from '../../core/services/users.service';
import { ToastService } from '../../core/services/toast.service';
import { ProfileResponse } from '../../core/models/user.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  form: FormGroup;
  profile: ProfileResponse | null = null;
  loading = true;
  saving = false;
  requestingVerification = false;

  constructor(
    private fb: FormBuilder,
    private usersService: UsersService,
    private toast: ToastService
  ) {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      phone: [''],
      avatarUrl: ['']
    });
  }

  ngOnInit(): void {
    this.usersService.getProfile().subscribe({
      next: profile => {
        this.profile = profile;
        this.form.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName,
          phone: profile.phone ?? '',
          avatarUrl: profile.avatarUrl ?? ''
        });
        this.loading = false;
      },
      error: () => {
        this.toast.error('Failed to load profile.');
        this.loading = false;
      }
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const v = this.form.value;

    this.usersService.updateProfile({
      firstName: v.firstName,
      lastName: v.lastName,
      phone: v.phone || undefined,
      avatarUrl: v.avatarUrl || undefined
    }).subscribe({
      next: updated => {
        this.profile = updated;
        this.saving = false;
        this.toast.success('Profile updated successfully.');
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to update profile.');
        this.saving = false;
      }
    });
  }

  requestVerification(): void {
    this.requestingVerification = true;
    this.usersService.requestVerification().subscribe({
      next: () => {
        this.toast.success('Verification email sent. Check your inbox.');
        this.requestingVerification = false;
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to send verification email.');
        this.requestingVerification = false;
      }
    });
  }
}
