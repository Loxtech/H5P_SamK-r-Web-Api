namespace H5P_Samkør_Web_Api.Models.DTOs;

public record AuthResponse(
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    string FullName,
    string Email,
    IList<string> Roles
);
