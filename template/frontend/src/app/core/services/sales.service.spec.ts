import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiResponse, Sale, SaleInput, UpdateSaleInput } from '../models/sale.model';
import { SalesService } from './sales.service';

describe('SalesService API contract', () => {
  let service: SalesService;
  let http: HttpTestingController;
  const input: SaleInput = {
    saleDate: '2026-09-30T10:00:00Z',
    customerId: '31b70b8b-c8bb-4c24-befd-68350f07f1a0',
    customerName: 'Customer',
    branchId: 'a2fa0119-1d21-4997-84c2-f51887145138',
    branchName: 'Main branch',
    items: [{ productId: 'd2d3b768-bd68-4d28-83c2-52b4943d5a3a', productName: 'Product', quantity: 10, unitPrice: 25 }],
  };
  const sale: Sale = {
    ...input,
    id: '5f09eb7b-d48d-4a83-a535-d418ad70e08f',
    saleNumber: 'SALE-001',
    totalAmount: 200,
    isCancelled: false,
    items: [{ ...input.items[0], id: '55baf220-3429-42fa-9548-1520bdb6e304', discountRate: 0.2, discountAmount: 50, totalAmount: 200, isCancelled: false }],
  };
  const response: ApiResponse<Sale> = { success: true, message: '', errors: [], data: sale };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(SalesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts only creation inputs and preserves backend dates, discounts and totals', () => {
    service.create(input).subscribe((result) => expect(result).toEqual(response));
    const request = http.expectOne({ method: 'POST', url: '/api/sales' });
    expect(request.request.body).toEqual(input);
    expect(request.request.body.totalAmount).toBeUndefined();
    expect(request.request.body.items[0].discountRate).toBeUndefined();
    request.flush(response, { status: 201, statusText: 'Created' });
  });

  it('gets a sale with the response envelope intact', () => {
    service.getById(sale.id).subscribe((result) => expect(result).toEqual(response));
    http.expectOne({ method: 'GET', url: `/api/sales/${sale.id}` }).flush(response);
  });

  it('keeps item IDs for updates and permits new items without IDs', () => {
    const update: UpdateSaleInput = {
      ...input,
      items: [{ ...input.items[0], id: sale.items[0].id, quantity: 12 }, { ...input.items[0], id: null }],
    };
    service.update(sale.id, update).subscribe();
    const request = http.expectOne({ method: 'PUT', url: `/api/sales/${sale.id}` });
    expect(request.request.body).toEqual(update);
    request.flush(response);
  });

  it('uses PATCH with an empty body for whole-sale and item cancellation', () => {
    service.cancel(sale.id).subscribe();
    const cancel = http.expectOne({ method: 'PATCH', url: `/api/sales/${sale.id}/cancel` });
    expect(cancel.request.body).toEqual({});
    cancel.flush({ ...response, data: { ...sale, isCancelled: true } });

    service.cancelItem(sale.id, sale.items[0].id).subscribe();
    const item = http.expectOne({ method: 'PATCH', url: `/api/sales/${sale.id}/items/${sale.items[0].id}/cancel` });
    expect(item.request.body).toEqual({});
    item.flush(response);
  });

  it('completes deletion on a 204 response without expecting a data envelope', () => {
    const completed = jasmine.createSpy('completed');
    service.delete(sale.id).subscribe({ complete: completed });
    http.expectOne({ method: 'DELETE', url: `/api/sales/${sale.id}` })
      .flush(null, { status: 204, statusText: 'No Content' });
    expect(completed).toHaveBeenCalled();
  });

  it('encodes pagination, ordering and repository search for the pending list endpoint', () => {
    const page = { success: true, message: '', errors: [], data: [sale], currentPage: 2, totalPages: 3, totalCount: 21 };
    service.list(2, 10, 'saleDate desc, customerName asc', { searchTerm: ' A & B ' })
      .subscribe((result) => expect(result).toEqual(page));
    const request = http.expectOne((req) => req.url === '/api/sales');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('_page')).toBe('2');
    expect(request.request.params.get('_size')).toBe('10');
    expect(request.request.params.get('_order')).toBe('saleDate desc, customerName asc');
    expect(request.request.params.get('searchTerm')).toBe('A & B');
    expect(request.request.urlWithParams).toContain('searchTerm=A%20%26%20B');
    request.flush(page);
  });

  it('omits empty filters and preserves backend error details', () => {
    const error = { success: false, message: 'Validation failed.', errors: [{ error: 'Quantity', detail: 'Maximum quantity is 20.' }] };
    service.list(1, 10, 'saleDate desc', { searchTerm: '   ' }).subscribe({
      next: () => fail('Expected an HTTP error'),
      error: (result) => {
        expect(result.status).toBe(400);
        expect(result.error).toEqual(error);
      },
    });
    const request = http.expectOne((req) => req.url === '/api/sales');
    expect(request.request.params.has('searchTerm')).toBeFalse();
    request.flush(error, { status: 400, statusText: 'Bad Request' });
  });
});
