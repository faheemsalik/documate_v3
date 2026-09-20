import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import {
  AdminApiService,
  type AdminAgentListItem,
  type AdminBusinessListItem,
  type AdminTenantListItem,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-agent-list-page',
  imports: [FormsModule, Button, InputText, Message, Select, Tag, RouterLink],
  templateUrl: './agent-list.page.html',
  styleUrl: './agent-list.page.scss',
})
export class AgentListPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<AdminAgentListItem[]>([]);
  readonly total = signal(0);
  readonly tenants = signal<AdminTenantListItem[]>([]);
  readonly businesses = signal<AdminBusinessListItem[]>([]);

  name = '';
  tenantId: string | null = null;
  businessId: string | null = null;

  tenantOptions(): { label: string; value: string }[] {
    return this.tenants().map((t) => ({ label: t.name, value: t.id }));
  }

  businessOptions(): { label: string; value: string }[] {
    const tenantId = this.tenantId;
    const all = this.businesses();
    const filtered = tenantId ? all.filter((b) => b.tenantId === tenantId) : all;
    return filtered.map((b) => ({ label: `${b.name} (${b.tenantName})`, value: b.idenBusinessId }));
  }

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.name = q.get('name') ?? '';
    this.tenantId = q.get('tenantId');
    this.businessId = q.get('businessId');
    this.reload();
    this.api.listTenants({ pageSize: 200 }).subscribe({
      next: (res) => this.tenants.set(res.items),
    });
    this.api.listBusinesses({ pageSize: 200 }).subscribe({
      next: (res) => this.businesses.set(res.items),
    });
  }

  fmt(iso: string): string {
    return iso.slice(0, 19).replace('T', ' ');
  }

  onTenantChange(): void {
    if (!this.tenantId || !this.businessId) return;
    const match = this.businesses().find((b) => b.idenBusinessId === this.businessId);
    if (match && match.tenantId !== this.tenantId) {
      this.businessId = null;
    }
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        name: this.name || null,
        tenantId: this.tenantId || null,
        businessId: this.businessId || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.api
      .listAgents({
        name: this.name || undefined,
        tenantId: this.tenantId || undefined,
        businessId: this.businessId || undefined,
        pageSize: 100,
      })
      .subscribe({
        next: (res) => {
          this.rows.set(res.items);
          this.total.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load agents.');
          this.loading.set(false);
        },
      });
  }
}
