import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import {
  assignKeyFromLabel,
  buildOutputSchema,
  DATE_INPUT_FORMATS,
  DATE_OUTPUT_FORMATS,
  defaultFormatsForType,
  emptyField,
  formatCaptionsInput,
  NUMBER_INPUT_FORMATS,
  NUMBER_OUTPUT_FORMATS,
  parseCaptionsInput,
  parseOutputSchema,
  usesFormats,
  type SchemaField,
  type SchemaFieldType,
  type SchemaTableSection,
} from '../utils/schema-builder.util';

@Component({
  selector: 'app-schema-form-builder',
  imports: [FormsModule, Button, Checkbox, InputText, Select, Tag],
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
    { label: 'Date', value: 'date' },
    { label: 'Boolean', value: 'boolean' },
  ];

  readonly dateInputFormats = DATE_INPUT_FORMATS;
  readonly dateOutputFormats = DATE_OUTPUT_FORMATS;
  readonly numberInputFormats = NUMBER_INPUT_FORMATS;
  readonly numberOutputFormats = NUMBER_OUTPUT_FORMATS;

  readonly fields = signal<SchemaField[]>([]);
  readonly tables = signal<SchemaTableSection[]>([]);
  readonly expanded = signal<Set<string>>(new Set());

  /** Local draft strings for captions inputs keyed by expand id. */
  private readonly captionDrafts = new Map<string, string>();

  constructor() {
    effect(() => {
      const parsed = parseOutputSchema(this.schemaJson());
      this.fields.set(parsed.fields.length ? parsed.fields : [emptyField()]);
      this.tables.set(parsed.tables);
      this.captionDrafts.clear();
    });
  }

  emitChange(): void {
    this.schemaChange.emit(buildOutputSchema(this.fields(), this.tables()));
  }

  addField(): void {
    this.fields.update((f) => [...f, emptyField()]);
    this.emitChange();
  }

  removeField(index: number): void {
    this.fields.update((f) => f.filter((_, i) => i !== index));
    this.expanded.update((set) => {
      const next = new Set(set);
      next.delete(this.fieldId(index));
      return next;
    });
    this.emitChange();
  }

  onFieldLabelChange(index: number): void {
    this.fields.update((fields) => {
      const next = fields.map((f) => ({ ...f, captions: [...f.captions] }));
      const field = next[index];
      const used = this.rootUsedKeys(next, this.tables(), index, null);
      field.key = field.label.trim() ? assignKeyFromLabel(field.label, used) : '';
      return next;
    });
    this.emitChange();
  }

  onFieldTypeChange(index: number): void {
    this.fields.update((fields) => {
      const next = fields.map((f) => ({ ...f, captions: [...f.captions] }));
      applyTypeDefaults(next[index]);
      return next;
    });
    this.emitChange();
  }

  addTable(): void {
    this.tables.update((tables) => {
      const used = this.rootUsedKeys(this.fields(), tables, null, null);
      const key = assignKeyFromLabel('Line Items', used);
      return [
        ...tables,
        { key, label: 'Line Items', description: '', captions: [], columns: [emptyField()] },
      ];
    });
    this.emitChange();
  }

  removeTable(index: number): void {
    this.tables.update((t) => t.filter((_, i) => i !== index));
    this.expanded.update((set) => {
      const next = new Set(set);
      next.delete(this.tableId(index));
      return next;
    });
    this.emitChange();
  }

  onTableLabelChange(tableIndex: number): void {
    this.tables.update((tables) => {
      const next = tables.map((t) => ({
        ...t,
        captions: [...t.captions],
        columns: t.columns.map((c) => ({ ...c, captions: [...c.captions] })),
      }));
      const table = next[tableIndex];
      const used = this.rootUsedKeys(this.fields(), next, null, tableIndex);
      table.key = table.label.trim() ? assignKeyFromLabel(table.label, used) : '';
      return next;
    });
    this.emitChange();
  }

  addTableColumn(tableIndex: number): void {
    this.tables.update((tables) =>
      tables.map((table, i) =>
        i === tableIndex ? { ...table, columns: [...table.columns, emptyField()] } : table,
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
    this.expanded.update((set) => {
      const next = new Set(set);
      next.delete(this.colId(tableIndex, colIndex));
      return next;
    });
    this.emitChange();
  }

  onColumnLabelChange(tableIndex: number, colIndex: number): void {
    this.tables.update((tables) => {
      const next = tables.map((t) => ({
        ...t,
        captions: [...t.captions],
        columns: t.columns.map((c) => ({ ...c, captions: [...c.captions] })),
      }));
      const col = next[tableIndex].columns[colIndex];
      const used = next[tableIndex].columns
        .map((c, i) => (i === colIndex ? '' : c.key.trim()))
        .filter(Boolean);
      col.key = col.label.trim() ? assignKeyFromLabel(col.label, used) : '';
      return next;
    });
    this.emitChange();
  }

  onColumnTypeChange(tableIndex: number, colIndex: number): void {
    this.tables.update((tables) => {
      const next = tables.map((t) => ({
        ...t,
        captions: [...t.captions],
        columns: t.columns.map((c) => ({ ...c, captions: [...c.captions] })),
      }));
      applyTypeDefaults(next[tableIndex].columns[colIndex]);
      return next;
    });
    this.emitChange();
  }

  onFieldChange(): void {
    this.emitChange();
  }

  isExpanded(id: string): boolean {
    return this.expanded().has(id);
  }

  toggleDetails(id: string): void {
    this.expanded.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  openDetails(id: string): void {
    this.expanded.update((set) => new Set(set).add(id));
  }

  fieldId(index: number): string {
    return `f:${index}`;
  }

  tableId(index: number): string {
    return `t:${index}`;
  }

  colId(tableIndex: number, colIndex: number): string {
    return `c:${tableIndex}:${colIndex}`;
  }

  usesFormats = usesFormats;

  inputFormatsFor(type: SchemaFieldType) {
    return type === 'date' ? this.dateInputFormats : this.numberInputFormats;
  }

  outputFormatsFor(type: SchemaFieldType) {
    return type === 'date' ? this.dateOutputFormats : this.numberOutputFormats;
  }

  captionsDraft(id: string, captions: string[]): string {
    return this.captionDrafts.get(id) ?? formatCaptionsInput(captions);
  }

  onFieldCaptionsChange(index: number, value: string): void {
    const id = this.fieldId(index);
    this.captionDrafts.set(id, value);
    this.fields.update((fields) => {
      const next = fields.map((f) => ({ ...f, captions: [...f.captions] }));
      next[index].captions = parseCaptionsInput(value);
      return next;
    });
    this.emitChange();
  }

  onTableCaptionsChange(tableIndex: number, value: string): void {
    const id = this.tableId(tableIndex);
    this.captionDrafts.set(id, value);
    this.tables.update((tables) => {
      const next = tables.map((t) => ({
        ...t,
        captions: [...t.captions],
        columns: t.columns.map((c) => ({ ...c, captions: [...c.captions] })),
      }));
      next[tableIndex].captions = parseCaptionsInput(value);
      return next;
    });
    this.emitChange();
  }

  onColumnCaptionsChange(tableIndex: number, colIndex: number, value: string): void {
    const id = this.colId(tableIndex, colIndex);
    this.captionDrafts.set(id, value);
    this.tables.update((tables) => {
      const next = tables.map((t) => ({
        ...t,
        captions: [...t.captions],
        columns: t.columns.map((c) => ({ ...c, captions: [...c.captions] })),
      }));
      next[tableIndex].columns[colIndex].captions = parseCaptionsInput(value);
      return next;
    });
    this.emitChange();
  }

  private rootUsedKeys(
    fields: SchemaField[],
    tables: SchemaTableSection[],
    excludeFieldIndex: number | null,
    excludeTableIndex: number | null,
  ): string[] {
    const keys: string[] = [];
    for (let i = 0; i < fields.length; i++) {
      if (i === excludeFieldIndex) continue;
      const k = fields[i].key.trim();
      if (k) keys.push(k);
    }
    for (let i = 0; i < tables.length; i++) {
      if (i === excludeTableIndex) continue;
      const k = tables[i].key.trim();
      if (k) keys.push(k);
    }
    return keys;
  }
}

function applyTypeDefaults(field: SchemaField): void {
  if (usesFormats(field.type)) {
    const defaults = defaultFormatsForType(field.type);
    field.inputFormat = defaults.inputFormat;
    field.outputFormat = defaults.outputFormat;
  } else {
    field.inputFormat = '';
    field.outputFormat = '';
  }
}
