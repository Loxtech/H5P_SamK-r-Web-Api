namespace H5P_Samkør_Web_Api.Models.DTOs;

public record TripOverviewItem(
    Guid TripId,
    string FromCity,
    string ToCity,
    DateTime DepartureTime,
    string Role,   // "Chauffør" eller "Passager"
    string Status, // "Oprettet" (som chauffør) eller booking-status (som passager)
    bool IsCompleted
);
