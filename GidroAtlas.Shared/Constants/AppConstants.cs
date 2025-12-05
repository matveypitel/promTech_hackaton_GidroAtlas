namespace GidroAtlas.Shared.Constants;

/// <summary>
/// General application constants.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Database connection string name.
    /// </summary>
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>
    /// CORS Policy Name.
    /// </summary>
    public const string CorsPolicyName = "GidroAtlasWebPolicy";

    public static class Jwt
    {
        public const string SectionName = "JwtSettings";
        public const string SecretKey = "SecretKey";
        public const string Issuer = "Issuer";
        public const string Audience = "Audience";
    }

    public static class Cors
    {
        public const string SectionName = "CorsSettings";
        public const string AllowedOrigins = "AllowedOrigins";
    }

    public static class Routes
    {
        public const string WaterObjects = "api/WaterObjects";
        public const string Regions = "regions";
        public const string Priorities = "priorities";
        public const string PrioritiesSummary = "priorities/summary";
    }

    public static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
    }

    public static class ErrorMessages
    {
        public const string WaterObjectNotFound = "Water object not found";
        public const string LoginPasswordRequired = "Login and password are required";
        public const string InvalidLoginOrPassword = "Invalid login or password";
    }
}
