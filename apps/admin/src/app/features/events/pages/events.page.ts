import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import {
  AdminApiService,
  type AdminPublicActionType,
  type AdminPublicEvent,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-events-page',
  imports: [RouterLink, Message, Tag],
  templateUrl: './events.page.html',
  styleUrl: './events.page.scss',
})
export class EventsPage implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly events = signal<AdminPublicEvent[]>([]);
  readonly actionTypes = signal<AdminPublicActionType[]>([]);

  ngOnInit(): void {
    this.loading.set(true);
    this.api.getPublicEventsCatalog().subscribe({
      next: (catalog) => {
        this.events.set(catalog.events);
        this.actionTypes.set(catalog.actionTypes);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load the public event catalog.');
        this.loading.set(false);
      },
    });
  }
}
