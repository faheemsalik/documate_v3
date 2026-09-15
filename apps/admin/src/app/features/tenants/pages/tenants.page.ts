import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminTenantListItem } from '../../../core/admin-api.service';

@Component({
  selector: 'app-tenants-page',
  imports: [FormsModule, Button, InputText, Dialog, Message, RouterLink],
  templateUrl: './tenants.page.html',
  styleUrl: './tenants.page.scss',
})
export class TenantsPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<AdminTenantListItem[]>([]);
  readonly total = signal(0);

  search = '';
  showCreate = false;
  createName = '';
  createIden = '';
  createBusiness = '';
  createError = '';
  creating = false;

  ngOnInit(): void {
    this.reload();
  }

  fmt(iso: string): string {
    return iso.slice(0, 19).replace('T', ' ');
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listTenants({ search: this.search || undefined, pageSize: 100 }).subscribe({
      next: (res) => {
        this.rows.set(res.items);
        this.total.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load tenants.');
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.createName = '';
    this.createIden = '';
    this.createBusiness = '';
    this.createError = '';
    this.showCreate = true;
  }

  submitCreate(): void {
    if (!this.createName.trim()) {
      this.createError = 'Name is required.';
      return;
    }
    this.creating = true;
    this.createError = '';
    this.api
      .createTenant({
        name: this.createName.trim(),
        idenTenantId: this.createIden.trim() || null,
        initialBusinessName: this.createBusiness.trim() || null,
      })
      .subscribe({
        next: (t) => {
          this.creating = false;
          this.showCreate = false;
          void this.router.navigate(['/tenants', t.id]);
        },
        error: (err) => {
          this.creating = false;
          this.createError = err?.error?.error ?? 'Create failed.';
        },
      });
  }
}
