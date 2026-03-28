using Microsoft.AspNetCore.Mvc;
using PersonalExpense.Api.DTOs.Auth;
using PersonalExpense.Api.Services;
using PersonalExpense.Domain.Entities;
using PersonalExpense.Infrastructure.Repositories;

namespace PersonalExpense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    
    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username) != null)
        {
            return BadRequest("Username is already taken");
        }
        
        if (await _userRepository.GetByEmailAsync(request.Email) != null)
        {
            return BadRequest("Email is already registered");
        }
        
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        
        var (token, expiration) = _tokenGenerator.GenerateToken(user);
        
        return Ok(new AuthResponse
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);
        
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid username/email or password");
        }
        
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();
        
        var (token, expiration) = _tokenGenerator.GenerateToken(user);
        
        return Ok(new AuthResponse
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }
}