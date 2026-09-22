namespace H5P_Samkør_Web_Api.Models.DTOs;

public record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    IList<string> Roles,
    bool IsLockedOut
);
