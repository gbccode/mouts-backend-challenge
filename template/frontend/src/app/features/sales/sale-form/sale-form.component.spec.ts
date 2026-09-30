import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { Sale } from '../../../core/models/sale.model';
import { BRANCH_FIXTURES, CUSTOMER_FIXTURES, PRODUCT_FIXTURES } from '../sales.fixtures';
import { SaleFormComponent } from './sale-form.component';

describe('SaleFormComponent', () => {
  let fixture: ComponentFixture<SaleFormComponent>;
  let component: SaleFormComponent;
  let http: HttpTestingController;
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let navigate: jasmine.Spy;
  const sale: Sale = {
    id: '40000000-0000-4000-8000-000000000001', saleNumber: 'SALE-DEMO',
    saleDate: new Date(2026, 8, 30, 9, 30, 12, 345).toISOString(),
    customerId: CUSTOMER_FIXTURES[0].id, customerName: CUSTOMER_FIXTURES[0].name,
    branchId: BRANCH_FIXTURES[0].id, branchName: BRANCH_FIXTURES[0].name,
    totalAmount: 800, isCancelled: false,
    items: [{
      id: '50000000-0000-4000-8000-000000000001', productId: PRODUCT_FIXTURES[0].id,
      productName: PRODUCT_FIXTURES[0].name, quantity: 10, unitPrice: 100,
      discountRate: 0.2, discountAmount: 200, totalAmount: 800, isCancelled: false,
    }],
  };
  const response = (data: Sale) => ({ success: true, message: '', errors: [], data });

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({}));
    TestBed.configureTestingModule({
      imports: [SaleFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params.asObservable() } }],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = spyOn(TestBed.inject(Router), 'navigate').and.returnValue(Promise.resolve(true));
    fixture = TestBed.createComponent(SaleFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  function fill(): void {
    component.form.controls.saleDate.setValue('2026-09-30T09:30');
    component.selectCustomer(CUSTOMER_FIXTURES[0].id);
    component.selectBranch(BRANCH_FIXTURES[0].id);
    component.selectProduct(0, PRODUCT_FIXTURES[0].id);
    component.items.at(0).patchValue({ quantity: 10, unitPrice: 100 });
  }

  function edit(data: Sale = sale): void {
    params.next(convertToParamMap({ id: data.id }));
    expect(component.loading).toBeTrue();
    http.expectOne(`/api/sales/${data.id}`).flush(response(data));
    fixture.detectChanges();
  }

  it('starts with an empty header and one item, then reveals invalid fields on submit', () => {
    expect(component.form.controls.saleDate.value).toBe('');
    expect(component.form.controls.customerId.value).toBe('');
    expect(component.items.length).toBe(1);
    expect(component.items.at(0).getRawValue()).toEqual({ id: null, productId: '', productName: '', quantity: 1, unitPrice: 1 });
    component.submit();
    expect(component.form.controls.customerId.touched).toBeTrue();
    expect(component.items.at(0).controls.productId.touched).toBeTrue();
    http.expectNone('/api/sales');
  });

  it('rejects empty items, duplicate active products, fractional quantities and invalid prices', () => {
    fill();
    const row = component.items.at(0);
    for (const quantity of [0, 21, 1.5]) {
      row.controls.quantity.setValue(quantity);
      component.submit();
      expect(row.controls.quantity.invalid).withContext(`quantity ${quantity}`).toBeTrue();
    }
    row.controls.quantity.setValue(10);
    for (const price of [0, -1, 1.001]) {
      row.controls.unitPrice.setValue(price);
      component.submit();
      expect(row.controls.unitPrice.invalid).withContext(`price ${price}`).toBeTrue();
    }
    row.controls.unitPrice.setValue(100);
    component.addItem();
    component.selectProduct(1, PRODUCT_FIXTURES[0].id);
    expect(component.items.hasError('duplicateProducts')).toBeTrue();
    component.submit();
    component.removeItem(1);
    expect(component.form.valid).toBeTrue();
    component.removeItem(0);
    component.submit();
    expect(component.items.hasError('noActiveItems')).toBeTrue();
    http.expectNone('/api/sales');
  });

  it('rejects a cleared numeric input from the actual template', () => {
    fill();
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#quantity-0') as HTMLInputElement;
    input.value = '';
    input.dispatchEvent(new Event('input'));
    component.submit();
    expect(component.items.at(0).controls.quantity.hasError('required')).toBeTrue();
    http.expectNone('/api/sales');
  });

  it('converts local time to UTC, maps only create fields, prevents double-submit and uses server totals', () => {
    fill();
    component.submit();
    component.submit();
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBeTrue();
    const request = http.expectOne({ method: 'POST', url: '/api/sales' });
    expect(request.request.body).toEqual({
      saleDate: new Date('2026-09-30T09:30').toISOString(),
      customerId: sale.customerId, customerName: sale.customerName,
      branchId: sale.branchId, branchName: sale.branchName,
      items: [{ productId: sale.items[0].productId, productName: sale.items[0].productName, quantity: 10, unitPrice: 100 }],
    });
    request.flush(response(sale), { status: 201, statusText: 'Created' });
    expect(component.saving).toBeFalse();
    expect(component.savedSale?.totalAmount).toBe(800);
    expect(component.savedSale?.items[0].discountAmount).toBe(200);
    expect(navigate).toHaveBeenCalledOnceWith(['/sales', sale.id]);
  });

  it('loads only active rows, preserves their IDs and shows cancelled items separately', () => {
    const cancelled = { ...sale.items[0], id: '50000000-0000-4000-8000-000000000002', isCancelled: true };
    edit({ ...sale, items: [...sale.items, cancelled] });
    expect(component.loading).toBeFalse();
    expect(component.items.length).toBe(1);
    expect(component.cancelledItems).toEqual([cancelled]);
    expect(component.form.controls.saleDate.value).toBe('2026-09-30T09:30:12.345');
    expect(component.items.at(0).controls.productId.disabled).toBeTrue();
    expect(component.items.at(0).controls.id.value).toBe(sale.items[0].id);
    expect(component.form.valid).toBeTrue();
    expect(fixture.nativeElement.querySelector('#cancelled-items').textContent).toContain('Cancelled items');

    component.items.at(0).controls.quantity.setValue(12);
    component.addItem();
    component.selectProduct(1, PRODUCT_FIXTURES[1].id);
    component.submit();
    const request = http.expectOne({ method: 'PUT', url: `/api/sales/${sale.id}` });
    expect(request.request.body.items).toEqual([
      { id: sale.items[0].id, productId: sale.items[0].productId, productName: sale.items[0].productName, quantity: 12, unitPrice: 100 },
      { id: null, productId: PRODUCT_FIXTURES[1].id, productName: PRODUCT_FIXTURES[1].name, quantity: 1, unitPrice: 25 },
    ]);
    request.flush(response(sale));
  });

  it('keeps external identities that are absent from fixtures and omits removed IDs on update', () => {
    const custom = { ...sale,
      customerId: '60000000-0000-4000-8000-000000000001', customerName: 'Existing customer',
      branchId: '60000000-0000-4000-8000-000000000002', branchName: 'Existing branch',
      items: [{ ...sale.items[0], productId: '60000000-0000-4000-8000-000000000003', productName: 'Existing product' }],
    };
    edit(custom);
    expect(component.customers.some((item) => item.id === custom.customerId && item.name === custom.customerName)).toBeTrue();
    expect(component.products.some((item) => item.id === custom.items[0].productId)).toBeTrue();
    component.removeItem(0);
    component.addItem();
    component.selectProduct(0, PRODUCT_FIXTURES[2].id);
    component.submit();
    const request = http.expectOne({ method: 'PUT', url: `/api/sales/${sale.id}` });
    expect(request.request.body.customerId).toBe(custom.customerId);
    expect(request.request.body.items.length).toBe(1);
    expect(request.request.body.items[0].id).toBeNull();
    request.flush(response(sale));
  });

  for (const [body, message] of [
    [{ detail: 'Detailed validation failure', message: 'Generic failure' }, 'Detailed validation failure'],
    [{ message: 'Sale cannot be updated' }, 'Sale cannot be updated'],
    [null, 'Unable to save the sale. Check the API and try again.'],
  ] as const) {
    it(`resets saving and displays the API error: ${message}`, () => {
      fill();
      component.submit();
      http.expectOne('/api/sales').flush(body, { status: 400, statusText: 'Bad Request' });
      expect(component.saving).toBeFalse();
      expect(component.error).toBe(message);
      expect(component.items.at(0).controls.quantity.value).toBe(10);
      expect(navigate).not.toHaveBeenCalled();
    });
  }

  it('does not navigate when the response has no saved sale', () => {
    fill();
    component.submit();
    http.expectOne('/api/sales').flush({ success: true, data: null, message: '', errors: [] });
    expect(component.saving).toBeFalse();
    expect(component.error).toBe('The API did not return the saved sale.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('blocks editing a cancelled sale and prevents submissions after a failed load', () => {
    edit({ ...sale, isCancelled: true });
    component.submit();
    http.expectNone((request) => request.method === 'PUT');
    params.next(convertToParamMap({ id: 'missing' }));
    http.expectOne('/api/sales/missing').flush({ message: 'Sale not found.' }, { status: 404, statusText: 'Not Found' });
    expect(component.loading).toBeFalse();
    expect(component.loadFailed).toBeTrue();
    component.submit();
    expect(component.error).toBe('Sale not found.');
    http.expectNone((request) => request.method === 'PUT');
  });
});
