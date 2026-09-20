export type SchemaFieldType = 'string' | 'number' | 'integer' | 'boolean' | 'date';

export interface SchemaFormatOption {
  label: string;
  value: string;
}

/** Document-side date patterns (what appears on the page). */
export const DATE_INPUT_FORMATS: SchemaFormatOption[] = [
  { label: 'MM/DD/YYYY', value: 'MM/DD/YYYY' },
  { label: 'DD/MM/YYYY', value: 'DD/MM/YYYY' },
  { label: 'YYYY-MM-DD', value: 'YYYY-MM-DD' },
  { label: 'DD-MMM-YYYY', value: 'DD-MMM-YYYY' },
  { label: 'MMM DD, YYYY', value: 'MMM DD, YYYY' },
  { label: 'ISO-8601', value: 'ISO-8601' },
];

/** Preferred extract JSON date formats. */
export const DATE_OUTPUT_FORMATS: SchemaFormatOption[] = [
  { label: 'YYYY-MM-DD', value: 'YYYY-MM-DD' },
  { label: 'MM/DD/YYYY', value: 'MM/DD/YYYY' },
  { label: 'DD/MM/YYYY', value: 'DD/MM/YYYY' },
  { label: 'ISO-8601', value: 'ISO-8601' },
];

/** Document-side number patterns. */
export const NUMBER_INPUT_FORMATS: SchemaFormatOption[] = [
  { label: '1,234.56 (US)', value: 'US' },
  { label: '1.234,56 (EU)', value: 'EU' },
  { label: '1234.56 (plain)', value: 'plain' },
  { label: 'Integer only', value: 'integer' },
  { label: 'Currency ($1,234.56)', value: 'currency_us' },
  { label: 'Currency (1.234,56 €)', value: 'currency_eu' },
];

/** Preferred extract JSON number formats. */
export const NUMBER_OUTPUT_FORMATS: SchemaFormatOption[] = [
  { label: 'Plain decimal', value: 'decimal' },
  { label: 'Fixed 2 places', value: 'fixed_2' },
  { label: 'Integer', value: 'integer' },
];

export const DEFAULT_DATE_INPUT_FORMAT = 'MM/DD/YYYY';
export const DEFAULT_DATE_OUTPUT_FORMAT = 'YYYY-MM-DD';
export const DEFAULT_NUMBER_INPUT_FORMAT = 'US';
export const DEFAULT_NUMBER_OUTPUT_FORMAT = 'decimal';

const X_TYPE = 'x-documate-type';
const X_INPUT = 'x-documate-inputFormat';
const X_OUTPUT = 'x-documate-outputFormat';
const X_CAPTIONS = 'x-documate-captions';
const CAPTIONS_PREFIX = 'Common captions:';

export interface SchemaField {
  /** JSON Schema property name (snake_case). */
  key: string;
  /** Human-readable display name (JSON Schema `title`). */
  label: string;
  type: SchemaFieldType;
  required: boolean;
  /** Additional extract instructions (JSON Schema `description`, without captions suffix). */
  description: string;
  /** Alternate labels that appear on documents. */
  captions: string[];
  /** Predefined input format (date / number / integer). */
  inputFormat: string;
  /** Predefined output format (date / number / integer). */
  outputFormat: string;
}

export interface SchemaTableSection {
  key: string;
  label: string;
  description: string;
  captions: string[];
  columns: SchemaField[];
}

export interface ParsedSchema {
  fields: SchemaField[];
  tables: SchemaTableSection[];
}

interface SchemaNode {
  type?: string;
  format?: string;
  title?: string;
  description?: string;
  items?: { properties?: Record<string, SchemaNode> };
  [key: string]: unknown;
}

export function parseOutputSchema(json: string): ParsedSchema {
  try {
    const root = JSON.parse(json) as {
      properties?: Record<string, SchemaNode>;
      required?: string[];
    };
    const required = new Set(root.required ?? []);
    const fields: SchemaField[] = [];
    const tables: SchemaTableSection[] = [];

    for (const [key, prop] of Object.entries(root.properties ?? {})) {
      if (prop.type === 'array') {
        const cols: SchemaField[] = [];
        for (const [colKey, colProp] of Object.entries(prop.items?.properties ?? {})) {
          cols.push(toField(colKey, colProp, false));
        }
        tables.push({
          key,
          label: readLabel(key, prop),
          description: stripCaptionsFromDescription(readDescription(prop)),
          captions: readCaptions(prop),
          columns: cols.length ? cols : [emptyField()],
        });
      } else {
        fields.push(toField(key, prop, required.has(key)));
      }
    }

    return { fields, tables };
  } catch {
    return { fields: [], tables: [] };
  }
}

export function buildOutputSchema(fields: SchemaField[], tables: SchemaTableSection[]): string {
  const properties: Record<string, unknown> = {};
  const required = fields.filter((f) => f.required && f.key.trim()).map((f) => f.key.trim());

  for (const field of fields) {
    const key = field.key.trim();
    if (!key) continue;
    properties[key] = fieldNode(field);
  }

  for (const table of tables) {
    const key = table.key.trim();
    if (!key) continue;
    const colProps: Record<string, unknown> = {};
    for (const col of table.columns) {
      const colKey = col.key.trim();
      if (!colKey) continue;
      colProps[colKey] = fieldNode(col);
    }
    properties[key] = withMeta(
      { type: 'array', items: { type: 'object', properties: colProps } },
      table.description,
      table.label,
      table.captions,
    );
  }

  return JSON.stringify({ type: 'object', properties, required }, null, 2);
}

