using System.ComponentModel.DataAnnotations;

namespace H5P_Samkør_Web_Api.Models.DTOs;

public record RegisterRequest(
    [Required, MaxLength(100)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);
