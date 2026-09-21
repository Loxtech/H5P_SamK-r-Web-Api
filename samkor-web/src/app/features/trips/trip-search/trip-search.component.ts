import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TripService } from '../../../core/services/trip.service';
import { Trip } from '../../../core/models/trip.models';

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

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly results = signal<Trip[]>([]);
  readonly hasSearched = signal(false);

  readonly form = this.fb.group({
    from: [''],
    to: [''],
    date: [''],
  });

  ngOnInit(): void {
    // Viser alle kommende ture med det samme, uden at brugeren skal søge først
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
}
