namespace H5P_Samkør_Web_Api.Models.DTOs;

public record MessageResponse(
    Guid Id,
    Guid TripId,
    Guid SenderId,
    string SenderFullName,
    string Content,
    DateTime SentAt
);