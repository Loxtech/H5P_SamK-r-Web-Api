import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

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
  {
    path: 'trips',
    loadComponent: () =>
      import('./features/trips/trip-search/trip-search.component').then(
        (m) => m.TripSearchComponent,
      ),
  },
  {
    path: 'trips/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/trips/create-trip/create-trip.component').then(
        (m) => m.CreateTripComponent,
      ),
  },
  {
    path: 'my-trips',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/trips/my-trips/my-trips.component').then((m) => m.MyTripsComponent),
  },
  {
    path: 'bookings/received',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/bookings/received-bookings/received-bookings.component').then(
        (m) => m.ReceivedBookingsComponent,
      ),
  },
];
