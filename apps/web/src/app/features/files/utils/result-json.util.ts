export interface ScalarField {
  key: string;
  value: unknown;
}

export interface TableSection {
  key: string;
  rows: Record<string, unknown>[];
  columns: string[];
}

export interface ParsedResultJson {
  scalars: ScalarField[];
  tables: TableSection[];
}

export function parseResultJson(result: Record<string, unknown> | null | undefined): ParsedResultJson {
  if (!result) {
    return { scalars: [], tables: [] };
  }

  const scalars: ScalarField[] = [];
  const tables: TableSection[] = [];

  for (const [key, value] of Object.entries(result)) {
    if (isTableValue(value)) {
      const rows = value as Record<string, unknown>[];
      const columns = collectColumns(rows);
      tables.push({ key, rows, columns });
    } else {
      scalars.push({ key, value });
    }
  }

  return { scalars, tables };
}

function isTableValue(value: unknown): value is Record<string, unknown>[] {
  return (
    Array.isArray(value) &&
    value.length > 0 &&
    value.every((row) => row !== null && typeof row === 'object' && !Array.isArray(row))
  );
}

function collectColumns(rows: Record<string, unknown>[]): string[] {
  const seen = new Set<string>();
  const columns: string[] = [];
  for (const row of rows) {
    for (const key of Object.keys(row)) {
      if (!seen.has(key)) {
        seen.add(key);
        columns.push(key);
      }
    }
  }
  return columns;
}

export function formatFieldValue(value: unknown): string {
  if (value == null) return '—';
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
}
