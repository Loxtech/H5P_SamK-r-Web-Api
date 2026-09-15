namespace H5P_Samkør_Web_Api.Models.DTOs;

public record BookingResponse(
    Guid Id,
    Guid TripId,
    string FromCity,
    string ToCity,
    DateTime DepartureTime,
    Guid PassengerId,
    string PassengerFullName,
    string Status,
    DateTime CreatedAt
);
