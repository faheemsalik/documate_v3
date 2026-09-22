import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { RadioButton } from 'primeng/radiobutton';
import { UsersApiService, type AppUser } from '../../../core/api/users-api.service';

const ROLE_OPTIONS = [
  { label: 'Admin', value: 'documate.admin' },
  { label: 'Editor', value: 'documate.editor' },
  { label: 'Files operator', value: 'documate.files_operator' },
  { label: 'Viewer', value: 'documate.viewer' },
];

/** Informative FeatureKey summary by role (mirrors catalog role templates). */
const ROLE_PERMISSIONS: Record<string, { allow: string[]; deny: string[] }> = {
  'documate.admin': {
    allow: ['Agents full', 'Files full', 'Users manage', 'API keys manage'],
    deny: [],
  },
  'documate.editor': {
    allow: ['Agents list/view/create/update', 'Files list/view/upload/update'],
    deny: ['Agents delete', 'Files delete', 'Users manage'],
  },
  'documate.files_operator': {
    allow: ['Files list/view/upload/update', 'Documents view/edit'],
    deny: ['Agents create', 'Users manage', 'API keys manage'],
  },
  'documate.viewer': {
    allow: ['Agents list/view', 'Files list/view', 'Documents view'],
    deny: ['Agents create/update/delete', 'Files upload/delete', 'Users manage'],
  },
};

@Component({
  selector: 'app-user-detail-page',
  imports: [FormsModule, Button, Message, RadioButton, RouterLink],
  templateUrl: './user-detail.page.html',
  styleUrl: './user-detail.page.scss',
})
export class UserDetailPage implements OnInit {
  private readonly api = inject(UsersApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly user = signal<AppUser | null>(null);
  readonly roleOptions = ROLE_OPTIONS;
  readonly saving = signal(false);
  selectedRole = 'documate.viewer';

  ngOnInit(): void {
    const memberId = this.route.snapshot.paramMap.get('memberId');
    if (!memberId) {
      this.error.set('Missing user id.');
      this.loading.set(false);
      return;
    }
    this.api.list().subscribe({
      next: (items) => {
        const found = items.find((u) => u.memberId === memberId) ?? null;
        this.user.set(found);
        this.selectedRole = found?.roleSysKey || 'documate.viewer';
        this.loading.set(false);
        if (!found) this.error.set('User not found in this business.');
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load user.');
      },
    });
  }

  permissions() {
    return ROLE_PERMISSIONS[this.selectedRole] ?? { allow: [], deny: [] };
  }

  saveRole(): void {
    const u = this.user();
    if (!u) return;
    this.saving.set(true);
    this.api.assignRole(u.memberId, this.selectedRole).subscribe({
      next: () => {
        this.user.set({ ...u, roleSysKey: this.selectedRole });
        this.saving.set(false);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.error ?? 'Could not save role.');
      },
    });
  }

  deactivate(): void {
    const u = this.user();
    if (!u) return;
    this.api.deactivate(u.memberId).subscribe({
      next: () => this.user.set({ ...u, isActive: false }),
      error: (err) => this.error.set(err?.error?.error ?? 'Deactivate failed.'),
    });
  }
}
