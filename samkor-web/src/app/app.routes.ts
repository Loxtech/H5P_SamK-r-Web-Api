import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'trips', pathMatch: 'full' },

  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },

  // Tilføjes i de kommende skridt:
  // { path: 'trips', loadComponent: () => import('./features/trips/trip-search/trip-search.component').then(m => m.TripSearchComponent) },
  // { path: 'my-trips', canActivate: [authGuard], loadComponent: () => ... },
];
