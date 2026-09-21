import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateTripRequest,
  Trip,
  TripOverviewResponse,
  TripSearchParams,
} from '../models/trip.models';

@Injectable({ providedIn: 'root' })
export class TripService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Trips`;

  search(params: TripSearchParams): Observable<Trip[]> {
    let httpParams = new HttpParams();
    if (params.from) httpParams = httpParams.set('from', params.from);
    if (params.to) httpParams = httpParams.set('to', params.to);
    if (params.departureAfter) httpParams = httpParams.set('departureAfter', params.departureAfter);

    return this.http.get<Trip[]>(this.apiUrl, { params: httpParams });
  }

  getById(id: string): Observable<Trip> {
    return this.http.get<Trip>(`${this.apiUrl}/${id}`);
  }

  // Krav 7 - samlet oversigt over egne ture (chauffør + passager)
  getMyOverview(): Observable<TripOverviewResponse> {
    return this.http.get<TripOverviewResponse>(`${this.apiUrl}/mine`);
  }

  // Krav 2 - oprettelse af tur
  create(request: CreateTripRequest): Observable<Trip> {
    return this.http.post<Trip>(this.apiUrl, request);
  }

  // Krav 2 - aflysning af egen tur (kun muligt hvis ikke fuldt booket)
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
