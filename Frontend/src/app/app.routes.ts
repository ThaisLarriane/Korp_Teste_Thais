import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'notas' },
  {
    path: 'produtos',
    loadComponent: () => import('./features/produtos/produtos-page').then((m) => m.ProdutosPage),
  },
  {
    path: 'notas',
    loadComponent: () =>
      import('./features/notas/notas-list/notas-list-page').then((m) => m.NotasListPage),
  },
  {
    path: 'notas/:numero',
    loadComponent: () =>
      import('./features/notas/nota-detail/nota-detail-page').then((m) => m.NotaDetailPage),
  },
  { path: '**', redirectTo: 'notas' },
];
