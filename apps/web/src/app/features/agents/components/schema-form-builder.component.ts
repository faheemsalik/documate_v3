import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import {
  buildOutputSchema,
  parseOutputSchema,
  type SchemaField,
  type SchemaFieldType,
  type SchemaTableSection,
} from '../utils/schema-builder.util';

@Component({
  selector: 'app-schema-form-builder',
  imports: [FormsModule, Button, Checkbox, InputText, Select],
  templateUrl: './schema-form-builder.component.html',
  styleUrl: './schema-form-builder.component.scss',
})
export class SchemaFormBuilderComponent {
  readonly schemaJson = input.required<string>();
  readonly schemaChange = output<string>();

  readonly fieldTypes = [
    { label: 'Text', value: 'string' },
    { label: 'Number', value: 'number' },
    { label: 'Integer', value: 'integer' },
    { label: 'Boolean', value: 'boolean' },
  ];

  readonly fields = signal<SchemaField[]>([]);
  readonly tables = signal<SchemaTableSection[]>([]);

  constructor() {
    effect(() => {
      const parsed = parseOutputSchema(this.schemaJson());
      this.fields.set(parsed.fields.length ? parsed.fields : [{ key: '', type: 'string', required: false }]);
      this.tables.set(parsed.tables);
    });
  }

  emitChange(): void {
    this.schemaChange.emit(buildOutputSchema(this.fields(), this.tables()));
  }

  addField(): void {
    this.fields.update((f) => [...f, { key: '', type: 'string', required: false }]);
    this.emitChange();
  }

  removeField(index: number): void {
    this.fields.update((f) => f.filter((_, i) => i !== index));
    this.emitChange();
  }

  addTable(): void {
    this.tables.update((t) => [
      ...t,
      { key: 'line_items', columns: [{ key: 'description', type: 'string', required: false }] },
    ]);
    this.emitChange();
  }

  removeTable(index: number): void {
    this.tables.update((t) => t.filter((_, i) => i !== index));
    this.emitChange();
  }

  addTableColumn(tableIndex: number): void {
    this.tables.update((tables) =>
      tables.map((table, i) =>
        i === tableIndex
          ? { ...table, columns: [...table.columns, { key: '', type: 'string' as SchemaFieldType, required: false }] }
          : table,
      ),
    );
    this.emitChange();
  }

  removeTableColumn(tableIndex: number, colIndex: number): void {
    this.tables.update((tables) =>
      tables.map((table, i) =>
        i === tableIndex
          ? { ...table, columns: table.columns.filter((_, ci) => ci !== colIndex) }
          : table,
      ),
    );
    this.emitChange();
  }

  onFieldChange(): void {
    this.emitChange();
  }
}
