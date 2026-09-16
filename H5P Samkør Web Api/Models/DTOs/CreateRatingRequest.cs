using System.ComponentModel.DataAnnotations;

namespace H5P_Samkør_Web_Api.Models.DTOs;

public record CreateRatingRequest(
    [Required] Guid TripId,
    [Required] Guid RateeId,
    [Required, Range(1, 5)] int Stars,
    [MaxLength(500)] string? Comment
);
