/** Demo external identities only. Sales always use the real /api/sales backend. */
export interface ExternalReference {
  readonly id: string;
  readonly name: string;
}

export interface ProductReference extends ExternalReference {
  readonly unitPrice: number;
}

export const CUSTOMER_FIXTURES: readonly ExternalReference[] = [
  { id: '10000000-0000-4000-8000-000000000001', name: 'Acme Market' },
  { id: '10000000-0000-4000-8000-000000000002', name: 'Central Grocery' },
];

export const BRANCH_FIXTURES: readonly ExternalReference[] = [
  { id: '20000000-0000-4000-8000-000000000001', name: 'Blumenau' },
  { id: '20000000-0000-4000-8000-000000000002', name: 'Joinville' },
];

export const PRODUCT_FIXTURES: readonly ProductReference[] = [
  { id: '30000000-0000-4000-8000-000000000001', name: 'Beverage case', unitPrice: 100 },
  { id: '30000000-0000-4000-8000-000000000002', name: 'Sparkling water case', unitPrice: 25 },
  { id: '30000000-0000-4000-8000-000000000003', name: 'Juice case', unitPrice: 75 },
];
