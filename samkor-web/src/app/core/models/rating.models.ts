export interface Participant {
  userId: string;
  fullName: string;
  role: string;
}

export interface CreateRatingRequest {
  tripId: string;
  rateeId: string;
  stars: number;
  comment?: string;
}

export interface Rating {
  id: string;
  tripId: string;
  raterId: string;
  raterFullName: string;
  rateeId: string;
  rateeFullName: string;
  stars: number;
  comment?: string;
  createdAt: string;
}
