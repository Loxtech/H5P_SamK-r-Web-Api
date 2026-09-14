using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using H5P_Samkør_Web_Api.Models;
using H5P_Samkør_Web_Api.Models.DTOs;
using H5P_Samkør_Web_Api.Services;

namespace H5P_Samkør_Web_Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(UserManager<User> userManager, ITokenService tokenService, IConfiguration config)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _config = config;
    }

    // Krav 1 - Brugeroprettelse: rollen "User" tildeles automatisk
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Conflict("En bruger med denne e-mail findes allerede.");

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "User");

        return Ok(await BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized("Forkert e-mail eller adgangskode.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized("Forkert e-mail eller adgangskode.");

        return Ok(await BuildAuthResponse(user));
    }

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.CreateToken(user, roles);
        var expiresInMinutes = double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60");

        return new AuthResponse(
            token,
            DateTime.UtcNow.AddMinutes(expiresInMinutes),
            user.FullName,
            user.Email!,
            roles
        );
    }
}
