using System.ComponentModel.DataAnnotations;

namespace H5P_Samkør_Web_Api.Models.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
