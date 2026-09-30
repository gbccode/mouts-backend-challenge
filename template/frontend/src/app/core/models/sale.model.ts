export type { ApiResponse, PagedResponse, ValidationErrorDetail } from './api-response.model';

export interface SaleItem {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  /** A fraction returned by the backend, for example 0.20 for 20%. */
  discountRate: number;
  discountAmount: number;
  totalAmount: number;
  isCancelled: boolean;
}

export interface Sale {
  id: string;
  saleNumber: string;
  /** ISO date/time string as returned by the API. */
  saleDate: string;
  customerId: string;
  customerName: string;
  branchId: string;
  branchName: string;
  totalAmount: number;
  isCancelled: boolean;
  items: SaleItem[];
}

export interface SaleItemInput {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

/** Creation input: sale number, discounts and totals are calculated by the backend. */
export interface SaleInput {
  saleDate: string;
  customerId: string;
  customerName: string;
  branchId: string;
  branchName: string;
  items: SaleItemInput[];
}

export interface UpdateSaleItemInput extends SaleItemInput {
  /** Keep the existing item ID; omit it or use null when adding an item. */
  id?: string | null;
}

export interface UpdateSaleInput extends Omit<SaleInput, 'items'> {
  items: UpdateSaleItemInput[];
}

/** The repository currently supports only searchTerm; HTTP listing is still pending. */
export interface SaleFilters {
  /** Matches sale number, customer name or branch name. */
  searchTerm?: string;
}
