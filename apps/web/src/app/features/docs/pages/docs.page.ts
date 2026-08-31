import { Component } from '@angular/core';
import { Card } from 'primeng/card';
import { API_BASE_URL } from '../../../core/api-base';

@Component({
  selector: 'app-docs-page',
  imports: [Card],
  templateUrl: './docs.page.html',
  styleUrl: './docs.page.scss',
})
export class DocsPage {
  readonly apiBase = API_BASE_URL;
  readonly openApiUrl = `${API_BASE_URL}/openapi/v1.json`;
  readonly swaggerUrl = `${API_BASE_URL}/swagger`;
}
