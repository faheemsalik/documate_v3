import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { map } from 'rxjs';

/** Temporary page body until the feature DQ lands. */
@Component({
  selector: 'app-feature-stub-page',
  imports: [Card, Tag],
  template: `
    <p-card [header]="pageTitle()">
      <div class="badge-row">
        <p-tag severity="secondary" value="Shell stub" />
      </div>
      <p class="muted">{{ blurb() }}</p>
    </p-card>
  `,
  styles: `
    .badge-row {
      margin-bottom: 0.75rem;
    }
    .muted {
      color: var(--p-text-muted-color);
      margin: 0;
    }
  `,
})
export class FeatureStubPage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = toSignal(this.route.data.pipe(map((d) => d)), {
    initialValue: this.route.snapshot.data,
  });

  readonly pageTitle = computed(() => String(this.data()?.['title'] ?? 'Page'));
  readonly blurb = computed(
    () =>
      String(this.data()?.['blurb'] ?? 'UI for this screen ships in a later Band 16 DQ.')
  );
}
