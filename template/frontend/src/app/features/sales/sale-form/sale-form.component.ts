import { DecimalPipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, EMPTY, finalize, map, switchMap, tap } from 'rxjs';
import { Sale, SaleInput, SaleItem, UpdateSaleInput } from '../../../core/models/sale.model';
import { SalesService } from '../../../core/services/sales.service';
import { BRANCH_FIXTURES, CUSTOMER_FIXTURES, ExternalReference, PRODUCT_FIXTURES, ProductReference } from '../sales.fixtures';
import { activeItemsValidator, apiErrorMessage, integerValidator, localDateTimeValidator, moneyValidator, toLocalDateTime } from '../sales.helpers';

@Component({
  selector: 'app-sale-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, PercentPipe],
  templateUrl: './sale-form.component.html',
  styleUrl: './sale-form.component.scss',
})
export class SaleFormComponent implements OnInit {
  readonly fb = inject(FormBuilder);
  private readonly sales = inject(SalesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = this.fb.nonNullable.group({
    saleDate: ['', [Validators.required, localDateTimeValidator]],
    customerId: ['', Validators.required],
    customerName: ['', [Validators.required, Validators.maxLength(200)]],
    branchId: ['', Validators.required],
    branchName: ['', [Validators.required, Validators.maxLength(200)]],
    items: this.fb.array([this.createItem()], { validators: activeItemsValidator }),
  });

  customers: readonly ExternalReference[] = CUSTOMER_FIXTURES;
  branches: readonly ExternalReference[] = BRANCH_FIXTURES;
  products: readonly ProductReference[] = PRODUCT_FIXTURES;
  saleId: string | null = null;
  savedSale: Sale | null = null;
  cancelledItems: SaleItem[] = [];
  loading = false;
  saving = false;
  loadFailed = false;
  error = '';

  get items() { return this.form.controls.items; }
  get isCancelled() { return this.savedSale?.isCancelled ?? false; }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map((params) => params.get('id')),
      distinctUntilChanged(),
      switchMap((id) => {
        this.reset(id);
        if (!id) return EMPTY;
        this.loading = true;
        return this.sales.getById(id).pipe(
          tap((response) => {
            if (!response.success || !response.data) {
              this.loadFailed = true;
              this.error = response.message || 'Unable to load this sale.';
              return;
            }
            this.populate(response.data);
          }),
          catchError((error: unknown) => {
            this.loadFailed = true;
            this.error = apiErrorMessage(error, 'Unable to load this sale. Check the API and try again.');
            return EMPTY;
          }),
          finalize(() => { this.loading = false; }),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  selectCustomer(id: string): void {
    this.form.patchValue({ customerId: id, customerName: this.customers.find((item) => item.id === id)?.name ?? '' });
  }

  selectBranch(id: string): void {
    this.form.patchValue({ branchId: id, branchName: this.branches.find((item) => item.id === id)?.name ?? '' });
  }

  selectProduct(index: number, id: string): void {
    const row = this.items.at(index);
    if (row.controls.id.value) return;
    const product = this.products.find((item) => item.id === id);
    row.patchValue({ productId: id, productName: product?.name ?? '', unitPrice: product?.unitPrice ?? 1 });
  }

  addItem(): void {
    if (this.loading || this.saving || this.loadFailed || this.isCancelled) return;
    this.items.push(this.createItem());
    this.items.markAsDirty();
  }

  removeItem(index: number): void {
    if (this.loading || this.saving || this.loadFailed || this.isCancelled) return;
    this.items.removeAt(index);
    this.items.markAsDirty();
  }

  invalid(control: AbstractControl): boolean {
    return control.invalid && (control.touched || control.dirty);
  }

  submit(): void {
    if (this.loading || this.saving || this.loadFailed || this.isCancelled) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const input: SaleInput = {
      saleDate: new Date(value.saleDate).toISOString(),
      customerId: value.customerId,
      customerName: value.customerName.trim(),
      branchId: value.branchId,
      branchName: value.branchName.trim(),
      items: value.items.map((item) => ({
        productId: item.productId, productName: item.productName.trim(),
        quantity: item.quantity, unitPrice: item.unitPrice,
      })),
    };
    const update: UpdateSaleInput = {
      ...input,
      items: input.items.map((item, index) => ({ ...item, id: value.items[index].id })),
    };

    this.error = '';
    this.saving = true;
    const request = this.saleId ? this.sales.update(this.saleId, update) : this.sales.create(input);
    request.pipe(
      finalize(() => { this.saving = false; }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (response) => {
        if (!response.success || !response.data) {
          this.error = response.message || 'The API did not return the saved sale.';
          return;
        }
        // Keep the server result and IDs even if navigation fails; retrying cannot create a duplicate sale.
        this.saleId = response.data.id;
        this.populate(response.data);
        void this.router.navigate(['/sales', response.data.id]).then((navigated) => {
          if (!navigated) this.error = 'Sale saved. Open its details using the link below.';
        }).catch(() => { this.error = 'Sale saved. Open its details using the link below.'; });
      },
      error: (error: unknown) => {
        this.error = apiErrorMessage(error, 'Unable to save the sale. Check the API and try again.');
      },
    });
  }

  private createItem(item?: SaleItem) {
    const row = this.fb.nonNullable.group({
      id: this.fb.control<string | null>(item?.id ?? null),
      productId: [item?.productId ?? '', Validators.required],
      productName: [item?.productName ?? '', [Validators.required, Validators.maxLength(200)]],
      quantity: [item?.quantity ?? 1, [Validators.required, Validators.min(1), Validators.max(20), integerValidator]],
      unitPrice: [item?.unitPrice ?? 1, [Validators.required, Validators.min(0.01), moneyValidator]],
    });
    // SaleItem.Update does not change ProductId. Replace a product by removing/adding a row.
    if (item) row.controls.productId.disable();
    return row;
  }

  private reset(id: string | null): void {
    this.saleId = id;
    this.savedSale = null;
    this.cancelledItems = [];
    this.customers = CUSTOMER_FIXTURES;
    this.branches = BRANCH_FIXTURES;
    this.products = PRODUCT_FIXTURES;
    this.error = '';
    this.loadFailed = false;
    this.form.reset();
    this.items.clear();
    this.items.push(this.createItem());
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private populate(sale: Sale): void {
    this.savedSale = sale;
    this.cancelledItems = sale.items.filter((item) => item.isCancelled);
    // Keep snapshots from sales whose external references are not in the demo fixtures.
    this.customers = this.includeReference(CUSTOMER_FIXTURES, { id: sale.customerId, name: sale.customerName });
    this.branches = this.includeReference(BRANCH_FIXTURES, { id: sale.branchId, name: sale.branchName });
    this.products = [...PRODUCT_FIXTURES];
    for (const item of sale.items.filter((item) => !item.isCancelled)) {
      this.products = this.includeReference(this.products, { id: item.productId, name: item.productName, unitPrice: item.unitPrice });
    }
    this.form.patchValue({
      saleDate: toLocalDateTime(sale.saleDate), customerId: sale.customerId,
      customerName: sale.customerName, branchId: sale.branchId, branchName: sale.branchName,
    });
    this.items.clear();
    for (const item of sale.items.filter((item) => !item.isCancelled)) this.items.push(this.createItem(item));
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private includeReference<T extends ExternalReference>(references: readonly T[], saved: T): readonly T[] {
    return [...references.filter((item) => item.id !== saved.id), saved];
  }
}
