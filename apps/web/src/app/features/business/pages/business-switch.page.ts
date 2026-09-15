import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import {
  BusinessApiService,
  type BusinessListItem,
} from '../../../core/api/business-api.service';
import { AppContextService } from '../../../core/app-context.service';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-business-switch-page',
  imports: [RouterLink, FormsModule, Card, Button, InputText, Message, Tag],
  templateUrl: './business-switch.page.html',
  styleUrl: './business-switch.page.scss',
})
export class BusinessSwitchPage implements OnInit {
  private readonly businessApi = inject(BusinessApiService);
  private readonly auth = inject(AuthService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly switchingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly businesses = signal<BusinessListItem[]>([]);
  newBusinessName = '';

  ngOnInit(): void {
    this.reload();
  }

  switch(businessId: string): void {
    if (this.switchingId()) return;
    this.switchingId.set(businessId);
    this.error.set(null);
    this.auth.switchBusiness(businessId);
    this.ctx.refreshMe();
    this.reload();
    this.switchingId.set(null);
  }

  create(): void {
    const name = this.newBusinessName.trim();
    if (!name || this.creating()) return;

    this.creating.set(true);
    this.error.set(null);
    this.businessApi.create(name).subscribe({
      next: (created) => {
        this.newBusinessName = '';
        this.creating.set(false);
        this.auth.switchBusiness(created.businessId);
        this.ctx.refreshMe();
        this.reload();
      },
      error: () => {
        this.creating.set(false);
        this.error.set('Could not create business.');
      },
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.businessApi.list().subscribe({
      next: (items) => {
        this.businesses.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load businesses.');
      },
    });
  }
}
