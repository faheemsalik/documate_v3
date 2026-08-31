import { parseResultJson } from './result-json.util';

describe('parseResultJson', () => {
  it('splits scalar fields and line tables', () => {
    const parsed = parseResultJson({
      invoice_number: 'INVO-005',
      total: 425,
      line_items: [
        { description: 'Widget', qty: 2, amount: 200 },
        { description: 'Service', qty: 1, amount: 225 },
      ],
    });

    expect(parsed.scalars).toHaveLength(2);
    expect(parsed.tables).toHaveLength(1);
    expect(parsed.tables[0].key).toBe('line_items');
    expect(parsed.tables[0].columns).toEqual(['description', 'qty', 'amount']);
  });

  it('returns empty sections for null input', () => {
    const parsed = parseResultJson(null);
    expect(parsed.scalars).toEqual([]);
    expect(parsed.tables).toEqual([]);
  });
});
