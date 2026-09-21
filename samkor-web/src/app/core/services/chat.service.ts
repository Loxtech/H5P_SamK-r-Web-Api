import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ChatMessage } from '../models/chat.models';

type ConnectionState = 'idle' | 'loading' | 'connected' | 'error';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  private connection?: signalR.HubConnection;
  private currentTripId?: string;

  private readonly messagesSignal = signal<ChatMessage[]>([]);
  readonly messages = this.messagesSignal.asReadonly();

  readonly connectionState = signal<ConnectionState>('idle');
  readonly errorMessage = signal<string | null>(null);

  // Henter historik og opretter SignalR-forbindelsen for en given tur.
  // Kaldes når chat-siden åbnes.
  async join(tripId: string): Promise<void> {
    this.currentTripId = tripId;
    this.connectionState.set('loading');
    this.errorMessage.set(null);
    this.messagesSignal.set([]);

    try {
      const history = await firstValueFrom(
        this.http.get<ChatMessage[]>(`${environment.apiBaseUrl}/api/trips/${tripId}/messages`),
      );
      this.messagesSignal.set(history);
    } catch {
      this.connectionState.set('error');
      this.errorMessage.set('Kunne ikke hente chatten. Har du adgang til denne tur?');
      return;
    }

    const token = this.authService.getToken();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/chat`, {
        accessTokenFactory: () => token ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.messagesSignal.update((current) => [...current, message]);
    });

    this.connection.onclose(() => {
      if (this.connectionState() === 'connected') {
        this.connectionState.set('idle');
      }
    });

    try {
      await this.connection.start();
      await this.connection.invoke('JoinTrip', tripId);
      this.connectionState.set('connected');
    } catch {
      this.connectionState.set('error');
      this.errorMessage.set('Kunne ikke forbinde til chatten i realtid.');
    }
  }

  async sendMessage(content: string): Promise<void> {
    if (!this.connection || !this.currentTripId) return;
    await this.connection.invoke('SendMessage', this.currentTripId, content);
  }

  // Kaldes når chat-siden lukkes/forlades
  async leave(): Promise<void> {
    if (this.connection && this.currentTripId) {
      try {
        await this.connection.invoke('LeaveTrip', this.currentTripId);
      } catch {
        // Forbindelsen kan allerede være lukket - ikke kritisk
      }
      await this.connection.stop();
    }

    this.connection = undefined;
    this.currentTripId = undefined;
    this.connectionState.set('idle');
  }
}
