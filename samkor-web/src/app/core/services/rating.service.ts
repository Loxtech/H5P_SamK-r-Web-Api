import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateRatingRequest, Participant, Rating } from '../models/rating.models';

@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly http = inject(HttpClient);
  private readonly tripsUrl = `${environment.apiBaseUrl}/api/Trips`;
  private readonly ratingsUrl = `${environment.apiBaseUrl}/api/Ratings`;

  getParticipants(tripId: string): Observable<Participant[]> {
    return this.http.get<Participant[]>(`${this.tripsUrl}/${tripId}/participants`);
  }

  getMyRatingsForTrip(tripId: string): Observable<Rating[]> {
    return this.http.get<Rating[]>(`${this.ratingsUrl}/trip/${tripId}/mine`);
  }

  create(request: CreateRatingRequest): Observable<Rating> {
    return this.http.post<Rating>(this.ratingsUrl, request);
  }
}
