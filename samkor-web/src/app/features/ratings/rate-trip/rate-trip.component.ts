import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { RatingService } from '../../../core/services/rating.service';
import { Participant } from '../../../core/models/rating.models';

interface RatingRowState {
  stars: number;
  comment: string;
  status: 'idle' | 'loading' | 'success' | 'error';
  message?: string;
}

@Component({
  selector: 'app-rate-trip',
  standalone: true,
  templateUrl: './rate-trip.component.html',
  styleUrl: './rate-trip.component.scss',
})
export class RateTripComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ratingService = inject(RatingService);

  private tripId = '';

  readonly participants = signal<Participant[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly rowState = signal<Record<string, RatingRowState>>({});

  readonly stars = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.tripId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.tripId) {
      this.errorMessage.set('Ugyldigt turlink.');
      this.isLoading.set(false);
      return;
    }

    forkJoin({
      participants: this.ratingService.getParticipants(this.tripId),
      myRatings: this.ratingService.getMyRatingsForTrip(this.tripId),
    }).subscribe({
      next: ({ participants, myRatings }) => {
        this.participants.set(participants);

        const initialState: Record<string, RatingRowState> = {};
        for (const p of participants) {
          const existing = myRatings.find((r) => r.rateeId === p.userId);
          initialState[p.userId] = existing
            ? {
                stars: existing.stars,
                comment: existing.comment ?? '',
                status: 'success',
                message: `Du gav ${existing.stars} stjerner.`,
              }
            : { stars: 0, comment: '', status: 'idle' };
        }
        this.rowState.set(initialState);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Kunne ikke hente deltagere for denne tur.');
        this.isLoading.set(false);
      },
    });
  }

  setStars(userId: string, stars: number): void {
    this.updateRow(userId, { stars });
  }

  setComment(userId: string, comment: string): void {
    this.updateRow(userId, { comment });
  }

  submit(participant: Participant): void {
    const row = this.rowState()[participant.userId];
    if (!row || row.stars < 1) {
      this.updateRow(participant.userId, { status: 'error', message: 'Vælg en stjernebedømmelse.' });
      return;
    }

    this.updateRow(participant.userId, { status: 'loading' });

    this.ratingService
      .create({
        tripId: this.tripId,
        rateeId: participant.userId,
        stars: row.stars,
        comment: row.comment || undefined,
      })
      .subscribe({
        next: () => {
          this.updateRow(participant.userId, { status: 'success', message: 'Bedømmelse sendt.' });
        },
        error: (err) => {
          let message = 'Der opstod en fejl. Prøv igen.';
          if (err.status === 400) message = 'Du kan ikke bedømme dig selv.';
          if (err.status === 409) message = 'Du har allerede bedømt denne person.';
          this.updateRow(participant.userId, { status: 'error', message });
        },
      });
  }

  private updateRow(userId: string, partial: Partial<RatingRowState>): void {
    this.rowState.update((current) => ({
      ...current,
      [userId]: { ...current[userId], ...partial } as RatingRowState,
    }));
  }
}
