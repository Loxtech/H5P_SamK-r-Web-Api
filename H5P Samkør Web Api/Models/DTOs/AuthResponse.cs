namespace H5P_Samkør_Web_Api.Models.DTOs;

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string FullName,
    string Email,
    IList<string> Roles
);
