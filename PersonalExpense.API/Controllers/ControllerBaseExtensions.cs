using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.Exceptions;

namespace PersonalExpense.API.Controllers;

public enum UserIdValidationError
{
    NoClaim,
    EmptyClaim,
    InvalidGuid
}

public static class ControllerBaseExtensions
{
    public static Guid? GetCurrentUserIdSafe(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(claim.Value))
        {
            return null;
        }

        if (Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static (Guid? UserId, UserIdValidationError? Error) GetCurrentUserIdWithDetails(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
        {
            return (null, UserIdValidationError.NoClaim);
        }

        if (string.IsNullOrEmpty(claim.Value))
        {
            return (null, UserIdValidationError.EmptyClaim);
        }

        if (Guid.TryParse(claim.Value, out var userId))
        {
            return (userId, null);
        }

        return (null, UserIdValidationError.InvalidGuid);
    }

    public static Guid GetCurrentUserIdOrThrow(this ControllerBase controller)
    {
        var (userId, error) = controller.GetCurrentUserIdWithDetails();
        if (!userId.HasValue)
        {
            var message = error switch
            {
                UserIdValidationError.NoClaim => "User is not authenticated: missing NameIdentifier claim",
                UserIdValidationError.EmptyClaim => "User is not authenticated: NameIdentifier claim is empty",
                UserIdValidationError.InvalidGuid => "User is not authenticated: NameIdentifier claim is not a valid Guid",
                _ => "User is not authenticated"
            };
            throw new UnauthorizedException(message);
        }
        return userId.Value;
    }
}
