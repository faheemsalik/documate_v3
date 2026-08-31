import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Card } from 'primeng/card';
import { map } from 'rxjs';

@Component({
  selector: 'app-coming-soon-page',
  imports: [Card],
  template: `
    <p-card [header]="pageTitle()">
      <p class="muted">Coming soon — deferred for this MVP (Plan 07).</p>
    </p-card>
  `,
  styles: `
    .muted {
      color: var(--p-text-muted-color);
      margin: 0;
    }
  `,
})
export class ComingSoonPage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = toSignal(this.route.data.pipe(map((d) => d)), {
    initialValue: this.route.snapshot.data,
  });

  readonly pageTitle = computed(() => String(this.data()?.['title'] ?? 'Coming soon'));
}
