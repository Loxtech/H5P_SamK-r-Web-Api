import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TripService } from '../../../core/services/trip.service';
import { FlatpickrDirective } from '../../../shared/directives/flatpickr.directive';

@Component({
  selector: 'app-create-trip',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, FlatpickrDirective],
  templateUrl: './create-trip.component.html',
  styleUrl: './create-trip.component.scss',
})
export class CreateTripComponent {
  private readonly fb = inject(FormBuilder);
  private readonly tripService = inject(TripService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    fromCity: ['', [Validators.required, Validators.maxLength(100)]],
    toCity: ['', [Validators.required, Validators.maxLength(100)]],
    departureTime: ['', [Validators.required]],
    availableSeats: [3, [Validators.required, Validators.min(1), Validators.max(8)]],
    pricePerSeat: [0, [Validators.required, Validators.min(0)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { fromCity, toCity, departureTime, availableSeats, pricePerSeat } =
      this.form.getRawValue();

    this.tripService
      .create({
        fromCity: fromCity!,
        toCity: toCity!,
        departureTime: departureTime!,
        availableSeats: availableSeats!,
        pricePerSeat: pricePerSeat!,
        vehicleId: null,
      })
      .subscribe({
        next: () => this.router.navigate(['/my-trips']),
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(
            err.status === 400
              ? 'Afgangstidspunktet skal ligge i fremtiden.'
              : 'Der opstod en fejl. Prøv igen.',
          );
        },
      });
  }
}
