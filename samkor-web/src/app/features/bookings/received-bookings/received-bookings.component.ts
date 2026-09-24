import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { Booking } from '../../../core/models/booking.models';

interface ActionUiState {
  status: 'idle' | 'loading' | 'error';
  message?: string;
}

@Component({
  selector: 'app-received-bookings',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './received-bookings.component.html',
  styleUrl: './received-bookings.component.scss',
})
export class ReceivedBookingsComponent implements OnInit {
  private readonly bookingService = inject(BookingService);

  readonly bookings = signal<Booking[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly actionState = signal<Record<string, ActionUiState>>({});

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.bookingService.getReceived().subscribe({
      next: (bookings) => {
        this.bookings.set(bookings);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Kunne ikke hente anmodninger lige nu.');
        this.isLoading.set(false);
      },
    });
  }

  accept(booking: Booking): void {
    this.setAction(booking.id, { status: 'loading' });

    this.bookingService.accept(booking.id).subscribe({
      next: () => this.load(),
      error: (err) => {
        const message =
          err.status === 409
            ? 'Turen blev opdateret samtidig af en anden anmodning. Prøv igen.'
            : 'Kunne ikke godkende anmodningen.';
        this.setAction(booking.id, { status: 'error', message });
      },
    });
  }

  reject(booking: Booking): void {
    this.setAction(booking.id, { status: 'loading' });

    this.bookingService.reject(booking.id).subscribe({
      next: () => this.load(),
      error: () => {
        this.setAction(booking.id, { status: 'error', message: 'Kunne ikke afvise anmodningen.' });
      },
    });
  }

  private setAction(id: string, state: ActionUiState): void {
    this.actionState.update((current) => ({ ...current, [id]: state }));
  }
}