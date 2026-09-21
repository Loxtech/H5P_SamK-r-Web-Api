import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TripService } from '../../../core/services/trip.service';
import { TripOverviewItem } from '../../../core/models/trip.models';

type Tab = 'planned' | 'completed';

@Component({
  selector: 'app-my-trips',
  standalone: true,
  imports: [DatePipe],
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

  ngOnInit(): void {
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
}
