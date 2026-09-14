namespace H5P_Samkør_Web_Api.Models.DTOs;

public record TripResponse(
    Guid Id,
    Guid DriverId,
    string DriverFullName,
    string FromCity,
    string ToCity,
    DateTime DepartureTime,
    int AvailableSeats,
    decimal PricePerSeat,
    Guid? VehicleId
);
