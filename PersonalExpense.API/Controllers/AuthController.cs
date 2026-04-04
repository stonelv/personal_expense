using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Interfaces;

namespace PersonalExpense.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto model)
    {
        if (model.Password != model.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match" });
        }

        var result = await _authService.RegisterAsync(model.Email, model.Password);

        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }

        return BadRequest(new { message = result.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto model)
    {
        var result = await _authService.LoginAsync(model.Email, model.Password);

        if (result.Success)
        {
            return Ok(new AuthResponseDto(
                Token: result.Token,
                Expiration: result.Expiration,
                Message: "Login successful"
            ));
        }

        return Unauthorized(new { message = "Invalid credentials" });
    }
}

