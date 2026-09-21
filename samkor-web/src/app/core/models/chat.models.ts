export interface ChatMessage {
  id: string;
  tripId: string;
  senderId: string;
  senderFullName: string;
  content: string;
  sentAt: string;
}
