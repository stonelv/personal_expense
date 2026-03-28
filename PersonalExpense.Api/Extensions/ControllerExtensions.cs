using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PersonalExpense.Api.Extensions;

public static class ControllerExtensions
{
    public static int GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }
}