import { Component } from '@angular/core';
import { Card } from 'primeng/card';
import { apiBaseUrl } from '../../../core/api-base';

@Component({
  selector: 'app-docs-page',
  imports: [Card],
  templateUrl: './docs.page.html',
  styleUrl: './docs.page.scss',
})
export class DocsPage {
  readonly apiBase = apiBaseUrl();
  readonly openApiUrl = `${apiBaseUrl()}/openapi/v1.json`;
  readonly swaggerUrl = `${apiBaseUrl()}/swagger`;
}
