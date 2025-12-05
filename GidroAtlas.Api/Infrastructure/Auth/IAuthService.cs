using GidroAtlas.Shared.DTOs;
using System.Security.Claims;

namespace GidroAtlas.Api.Infrastructure.Auth;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<bool> ValidateTokenAsync(string token);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}
