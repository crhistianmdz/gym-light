using System.Security.Claims;

namespace GymFlow.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                    ?? user.FindFirst("sub");

        if (claim is null || !Guid.TryParse(claim.Value, out var id))
            throw new InvalidOperationException("User ID claim not found or invalid.");

        return id;
    }
}