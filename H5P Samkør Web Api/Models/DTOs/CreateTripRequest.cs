using System.ComponentModel.DataAnnotations;

namespace H5P_Samkør_Web_Api.Models.DTOs;

public record CreateTripRequest(
    [Required, MaxLength(100)] string FromCity,
    [Required, MaxLength(100)] string ToCity,
    [Required] DateTime DepartureTime,
    [Range(1, 8)] int AvailableSeats,
    [Range(0, 10000)] decimal PricePerSeat,
    Guid? VehicleId
);
