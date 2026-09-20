import {
  assignKeyFromLabel,
  buildOutputSchema,
  DEFAULT_DATE_OUTPUT_FORMAT,
  DEFAULT_NUMBER_INPUT_FORMAT,
  parseCaptionsInput,
  parseOutputSchema,
  toSnakeCaseKey,
  uniqueKey,
} from './schema-builder.util';

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
    expect(parsed.fields[0].label).toBe('Invoice Number');

    const rebuilt = buildOutputSchema(parsed.fields, parsed.tables);
    const again = parseOutputSchema(rebuilt);
    expect(again.fields.map((f) => f.key)).toContain('invoice_number');
    expect(again.tables[0].columns.length).toBe(2);
  });

  it('round-trips JSON Schema title as label and description as instructions', () => {
    const json = JSON.stringify({
      type: 'object',
      properties: {
        invoice_number: {
          type: 'string',
          title: 'Invoice Number',
          description: 'Supplier invoice no',
        },
        line_items: {
          type: 'array',
          title: 'Line Items',
          description: 'Product lines',
          items: {
            type: 'object',
            properties: {
              qty: { type: 'integer', title: 'Qty', description: 'Units shipped' },
            },
          },
        },
      },
      required: ['invoice_number'],
    });

    const parsed = parseOutputSchema(json);
    expect(parsed.fields[0].label).toBe('Invoice Number');
    expect(parsed.fields[0].description).toBe('Supplier invoice no');
    expect(parsed.tables[0].label).toBe('Line Items');
    expect(parsed.tables[0].description).toBe('Product lines');
    expect(parsed.tables[0].columns[0].label).toBe('Qty');
    expect(parsed.tables[0].columns[0].description).toBe('Units shipped');

    const rebuilt = JSON.parse(buildOutputSchema(parsed.fields, parsed.tables)) as {
      properties: {
        invoice_number: { title?: string; description?: string };
        line_items: {
          title?: string;
          description?: string;
          items: { properties: { qty: { title?: string; description?: string } } };
        };
      };
    };
    expect(rebuilt.properties.invoice_number.title).toBe('Invoice Number');
    expect(rebuilt.properties.invoice_number.description).toBe('Supplier invoice no');
    expect(rebuilt.properties.line_items.title).toBe('Line Items');
    expect(rebuilt.properties.line_items.description).toBe('Product lines');
    expect(rebuilt.properties.line_items.items.properties.qty.title).toBe('Qty');
    expect(rebuilt.properties.line_items.items.properties.qty.description).toBe('Units shipped');
  });

  it('omits empty descriptions and titles from built schema', () => {
    const rebuilt = JSON.parse(
      buildOutputSchema(
        [
          {
            key: 'total',
            label: '',
            type: 'number',
            required: false,
            description: '  ',
            captions: [],
            inputFormat: '',
            outputFormat: '',
          },
        ],
        [
          {
            key: 'lines',
            label: '',
            description: '',
            captions: [],
            columns: [
              {
                key: 'sku',
                label: '',
                type: 'string',
                required: false,
                description: '',
                captions: [],
                inputFormat: '',
                outputFormat: '',
              },
            ],
          },
        ],
      ),
    ) as {
      properties: {
        total: { title?: string; description?: string };
        lines: { title?: string; description?: string };
      };
    };
    expect(rebuilt.properties.total.title).toBeUndefined();
    expect(rebuilt.properties.total.description).toBeUndefined();
    expect(rebuilt.properties.lines.title).toBeUndefined();
    expect(rebuilt.properties.lines.description).toBeUndefined();
  });

  it('derives snake_case keys from human labels with uniqueness', () => {
    expect(toSnakeCaseKey('Purchase Order No')).toBe('purchase_order_no');
    expect(toSnakeCaseKey('  VAT % / Tax  ')).toBe('vat_tax');
    expect(uniqueKey('total', ['total'])).toBe('total_2');
    expect(uniqueKey('total', ['total', 'total_2'])).toBe('total_3');
    expect(assignKeyFromLabel('Purchase Order No', ['purchase_order_no'])).toBe('purchase_order_no_2');
  });

  it('round-trips date type with input/output formats', () => {
    const rebuilt = JSON.parse(
      buildOutputSchema(
        [
          {
            key: 'invoice_date',
            label: 'Invoice Date',
            type: 'date',
            required: true,
            description: 'Date on invoice header',
            captions: [],
            inputFormat: 'DD/MM/YYYY',
            outputFormat: DEFAULT_DATE_OUTPUT_FORMAT,
          },
        ],
        [],
      ),
    ) as {
      properties: {
        invoice_date: Record<string, unknown>;
      };
      required: string[];
    };

    const node = rebuilt.properties.invoice_date;
    expect(node['type']).toBe('string');
    expect(node['format']).toBe('date');
    expect(node['x-documate-type']).toBe('date');
    expect(node['x-documate-inputFormat']).toBe('DD/MM/YYYY');
    expect(node['x-documate-outputFormat']).toBe('YYYY-MM-DD');
    expect(rebuilt.required).toContain('invoice_date');

    const parsed = parseOutputSchema(JSON.stringify(rebuilt));
    expect(parsed.fields[0].type).toBe('date');
    expect(parsed.fields[0].inputFormat).toBe('DD/MM/YYYY');
    expect(parsed.fields[0].outputFormat).toBe('YYYY-MM-DD');
    expect(parsed.fields[0].description).toBe('Date on invoice header');
  });

  it('round-trips number formats and integer formats', () => {
    const rebuilt = JSON.parse(
      buildOutputSchema(
        [
          {
            key: 'amount',
            label: 'Amount',
            type: 'number',
            required: false,
            description: '',
            captions: [],
            inputFormat: DEFAULT_NUMBER_INPUT_FORMAT,
            outputFormat: 'fixed_2',
          },
          {
            key: 'qty',
            label: 'Qty',
            type: 'integer',
            required: false,
            description: '',
            captions: [],
            inputFormat: 'integer',
            outputFormat: 'integer',
          },
        ],
        [],
      ),
    ) as {
      properties: {
        amount: Record<string, unknown>;
        qty: Record<string, unknown>;
      };
    };

    expect(rebuilt.properties.amount['type']).toBe('number');
    expect(rebuilt.properties.amount['x-documate-inputFormat']).toBe('US');
    expect(rebuilt.properties.amount['x-documate-outputFormat']).toBe('fixed_2');
    expect(rebuilt.properties.qty['type']).toBe('integer');
    expect(rebuilt.properties.qty['x-documate-inputFormat']).toBe('integer');

    const parsed = parseOutputSchema(JSON.stringify(rebuilt));
    expect(parsed.fields[0].inputFormat).toBe('US');
    expect(parsed.fields[0].outputFormat).toBe('fixed_2');
    expect(parsed.fields[1].type).toBe('integer');
    expect(parsed.fields[1].outputFormat).toBe('integer');
  });

  it('round-trips captions as x-documate-captions and enriches description', () => {
    const rebuilt = JSON.parse(
      buildOutputSchema(
        [
          {
            key: 'po_number',
            label: 'PO Number',
            type: 'string',
            required: false,
            description: 'Look near the top',
            captions: ['PO No', 'P.O.#', 'Purchase Order'],
            inputFormat: '',
            outputFormat: '',
          },
        ],
        [
          {
            key: 'lines',
            label: 'Lines',
            description: 'Table body',
            captions: ['Line Items', 'Details'],
            columns: [
              {
                key: 'sku',
                label: 'SKU',
                type: 'string',
                required: false,
                description: '',
                captions: ['Item #', 'Code'],
                inputFormat: '',
                outputFormat: '',
              },
            ],
          },
        ],
      ),
    ) as {
      properties: {
        po_number: Record<string, unknown>;
        lines: Record<string, unknown> & {
          items: { properties: { sku: Record<string, unknown> } };
        };
      };
    };

    expect(rebuilt.properties.po_number['x-documate-captions']).toEqual([
      'PO No',
      'P.O.#',
      'Purchase Order',
    ]);
    expect(rebuilt.properties.po_number['description']).toBe(
      'Look near the top\nCommon captions: PO No, P.O.#, Purchase Order',
    );
    expect(rebuilt.properties.lines['x-documate-captions']).toEqual(['Line Items', 'Details']);
    expect(rebuilt.properties.lines.items.properties.sku['x-documate-captions']).toEqual([
      'Item #',
      'Code',
    ]);

    const parsed = parseOutputSchema(JSON.stringify(rebuilt));
    expect(parsed.fields[0].captions).toEqual(['PO No', 'P.O.#', 'Purchase Order']);
    expect(parsed.fields[0].description).toBe('Look near the top');
    expect(parsed.tables[0].captions).toEqual(['Line Items', 'Details']);
    expect(parsed.tables[0].description).toBe('Table body');
    expect(parsed.tables[0].columns[0].captions).toEqual(['Item #', 'Code']);

    // Second build must not duplicate Common captions in description.
    const again = JSON.parse(buildOutputSchema(parsed.fields, parsed.tables)) as {
      properties: { po_number: { description?: string } };
    };
    expect(again.properties.po_number.description).toBe(
      'Look near the top\nCommon captions: PO No, P.O.#, Purchase Order',
    );
  });

  it('parses comma-separated captions input', () => {
    expect(parseCaptionsInput(' PO No , P.O.#,  ')).toEqual(['PO No', 'P.O.#']);
  });

  it('detects date from format:date when x-documate-type is absent', () => {
    const parsed = parseOutputSchema(
      JSON.stringify({
        type: 'object',
        properties: {
          due: { type: 'string', format: 'date', title: 'Due Date' },
        },
        required: [],
      }),
    );
    expect(parsed.fields[0].type).toBe('date');
  });
});
