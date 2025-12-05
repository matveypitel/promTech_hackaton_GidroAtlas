using GidroAtlas.Shared.DTOs;

namespace GidroAtlas.Web.Interfaces;

/// <summary>
/// Service for authentication operations with the API
/// </summary>
public interface IAuthApiService : IApiClient
{
    /// <summary>
    /// Authenticates user with login credentials
    /// </summary>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    
    /// <summary>
    /// Logs out the current user
    /// </summary>
    Task<bool> LogoutAsync();
    
    /// <summary>
    /// Gets current user information
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync();
}
