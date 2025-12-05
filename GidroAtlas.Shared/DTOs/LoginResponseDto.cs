using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required string Login { get; set; }
    public Role Role { get; set; }
    public DateTime ExpiresAt { get; set; }
}
