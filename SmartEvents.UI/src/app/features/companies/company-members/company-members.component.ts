import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CompanyMembersService, CompanyMember } from '../../../core/services/company-members.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-company-members',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LucideAngularModule],
  templateUrl: './company-members.component.html',
  styleUrls: ['./company-members.component.scss']
})
export class CompanyMembersComponent implements OnInit {
  companyId: string;

  members: CompanyMember[] = [];
  loading = true;
  error = '';

  addEmail = '';
  addRole = 'Attendee';
  addLoading = false;
  addError = '';

  availableRoles = ['CompanyAdmin', 'Organizer', 'Attendee'];

  constructor(
    private route: ActivatedRoute,
    private membersService: CompanyMembersService,
    private toast: ToastService
  ) {
    this.companyId = this.route.snapshot.params['id'];
  }

  ngOnInit(): void {
    this.loadMembers();
  }

  loadMembers(): void {
    this.loading = true;
    this.error = '';
    this.membersService.getMembers(this.companyId).subscribe({
      next: members => {
        this.members = members;
        this.loading = false;
      },
      error: err => {
        this.error = err.error?.message ?? 'Failed to load members.';
        this.loading = false;
      }
    });
  }

  addMember(): void {
    if (!this.addEmail) return;
    this.addLoading = true;
    this.addError = '';
    this.membersService.addMember(this.companyId, { email: this.addEmail, role: this.addRole }).subscribe({
      next: member => {
        this.members.push(member);
        this.addEmail = '';
        this.addRole = 'Attendee';
        this.addLoading = false;
        this.toast.success('Member added successfully.');
      },
      error: err => {
        this.addError = err.error?.message ?? 'Failed to add member.';
        this.addLoading = false;
      }
    });
  }

  updateRole(member: CompanyMember, role: string): void {
    this.membersService.updateRole(this.companyId, member.id, { role }).subscribe({
      next: updated => {
        member.role = updated.role;
        this.toast.success('Role updated.');
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to update role.');
      }
    });
  }

  removeMember(member: CompanyMember): void {
    this.membersService.removeMember(this.companyId, member.id).subscribe({
      next: () => {
        this.members = this.members.filter(m => m.id !== member.id);
        this.toast.success('Member removed.');
      },
      error: err => {
        this.toast.error(err.error?.message ?? 'Failed to remove member.');
      }
    });
  }
}
