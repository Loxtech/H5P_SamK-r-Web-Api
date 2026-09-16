namespace H5P_Samkør_Web_Api.Models.DTOs;

public record RatingResponse(
    Guid Id,
    Guid TripId,
    Guid RaterId,
    string RaterFullName,
    Guid RateeId,
    string RateeFullName,
    int Stars,
    string? Comment,
    DateTime CreatedAt
);
