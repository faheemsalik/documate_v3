export type SchemaFieldType = 'string' | 'number' | 'integer' | 'boolean';

export interface SchemaField {
  key: string;
  type: SchemaFieldType;
  required: boolean;
}

export interface SchemaTableSection {
  key: string;
  columns: SchemaField[];
}

export interface ParsedSchema {
  fields: SchemaField[];
  tables: SchemaTableSection[];
}

export function parseOutputSchema(json: string): ParsedSchema {
  try {
    const root = JSON.parse(json) as {
      properties?: Record<string, { type?: string; items?: { properties?: Record<string, { type?: string }> } }>;
      required?: string[];
    };
    const required = new Set(root.required ?? []);
    const fields: SchemaField[] = [];
    const tables: SchemaTableSection[] = [];

    for (const [key, prop] of Object.entries(root.properties ?? {})) {
      if (prop.type === 'array') {
        const cols: SchemaField[] = [];
        for (const [colKey, colProp] of Object.entries(prop.items?.properties ?? {})) {
          cols.push({ key: colKey, type: normalizeType(colProp.type), required: false });
        }
        tables.push({ key, columns: cols.length ? cols : [{ key: 'description', type: 'string', required: false }] });
      } else {
        fields.push({ key, type: normalizeType(prop.type), required: required.has(key) });
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
    properties[key] = { type: field.type };
  }

  for (const table of tables) {
    const key = table.key.trim();
    if (!key) continue;
    const colProps: Record<string, { type: string }> = {};
    for (const col of table.columns) {
      const colKey = col.key.trim();
      if (!colKey) continue;
      colProps[colKey] = { type: col.type };
    }
    properties[key] = { type: 'array', items: { type: 'object', properties: colProps } };
  }

  return JSON.stringify({ type: 'object', properties, required }, null, 2);
}

function normalizeType(type?: string): SchemaFieldType {
  if (type === 'number' || type === 'integer' || type === 'boolean') return type;
  return 'string';
}
