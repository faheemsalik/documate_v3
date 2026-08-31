import { buildOutputSchema, parseOutputSchema } from './schema-builder.util';

describe('schema-builder.util', () => {
  it('round-trips header fields and line table', () => {
    const json = JSON.stringify({
      type: 'object',
      properties: {
        invoice_number: { type: 'string' },
        total: { type: 'number' },
        line_items: {
          type: 'array',
          items: {
            type: 'object',
            properties: { description: { type: 'string' }, qty: { type: 'integer' } },
          },
        },
      },
      required: ['invoice_number'],
    });

    const parsed = parseOutputSchema(json);
    expect(parsed.fields).toHaveLength(2);
    expect(parsed.tables[0].key).toBe('line_items');
    expect(parsed.fields[0].required).toBe(true);

    const rebuilt = buildOutputSchema(parsed.fields, parsed.tables);
    const again = parseOutputSchema(rebuilt);
    expect(again.fields.map((f) => f.key)).toContain('invoice_number');
    expect(again.tables[0].columns.length).toBe(2);
  });
});
