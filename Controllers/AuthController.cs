using System.Security.Cryptography;
using System.Text;
using GLMS_CORE_APP.API.DTOs;
using GLMS_CORE_APP.API.Services;
using GLMS_CORE_APP.Shared.Data;
using GLMS_CORE_APP.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS_CORE_APP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(
    GlmsDbContext db,
    IJwtService jwtService,
    ILogger<AuthController> logger) : ControllerBase
{
    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), 200)]
    [ProducesResponseType(typeof(ApiErrorDto), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var hash = HashPassword(dto.Password);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email
                                   && u.PasswordHash == hash
                                   && u.IsActive);

        if (user is null)
        {
            logger.LogWarning("Failed login attempt for {Email}", dto.Email);
            return Unauthorized(new ApiErrorDto("Invalid email or password."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token   = jwtService.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(8);

        logger.LogInformation("User {Email} authenticated via API", user.Email);

        return Ok(new LoginResponseDto(token, user.FullName, user.Email,
            user.Role.ToString(), expires));
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponseDto), 201)]
    [ProducesResponseType(typeof(ApiErrorDto), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict(new ApiErrorDto("An account with this email already exists."));

        var user = new User
        {
            FullName     = dto.FullName,
            Email        = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Role         = dto.Role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token   = jwtService.GenerateToken(user);
        var expires = DateTime.UtcNow.AddHours(8);

        logger.LogInformation("New user registered via API: {Email}", user.Email);

        return CreatedAtAction(nameof(Login), new LoginResponseDto(
            token, user.FullName, user.Email, user.Role.ToString(), expires));
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
