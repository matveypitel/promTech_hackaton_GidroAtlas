namespace GidroAtlas.Web.Interfaces;

/// <summary>
/// Base interface for API client services
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Sets the authentication token for API requests
    /// </summary>
    void SetAuthToken(string token);
    
    /// <summary>
    /// Clears the authentication token
    /// </summary>
    void ClearAuthToken();
}
