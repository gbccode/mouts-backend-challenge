import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sales/new' },
  {
    path: 'sales/new',
    loadComponent: () => import('./features/sales/sale-form/sale-form.component').then((m) => m.SaleFormComponent),
  },
  {
    path: 'sales/:id/edit',
    loadComponent: () => import('./features/sales/sale-form/sale-form.component').then((m) => m.SaleFormComponent),
  },
  {
    path: 'sales/:id',
    loadComponent: () => import('./features/sales/sale-details/sale-details.component').then((m) => m.SaleDetailsComponent),
  },
  { path: '**', redirectTo: 'sales/new' },
];
