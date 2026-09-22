import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { UsersApiService, type AppUser } from '../../../core/api/users-api.service';

const ROLE_OPTIONS = [
  { label: 'Admin', value: 'documate.admin' },
  { label: 'Editor', value: 'documate.editor' },
  { label: 'Files operator', value: 'documate.files_operator' },
  { label: 'Viewer', value: 'documate.viewer' },
];

@Component({
  selector: 'app-users-page',
  imports: [FormsModule, Button, Dialog, InputText, Message, Select, TableModule, RouterLink],
  templateUrl: './users.page.html',
  styleUrl: './users.page.scss',
})
export class UsersPage implements OnInit {
  private readonly api = inject(UsersApiService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly users = signal<AppUser[]>([]);
  readonly roleOptions = ROLE_OPTIONS;

  showInvite = false;
  inviteEmail = '';
  inviteName = '';
  inviteRole = 'documate.viewer';
  inviteError = '';
  inviting = false;

  ngOnInit(): void {
    this.reload();
  }

  roleLabel(key?: string | null): string {
    return ROLE_OPTIONS.find((r) => r.value === key)?.label ?? key ?? '—';
  }

  openInvite(): void {
    this.inviteEmail = '';
    this.inviteName = '';
    this.inviteRole = 'documate.viewer';
    this.inviteError = '';
    this.showInvite = true;
  }

  submitInvite(): void {
    if (!this.inviteEmail.trim()) {
      this.inviteError = 'Email is required.';
      return;
    }
    this.inviting = true;
    this.inviteError = '';
    this.api
      .invite({
        email: this.inviteEmail.trim(),
        displayName: this.inviteName.trim() || undefined,
        roleSysKey: this.inviteRole,
      })
      .subscribe({
        next: (u) => {
          this.inviting = false;
          this.showInvite = false;
          void this.router.navigate(['/users', u.memberId]);
        },
        error: (err) => {
          this.inviting = false;
          this.inviteError = err?.error?.error ?? 'Invite failed.';
        },
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.list().subscribe({
      next: (items) => {
        this.users.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load users. Iden may be unconfigured.');
      },
    });
  }
}
