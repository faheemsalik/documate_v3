import { Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';
import { formatStatusLabel, statusSeverity, type StatusSeverity } from '../utils/file-status.util';

@Component({
  selector: 'app-status-pill',
  imports: [Tag],
  template: `
    <p-tag
      [value]="label()"
      [severity]="severity()"
      [rounded]="true"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `,
})
export class StatusPillComponent {
  readonly status = input<string | null | undefined>(null);

  readonly label = computed(() => formatStatusLabel(this.status()));
  readonly severity = computed((): StatusSeverity => statusSeverity(this.status()));
}
