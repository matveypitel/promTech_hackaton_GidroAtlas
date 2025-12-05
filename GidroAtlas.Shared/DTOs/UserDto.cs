using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

public class UserDto
{
    public required string Login { get; set; }

    public Role Role { get; set; }
}
