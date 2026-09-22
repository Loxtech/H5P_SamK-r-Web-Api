import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { BookingService } from '../../../core/services/booking.service';
import { TripService } from '../../../core/services/trip.service';
import { AdminUser } from '../../../core/models/admin.models';
import { Trip } from '../../../core/models/trip.models';
import { Booking } from '../../../core/models/booking.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

type Tab = 'users' | 'trips' | 'bookings';

type PendingAction =
  | { type: 'deactivate-user'; user: AdminUser }
  | { type: 'delete-trip'; trip: Trip }
  | { type: 'delete-booking'; booking: Booking };

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [DatePipe, RouterLink, ConfirmDialogComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly bookingService = inject(BookingService);
  private readonly tripService = inject(TripService);

  readonly activeTab = signal<Tab>('users');

  readonly users = signal<AdminUser[]>([]);
  readonly trips = signal<Trip[]>([]);
  readonly bookings = signal<Booking[]>([]);

  readonly userSearchTerm = signal('');
  readonly filteredUsers = computed(() => {
    const term = this.userSearchTerm().trim().toLowerCase();
    if (!term) return this.users();

    return this.users().filter(
      (u) => u.fullName.toLowerCase().includes(term) || u.email.toLowerCase().includes(term),
    );
  });

  readonly tripSearchTerm = signal('');
  readonly filteredTrips = computed(() => {
    const term = this.tripSearchTerm().trim().toLowerCase();
    if (!term) return this.trips();

    return this.trips().filter(
      (t) =>
        t.fromCity.toLowerCase().includes(term) ||
        t.toCity.toLowerCase().includes(term) ||
        t.driverFullName.toLowerCase().includes(term),
    );
  });

  readonly bookingSearchTerm = signal('');
  readonly filteredBookings = computed(() => {
    const term = this.bookingSearchTerm().trim().toLowerCase();
    if (!term) return this.bookings();

    return this.bookings().filter(
      (b) =>
        b.fromCity.toLowerCase().includes(term) ||
        b.toCity.toLowerCase().includes(term) ||
        b.passengerFullName.toLowerCase().includes(term) ||
        b.status.toLowerCase().includes(term),
    );
  });

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly pendingAction = signal<PendingAction | null>(null);

  ngOnInit(): void {
    this.loadAll();
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  setTripSearch(value: string): void {
    this.tripSearchTerm.set(value);
  }

  setUserSearch(value: string): void {
    this.userSearchTerm.set(value);
  }

  setBookingSearch(value: string): void {
    this.bookingSearchTerm.set(value);
  }

  private loadAll(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      users: this.adminService.getUsers(),
      trips: this.adminService.getAllTrips(),
      bookings: this.adminService.getAllBookings(),
    }).subscribe({
      next: ({ users, trips, bookings }) => {
        this.users.set(users);
        this.trips.set(trips);
        this.bookings.set(bookings);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Kunne ikke hente admin-data.');
        this.isLoading.set(false);
      },
    });
  }

  // Reaktivering er ikke destruktivt, så den springer bekræft-dialogen over
  reactivateUser(user: AdminUser): void {
    this.actionError.set(null);
    this.adminService.reactivateUser(user.id).subscribe({
      next: () => this.loadAll(),
      error: () => this.actionError.set('Kunne ikke genaktivere brugeren.'),
    });
  }

  requestDeactivate(user: AdminUser): void {
    this.pendingAction.set({ type: 'deactivate-user', user });
  }

  requestDeleteTrip(trip: Trip): void {
    this.pendingAction.set({ type: 'delete-trip', trip });
  }

  requestDeleteBooking(booking: Booking): void {
    this.pendingAction.set({ type: 'delete-booking', booking });
  }

  get dialogOpen(): boolean {
    return this.pendingAction() !== null;
  }

  get dialogTitle(): string {
    switch (this.pendingAction()?.type) {
      case 'deactivate-user':
        return 'Deaktivér bruger';
      case 'delete-trip':
        return 'Slet tur';
      case 'delete-booking':
        return 'Slet booking';
      default:
        return '';
    }
  }

  get dialogMessage(): string {
    const action = this.pendingAction();
    if (!action) return '';

    switch (action.type) {
      case 'deactivate-user':
        return `Er du sikker på, at du vil deaktivere ${action.user.fullName}? Personen kan ikke længere logge ind.`;
      case 'delete-trip':
        return `Er du sikker på, at du vil slette turen ${action.trip.fromCity} → ${action.trip.toCity}? Alle tilknyttede bookinger og beskeder slettes også.`;
      case 'delete-booking':
        return 'Er du sikker på, at du vil slette denne booking?';
    }
  }

  confirmAction(): void {
    const action = this.pendingAction();
    this.pendingAction.set(null);
    if (!action) return;

    this.actionError.set(null);

    switch (action.type) {
      case 'deactivate-user':
        this.adminService.deactivateUser(action.user.id).subscribe({
          next: () => this.loadAll(),
          error: () => this.actionError.set('Kunne ikke deaktivere brugeren.'),
        });
        break;
      case 'delete-trip':
        this.tripService.delete(action.trip.id).subscribe({
          next: () => this.loadAll(),
          error: () => this.actionError.set('Kunne ikke slette turen.'),
        });
        break;
      case 'delete-booking':
        this.bookingService.delete(action.booking.id).subscribe({
          next: () => this.loadAll(),
          error: () => this.actionError.set('Kunne ikke slette bookingen.'),
        });
        break;
    }
  }

  dismissAction(): void {
    this.pendingAction.set(null);
  }
}