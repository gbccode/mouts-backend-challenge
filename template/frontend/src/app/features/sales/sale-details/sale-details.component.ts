import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, EMPTY, finalize, map, switchMap, tap } from 'rxjs';
import { Sale } from '../../../core/models/sale.model';
import { SalesService } from '../../../core/services/sales.service';
import { apiErrorMessage } from '../sales.helpers';

@Component({
  selector: 'app-sale-details',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, PercentPipe],
  templateUrl: './sale-details.component.html',
  styleUrl: './sale-details.component.scss',
})
export class SaleDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly sales = inject(SalesService);
  private readonly destroyRef = inject(DestroyRef);
  sale: Sale | null = null;
  loading = false;
  error = '';

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map((params) => params.get('id')),
      distinctUntilChanged(),
      switchMap((id) => {
        this.sale = null;
        this.error = '';
        if (!id) {
          this.error = 'No sale ID was provided.';
          return EMPTY;
        }
        this.loading = true;
        // Always read the persisted aggregate, including after save and a browser refresh.
        return this.sales.getById(id).pipe(
          tap((response) => {
            if (response.success && response.data) this.sale = response.data;
            else this.error = response.message || 'Unable to load the saved sale.';
          }),
          catchError((error: unknown) => {
            this.error = apiErrorMessage(error, 'Unable to load the saved sale. Check the API and refresh.');
            return EMPTY;
          }),
          finalize(() => { this.loading = false; }),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
