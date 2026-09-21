export interface Trip {
  id: string;
  driverId: string;
  driverFullName: string;
  fromCity: string;
  toCity: string;
  departureTime: string;
  availableSeats: number;
  pricePerSeat: number;
  vehicleId: string | null;
}

export interface CreateTripRequest {
  fromCity: string;
  toCity: string;
  departureTime: string;
  availableSeats: number;
  pricePerSeat: number;
  vehicleId: string | null;
}

export interface TripSearchParams {
  from?: string;
  to?: string;
  departureAfter?: string; // ISO-datetime-streng
}

export interface TripOverviewItem {
  tripId: string;
  fromCity: string;
  toCity: string;
  departureTime: string;
  role: string;
  status: string;
  isCompleted: boolean;
}

export interface TripOverviewResponse {
  planned: TripOverviewItem[];
  completed: TripOverviewItem[];
}
