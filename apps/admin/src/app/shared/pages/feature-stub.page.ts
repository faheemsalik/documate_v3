import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'app-feature-stub',
  template: `
    <div class="stub">
      <h1>{{ title() }}</h1>
      <p class="meta">{{ screenId() }}</p>
      <p>{{ blurb() }}</p>
    </div>
  `,
  styles: `
    .stub { max-width: 42rem; }
    h1 { margin: 0 0 0.35rem; font-size: 1.35rem; }
    .meta { margin: 0 0 0.75rem; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.8rem; }
    p { margin: 0; color: var(--p-text-muted-color); }
  `,
})
export class FeatureStubPage {
  private readonly route = inject(ActivatedRoute);
  private readonly data = toSignal(
    this.route.data.pipe(
      map((d) => ({
        title: (d['title'] as string) ?? 'Page',
        screenId: (d['screenId'] as string) ?? '',
        blurb: (d['blurb'] as string) ?? 'Stub — implement in a later Band 17 DQ.',
      })),
    ),
    {
      initialValue: {
        title: 'Page',
        screenId: '',
        blurb: 'Stub — implement in a later Band 17 DQ.',
      },
    },
  );

  title = () => this.data().title;
  screenId = () => this.data().screenId;
  blurb = () => this.data().blurb;
}
