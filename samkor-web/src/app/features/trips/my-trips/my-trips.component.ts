import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TripService } from '../../../core/services/trip.service';
import { TripOverviewItem } from '../../../core/models/trip.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

type Tab = 'planned' | 'completed';

interface CancelUiState {
  status: 'idle' | 'loading' | 'error';
  message?: string;
}

@Component({
  selector: 'app-my-trips',
  standalone: true,
  imports: [DatePipe, RouterLink, ConfirmDialogComponent],
  templateUrl: './my-trips.component.html',
  styleUrl: './my-trips.component.scss',
})
export class MyTripsComponent implements OnInit {
  private readonly tripService = inject(TripService);

  readonly activeTab = signal<Tab>('planned');
  readonly planned = signal<TripOverviewItem[]>([]);
  readonly completed = signal<TripOverviewItem[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly cancelState = signal<Record<string, CancelUiState>>({});

  // Turen der er "på vej til at blive aflyst", mens brugeren bekræfter i dialogen
  readonly pendingCancelItem = signal<TripOverviewItem | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.tripService.getMyOverview().subscribe({
      next: (overview) => {
        this.planned.set(overview.planned);
        this.completed.set(overview.completed);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Kunne ikke hente dine ture lige nu.');
        this.isLoading.set(false);
      },
    });
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  cancelTrip(item: TripOverviewItem): void {
    this.pendingCancelItem.set(item);
  }

  confirmCancel(): void {
    const item = this.pendingCancelItem();
    if (!item) return;

    this.pendingCancelItem.set(null);
    this.setCancelState(item.tripId, { status: 'loading' });

    this.tripService.delete(item.tripId).subscribe({
      next: () => this.load(),
      error: (err) => {
        const message =
          err.status === 409
            ? 'Turen er fuldt booket og kan ikke aflyses.'
            : 'Kunne ikke aflyse turen.';
        this.setCancelState(item.tripId, { status: 'error', message });
      },
    });
  }

  dismissCancel(): void {
    this.pendingCancelItem.set(null);
  }

  private setCancelState(tripId: string, state: CancelUiState): void {
    this.cancelState.update((current) => ({ ...current, [tripId]: state }));
  }
}
