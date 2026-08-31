import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { BusinessApiService, type BusinessProfile } from '../../../core/api/business-api.service';
import { AppContextService } from '../../../core/app-context.service';

@Component({
  selector: 'app-business-profile-page',
  imports: [FormsModule, Button, InputText, Message],
  templateUrl: './business-profile.page.html',
  styleUrl: './business-profile.page.scss',
})
export class BusinessProfilePage implements OnInit {
  private readonly businessApi = inject(BusinessApiService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly profile = signal<BusinessProfile | null>(null);
  name = '';

  ngOnInit(): void {
    this.businessApi.get().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.name = p.name;
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load profile.');
      },
    });
  }

  save(): void {
    if (!this.name.trim()) return;
    this.saving.set(true);
    this.businessApi.update(this.name.trim()).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.saving.set(false);
        this.ctx.refreshMe();
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Save failed.');
      },
    });
  }
}
