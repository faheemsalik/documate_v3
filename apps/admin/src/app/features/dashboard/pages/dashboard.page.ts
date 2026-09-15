import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Message } from 'primeng/message';
import {
  AdminApiService,
  type AdminAnalyticsSummary,
  type AdminByBusinessRow,
  type AdminHourlyPoint,
  type AdminStageTimings,
  type AdminVolumePoint,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [FormsModule, Button, InputText, Select, Message],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly summary = signal<AdminAnalyticsSummary | null>(null);
  readonly volume = signal<AdminVolumePoint[]>([]);
  readonly hourly = signal<AdminHourlyPoint[]>([]);
  readonly byBusiness = signal<AdminByBusinessRow[]>([]);
  readonly stages = signal<AdminStageTimings | null>(null);

  granularity = 'day';
  month = new Date().toISOString().slice(0, 7);
  date = '';
  businessId = '';

  readonly granOptions = [
    { label: 'Hour', value: 'hour' },
    { label: 'Day', value: 'day' },
    { label: 'Month', value: 'month' },
  ];

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    const businessIds = this.businessId ? [this.businessId] : undefined;
    const from = this.date
      ? `${this.date}T00:00:00.000Z`
      : `${this.month}-01T00:00:00.000Z`;

    this.api.analyticsSummary({ from, businessIds }).subscribe({
      next: (s) => this.summary.set(s),
      error: () => this.error.set('Failed to load summary.'),
    });

    this.api.analyticsVolume({ granularity: this.granularity, from, businessIds }).subscribe({
      next: (v) => this.volume.set(v),
      error: () => this.error.set('Failed to load volume.'),
    });

    const byBizQuery = this.date
      ? { date: this.date, businessIds }
      : { month: this.month, businessIds };
    this.api.analyticsByBusiness(byBizQuery).subscribe({
      next: (rows) => this.byBusiness.set(rows),
      error: () => this.error.set('Failed to load business KPIs.'),
    });

    if (this.date) {
      this.api.analyticsHourly({ date: this.date, businessIds }).subscribe({
        next: (h) => this.hourly.set(h),
        error: () => this.error.set('Failed to load hourly.'),
      });
    } else {
      this.hourly.set([]);
    }

    this.api.analyticsStageTimings({ from, businessIds }).subscribe({
      next: (s) => {
        this.stages.set(s);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load stage timings.');
        this.loading.set(false);
      },
    });
  }

  drillBusiness(row: AdminByBusinessRow): void {
    void this.router.navigate(['/ops'], {
      queryParams: {
        businessId: row.businessId,
        date: this.date || undefined,
      },
    });
  }

  maxVolume(): number {
    return Math.max(1, ...this.volume().map((v) => v.files + v.documents));
  }

  maxHourly(): number {
    return Math.max(1, ...this.hourly().map((h) => h.files + h.documents));
  }

  maxStage(items: { avgDurationMs: number }[]): number {
    return Math.max(1, ...items.map((i) => i.avgDurationMs));
  }

  fmtMs(v: number | null | undefined): string {
    if (v == null) return '—';
    if (v < 1000) return `${Math.round(v)} ms`;
    return `${(v / 1000).toFixed(1)} s`;
  }

  bucketLabel(iso: string): string {
    return iso.replace('T', ' ').slice(0, 16);
  }

  padHour(h: number): string {
    return h.toString().padStart(2, '0');
  }
}
