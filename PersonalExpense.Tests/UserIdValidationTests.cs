using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PersonalExpense.API.Controllers;
using PersonalExpense.Application.Exceptions;

namespace PersonalExpense.Tests;

public class UserIdValidationTests
{
    #region GetCurrentUserIdSafe Tests

    [Fact]
    public void GetCurrentUserIdSafe_WithValidClaim_ShouldReturnGuid()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, expectedUserId.ToString());

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserIdSafe_WithoutNameIdentifierClaim_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.Email, "test@example.com");

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserIdSafe_WithEmptyClaimValue_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, string.Empty);

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserIdSafe_WithNullClaimValue_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, null!);

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserIdSafe_WithInvalidGuid_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, "not-a-valid-guid");

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserIdSafe_WithNoClaims_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithNoClaims();

        // Act
        var result = controller.GetCurrentUserIdSafe();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCurrentUserIdWithDetails Tests

    [Fact]
    public void GetCurrentUserIdWithDetails_WithValidClaim_ShouldReturnGuidAndNoError()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, expectedUserId.ToString());

        // Act
        var (userId, error) = controller.GetCurrentUserIdWithDetails();

        // Assert
        userId.Should().NotBeNull();
        userId.Should().Be(expectedUserId);
        error.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserIdWithDetails_WithoutClaim_ShouldReturnNoClaimError()
    {
        // Arrange
        var controller = CreateControllerWithNoClaims();

        // Act
        var (userId, error) = controller.GetCurrentUserIdWithDetails();

        // Assert
        userId.Should().BeNull();
        error.Should().Be(UserIdValidationError.NoClaim);
    }

    [Fact]
    public void GetCurrentUserIdWithDetails_WithEmptyClaim_ShouldReturnEmptyClaimError()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, string.Empty);

        // Act
        var (userId, error) = controller.GetCurrentUserIdWithDetails();

        // Assert
        userId.Should().BeNull();
        error.Should().Be(UserIdValidationError.EmptyClaim);
    }

    [Fact]
    public void GetCurrentUserIdWithDetails_WithInvalidGuid_ShouldReturnInvalidGuidError()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, "not-a-valid-guid");

        // Act
        var (userId, error) = controller.GetCurrentUserIdWithDetails();

        // Assert
        userId.Should().BeNull();
        error.Should().Be(UserIdValidationError.InvalidGuid);
    }

    #endregion

    #region GetCurrentUserIdOrThrow Tests

    [Fact]
    public void GetCurrentUserIdOrThrow_WithValidClaim_ShouldReturnGuid()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, expectedUserId.ToString());

        // Act
        var result = controller.GetCurrentUserIdOrThrow();

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserIdOrThrow_WithoutClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var controller = CreateControllerWithNoClaims();

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedException>(
            () => controller.GetCurrentUserIdOrThrow());

        exception.Message.Should().Contain("missing NameIdentifier claim");
        exception.StatusCode.Should().Be(401);
        exception.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void GetCurrentUserIdOrThrow_WithEmptyClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, string.Empty);

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedException>(
            () => controller.GetCurrentUserIdOrThrow());

        exception.Message.Should().Contain("NameIdentifier claim is empty");
        exception.StatusCode.Should().Be(401);
        exception.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void GetCurrentUserIdOrThrow_WithInvalidGuid_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var controller = CreateControllerWithClaim(ClaimTypes.NameIdentifier, "not-a-valid-guid");

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedException>(
            () => controller.GetCurrentUserIdOrThrow());

        exception.Message.Should().Contain("not a valid Guid");
        exception.StatusCode.Should().Be(401);
        exception.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void GetCurrentUserIdOrThrow_ShouldNotThrowUnauthorizedAccessException()
    {
        // Arrange
        var controller = CreateControllerWithNoClaims();

        // Act & Assert
        var exception = Record.Exception(() => controller.GetCurrentUserIdOrThrow());
        
        exception.Should().NotBeNull();
        exception.Should().BeOfType<UnauthorizedException>();
        exception.Should().NotBeOfType<UnauthorizedAccessException>();
    }

    #endregion

    #region Helper Methods

    private static ControllerBase CreateControllerWithClaim(string claimType, string? claimValue)
    {
        var claims = new List<Claim>();
        if (claimValue != null)
        {
            claims.Add(new Claim(claimType, claimValue));
        }
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var controller = new MockController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            }
        };

        return controller;
    }

    private static ControllerBase CreateControllerWithNoClaims()
    {
        var identity = new ClaimsIdentity();
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var controller = new MockController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            }
        };

        return controller;
    }

    private class MockController : ControllerBase
    {
    }

    #endregion
}
