import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TripService } from '../../../core/services/trip.service';
import { BookingService } from '../../../core/services/booking.service';
import { AuthService } from '../../../core/services/auth.service';
import { Trip } from '../../../core/models/trip.models';

interface BookingUiState {
  status: 'idle' | 'loading' | 'success' | 'error';
  message?: string;
}

@Component({
  selector: 'app-trip-search',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './trip-search.component.html',
  styleUrl: './trip-search.component.scss',
})
export class TripSearchComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tripService = inject(TripService);
  private readonly bookingService = inject(BookingService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly results = signal<Trip[]>([]);
  readonly hasSearched = signal(false);
  readonly bookingState = signal<Record<string, BookingUiState>>({});

  readonly form = this.fb.group({
    from: [''],
    to: [''],
    date: [''],
  });

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { from, to, date } = this.form.getRawValue();

    this.tripService
      .search({ from: from || undefined, to: to || undefined, date: date || undefined })
      .subscribe({
        next: (trips) => {
          this.results.set(trips);
          this.isLoading.set(false);
          this.hasSearched.set(true);
        },
        error: () => {
          this.errorMessage.set('Kunne ikke hente ture lige nu. Prøv igen.');
          this.isLoading.set(false);
        },
      });
  }

  requestBooking(trip: Trip): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    this.setBookingState(trip.id, { status: 'loading' });

    this.bookingService.create(trip.id).subscribe({
      next: () => {
        this.setBookingState(trip.id, { status: 'success', message: 'Anmodning sendt.' });
      },
      error: (err) => {
        let message = 'Der opstod en fejl. Prøv igen.';
        if (err.status === 400) message = 'Du kan ikke booke din egen tur.';
        if (err.status === 409) message = 'Du har allerede en anmodning på denne tur.';
        this.setBookingState(trip.id, { status: 'error', message });
      },
    });
  }

  private setBookingState(tripId: string, state: BookingUiState): void {
    this.bookingState.update((current) => ({ ...current, [tripId]: state }));
  }
}
