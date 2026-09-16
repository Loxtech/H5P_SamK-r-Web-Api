namespace H5P_Samkør_Web_Api.Models.DTOs;

public record UserRatingSummary(
    Guid UserId,
    string FullName,
    double AverageRating,
    int RatingCount
);
