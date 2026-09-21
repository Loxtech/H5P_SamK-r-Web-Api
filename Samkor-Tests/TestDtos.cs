namespace SamKor.Api.Tests;

internal record AuthResponseDto(
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    string FullName,
    string Email,
    List<string> Roles
);

internal record TripDto(
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

internal record BookingDto(
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
