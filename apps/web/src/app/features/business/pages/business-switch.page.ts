import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { BusinessApiService, type BusinessProfile } from '../../../core/api/business-api.service';
import { AppContextService } from '../../../core/app-context.service';

@Component({
  selector: 'app-business-switch-page',
  imports: [RouterLink, Card, Message],
  templateUrl: './business-switch.page.html',
  styleUrl: './business-switch.page.scss',
})
export class BusinessSwitchPage implements OnInit {
  private readonly businessApi = inject(BusinessApiService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly profile = signal<BusinessProfile | null>(null);

  ngOnInit(): void {
    this.businessApi.get().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load business.');
      },
    });
  }
}
