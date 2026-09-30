import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Sale } from '../../../core/models/sale.model';
import { SaleDetailsComponent } from './sale-details.component';

describe('Saved sale details', () => {
  let http: HttpTestingController;
  const sale: Sale = {
    id: 'saved-sale', saleNumber: 'SALE-DEMO', saleDate: '2026-09-30T12:30:00Z',
    customerId: 'customer', customerName: 'Customer', branchId: 'branch', branchName: 'Branch',
    totalAmount: 800, isCancelled: false,
    items: [{ id: 'item', productId: 'product', productName: 'Beverage case', quantity: 10, unitPrice: 100,
      discountRate: 0.2, discountAmount: 200, totalAmount: 800, isCancelled: false }],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [SaleDetailsComponent], providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: sale.id })) } },
    ] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('reads saved amounts from the API on every new page instance, including a reload', () => {
    for (let visit = 0; visit < 2; visit++) {
      const fixture = TestBed.createComponent(SaleDetailsComponent);
      fixture.detectChanges();
      http.expectOne({ method: 'GET', url: '/api/sales/saved-sale' })
        .flush({ success: true, message: '', errors: [], data: sale });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="sale-total"]').textContent).toBe('800.00');
      expect(fixture.nativeElement.textContent).toContain('20%');
      expect(fixture.nativeElement.textContent).toContain('200.00');
      expect(fixture.componentInstance.loading).toBeFalse();
      fixture.destroy();
    }
  });

  it('shows a backend error instead of stale or invented totals', () => {
    const fixture = TestBed.createComponent(SaleDetailsComponent);
    fixture.detectChanges();
    http.expectOne('/api/sales/saved-sale').flush({ detail: 'Sale not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toBe('Sale not found.');
    expect(fixture.nativeElement.querySelector('[data-testid="sale-total"]')).toBeNull();
    expect(fixture.componentInstance.loading).toBeFalse();
  });
});
