import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  effect,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { TripService } from '../../../core/services/trip.service';
import { ChatMessage } from '../../../core/models/chat.models';
import { Trip } from '../../../core/models/trip.models';

@Component({
  selector: 'app-trip-chat',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './trip-chat.component.html',
  styleUrl: './trip-chat.component.scss',
})
export class TripChatComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly tripService = inject(TripService);
  readonly chatService = inject(ChatService);
  readonly authService = inject(AuthService);

  private tripId = '';
  readonly draft = signal('');
  readonly trip = signal<Trip | null>(null);

  @ViewChild('scrollAnchor') scrollAnchor?: ElementRef<HTMLDivElement>;

  constructor() {
    // Ruller ned til seneste besked, hver gang listen ændrer sig
    effect(() => {
      this.chatService.messages();
      queueMicrotask(() =>
        this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' }),
      );
    });
  }

  ngOnInit(): void {
    this.tripId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.tripId) {
      this.chatService.join(this.tripId);
      this.tripService.getById(this.tripId).subscribe({
        next: (trip) => this.trip.set(trip),
        error: () => {
          // Titlen falder tilbage til "Chat" - ikke kritisk for selve chatfunktionen
        },
      });
    }
  }

  ngOnDestroy(): void {
    this.chatService.leave();
  }

  setDraft(value: string): void {
    this.draft.set(value);
  }

  send(): void {
    const content = this.draft().trim();
    if (!content) return;

    this.chatService.sendMessage(content);
    this.draft.set('');
  }

  isOwnMessage(message: ChatMessage): boolean {
    return message.senderId === this.authService.currentUser()?.userId;
  }
}