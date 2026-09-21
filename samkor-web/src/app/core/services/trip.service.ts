import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Trip, TripOverviewResponse, TripSearchParams } from '../models/trip.models';

@Injectable({ providedIn: 'root' })
export class TripService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Trips`;

  search(params: TripSearchParams): Observable<Trip[]> {
    let httpParams = new HttpParams();
    if (params.from) httpParams = httpParams.set('from', params.from);
    if (params.to) httpParams = httpParams.set('to', params.to);
    if (params.date) httpParams = httpParams.set('date', params.date);

    return this.http.get<Trip[]>(this.apiUrl, { params: httpParams });
  }

  getById(id: string): Observable<Trip> {
    return this.http.get<Trip>(`${this.apiUrl}/${id}`);
  }

  // Krav 7 - samlet oversigt over egne ture (chauffør + passager)
  getMyOverview(): Observable<TripOverviewResponse> {
    return this.http.get<TripOverviewResponse>(`${this.apiUrl}/mine`);
  }
}
