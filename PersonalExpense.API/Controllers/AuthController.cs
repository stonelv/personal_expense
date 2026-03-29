using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Application.DTOs.Auth;
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
    public async Task<IActionResult> Register(RegisterDto model)
    {
        var result = await _authService.RegisterAsync(model);

        if (result.Succeeded)
        {
            return Ok(new { message = "User registered successfully" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        var result = await _authService.LoginAsync(model);

        if (result.Succeeded)
        {
            return Ok(result.Result);
        }

        return Unauthorized(new { message = result.Message });
    }
}
