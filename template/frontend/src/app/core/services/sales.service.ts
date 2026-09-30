import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  ApiResponse,
  PagedResponse,
  Sale,
  SaleFilters,
  SaleInput,
  UpdateSaleInput,
} from '../models/sale.model';

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/sales';

  getById(id: string) {
    return this.http.get<ApiResponse<Sale>>(`${this.url}/${id}`);
  }

  create(input: SaleInput) {
    return this.http.post<ApiResponse<Sale>>(this.url, input);
  }

  update(id: string, input: UpdateSaleInput) {
    return this.http.put<ApiResponse<Sale>>(`${this.url}/${id}`, input);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  cancel(id: string) {
    return this.http.patch<ApiResponse<Sale>>(`${this.url}/${id}/cancel`, {});
  }

  cancelItem(saleId: string, itemId: string) {
    return this.http.patch<ApiResponse<Sale>>(
      `${this.url}/${saleId}/items/${itemId}/cancel`, {},
    );
  }

  /**
   * Prepared for GET /api/sales, which is not yet exposed by SalesController.
   * The backend must wire pagination/search and implement _order before use.
   */
  list(page = 1, size = 10, order = 'saleDate desc', filters: SaleFilters = {}) {
    let params = new HttpParams()
      .set('_page', page)
      .set('_size', size)
      .set('_order', order);

    const searchTerm = filters.searchTerm?.trim();
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResponse<Sale>>(this.url, { params });
  }
}
