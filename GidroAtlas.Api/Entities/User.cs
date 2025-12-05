using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Api.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string Login { get; set; }

    public required string PasswordHash { get; set; }

    public Role Role { get; set; }
}
