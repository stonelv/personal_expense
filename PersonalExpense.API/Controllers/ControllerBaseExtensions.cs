using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PersonalExpense.API.Controllers;

public static class ControllerBaseExtensions
{
    public static Guid? GetCurrentUserIdSafe(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || string.IsNullOrEmpty(claim.Value))
        {
            return null;
        }

        if (Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static Guid GetCurrentUserIdOrThrow(this ControllerBase controller)
    {
        var userId = controller.GetCurrentUserIdSafe();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated or has invalid claims");
        }
        return userId.Value;
    }
}
