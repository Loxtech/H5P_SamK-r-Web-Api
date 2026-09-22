import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking } from '../models/booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Bookings`;

  // Krav 4 - anmode om en ledig plads
  create(tripId: string): Observable<Booking> {
    return this.http.post<Booking>(this.apiUrl, { tripId });
  }

  getMine(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/mine`);
  }

  getReceived(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/received`);
  }

  // Krav 5 - godkend/afvis
  accept(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/accept`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/reject`, {});
  }

  // Krav 8 - administrator kan fjerne en booking helt (moderation)
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