export function emptyField(label = ''): SchemaField {
  return {
    key: '',
    label,
    type: 'string',
    required: false,
    description: '',
    captions: [],
    inputFormat: '',
    outputFormat: '',
  };
}

/** Lowercase snake_case from a human label (strip punctuation, collapse spaces). */
export function toSnakeCaseKey(label: string): string {
  const cleaned = label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .replace(/_+/g, '_');
  return cleaned || 'field';
}

/** Append `_2`, `_3`, … when `base` is already in `used`. */
export function uniqueKey(base: string, used: Iterable<string>): string {
  const taken = new Set(used);
  if (!taken.has(base)) return base;
  let n = 2;
  while (taken.has(`${base}_${n}`)) n += 1;
  return `${base}_${n}`;
}

export function assignKeyFromLabel(label: string, usedKeys: Iterable<string>): string {
  return uniqueKey(toSnakeCaseKey(label), usedKeys);
}

export function usesFormats(type: SchemaFieldType): boolean {
  return type === 'date' || type === 'number' || type === 'integer';
}

export function defaultFormatsForType(type: SchemaFieldType): { inputFormat: string; outputFormat: string } {
  if (type === 'date') {
    return { inputFormat: DEFAULT_DATE_INPUT_FORMAT, outputFormat: DEFAULT_DATE_OUTPUT_FORMAT };
  }
  if (type === 'number' || type === 'integer') {
    return {
      inputFormat: DEFAULT_NUMBER_INPUT_FORMAT,
      outputFormat: type === 'integer' ? 'integer' : DEFAULT_NUMBER_OUTPUT_FORMAT,
    };
  }
  return { inputFormat: '', outputFormat: '' };
}

export function parseCaptionsInput(value: string): string[] {
  return value
    .split(',')
    .map((c) => c.trim())
    .filter(Boolean);
}

export function formatCaptionsInput(captions: string[]): string {
  return captions.join(', ');
}

function toField(key: string, node: SchemaNode | undefined, required: boolean): SchemaField {
  const type = normalizeType(node);
  const formats = usesFormats(type)
    ? {
        inputFormat: readStringProp(node, X_INPUT) || defaultFormatsForType(type).inputFormat,
        outputFormat: readStringProp(node, X_OUTPUT) || defaultFormatsForType(type).outputFormat,
      }
    : { inputFormat: '', outputFormat: '' };

  return {
    key,
    label: readLabel(key, node),
    type,
    required,
    description: stripCaptionsFromDescription(readDescription(node)),
    captions: readCaptions(node),
    ...formats,
  };
}

function fieldNode(field: SchemaField): Record<string, unknown> {
  const node: Record<string, unknown> =
    field.type === 'date'
      ? { type: 'string', format: 'date', [X_TYPE]: 'date' }
      : { type: field.type };

  if (usesFormats(field.type)) {
    const input = field.inputFormat?.trim();
    const output = field.outputFormat?.trim();
    if (input) node[X_INPUT] = input;
    if (output) node[X_OUTPUT] = output;
  }

  return withMeta(node, field.description, field.label, field.captions);
}

function withMeta(
  node: Record<string, unknown>,
  description?: string,
  title?: string,
  captions?: string[],
): Record<string, unknown> {
  const titleText = title?.trim();
  if (titleText) node['title'] = titleText;

  const cleanCaptions = (captions ?? []).map((c) => c.trim()).filter(Boolean);
  if (cleanCaptions.length) node[X_CAPTIONS] = cleanCaptions;

  const descText = enrichDescription(description ?? '', cleanCaptions);
  if (descText) node['description'] = descText;
  return node;
}

function enrichDescription(description: string, captions: string[]): string {
  const base = stripCaptionsFromDescription(description);
  if (!captions.length) return base;
  const suffix = `${CAPTIONS_PREFIX} ${captions.join(', ')}`;
  return base ? `${base}\n${suffix}` : suffix;
}

function stripCaptionsFromDescription(desc: string): string {
  if (!desc) return '';
  const lines = desc.split('\n').filter((line) => !line.trimStart().startsWith(CAPTIONS_PREFIX));
  return lines.join('\n').trim();
}

function readDescription(node?: SchemaNode): string {
  return typeof node?.description === 'string' ? node.description : '';
}

function readCaptions(node?: SchemaNode): string[] {
  const raw = node?.[X_CAPTIONS];
  if (Array.isArray(raw)) {
    return raw.map((c) => String(c).trim()).filter(Boolean);
  }
  if (typeof raw === 'string' && raw.trim()) {
    return parseCaptionsInput(raw);
  }
  return [];
}

function readStringProp(node: SchemaNode | undefined, key: string): string {
  const value = node?.[key];
  return typeof value === 'string' ? value.trim() : '';
}

function readLabel(key: string, node?: SchemaNode): string {
  if (typeof node?.title === 'string' && node.title.trim()) return node.title.trim();
  return humanizeKey(key);
}

function humanizeKey(key: string): string {
  return key
    .split('_')
    .filter(Boolean)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');
}

function normalizeType(node?: SchemaNode): SchemaFieldType {
  if (readStringProp(node, X_TYPE) === 'date') return 'date';
  if (node?.type === 'string' && node.format === 'date') return 'date';
  if (node?.type === 'number' || node?.type === 'integer' || node?.type === 'boolean') {
    return node.type;
  }
  return 'string';
}
