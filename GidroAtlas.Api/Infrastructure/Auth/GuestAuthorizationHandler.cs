using GidroAtlas.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace GidroAtlas.Api.Infrastructure.Auth;

/// <summary>
/// Authorization handler that allows Guest access for unauthenticated users
/// </summary>
public class GuestAuthorizationHandler : AuthorizationHandler<GuestRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GuestRequirement requirement)
    {
        // If user is authenticated and has Guest or Expert role, allow
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (context.User.IsInRole(Roles.Guest) || context.User.IsInRole(Roles.Expert))
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            // If user is not authenticated, allow as Guest
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement for Guest access
/// </summary>
public class GuestRequirement : IAuthorizationRequirement
{
}
