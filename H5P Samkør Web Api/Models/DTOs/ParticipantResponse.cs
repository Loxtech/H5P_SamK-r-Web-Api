namespace H5P_Samkør_Web_Api.Models.DTOs;

public record ParticipantResponse(
    Guid UserId,
    string FullName,
    string Role // "Chauffør" eller "Passager"
);
