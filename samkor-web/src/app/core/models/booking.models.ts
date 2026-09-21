export interface Booking {
  id: string;
  tripId: string;
  fromCity: string;
  toCity: string;
  departureTime: string;
  passengerId: string;
  passengerFullName: string;
  status: string; // "Pending" | "Accepted" | "Rejected" | "Cancelled"
  createdAt: string;
}
