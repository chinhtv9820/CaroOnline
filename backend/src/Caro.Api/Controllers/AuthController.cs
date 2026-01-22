using Caro.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Controller quản lý các API liên quan đến authentication (đăng ký/đăng nhập)
/// </summary>
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// DTO cho request đăng ký
    /// </summary>
    public record RegisterRequest(string Username, string Email, string Password, string? DisplayName);

    /// <summary>
    /// DTO cho request đăng nhập
    /// </summary>
    public record LoginRequest(string UsernameOrEmail, string Password);

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="req">Registration request with username, email, password, and optional display name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User info and JWT token</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(req.Username))
                return BadRequest(new { message = "Username is required" });
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "Email is required" });
            if (string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Password is required" });

            var user = await _authService.RegisterAsync(req.Username, req.Email, req.Password, req.DisplayName, ct);
            var token = _authService.GenerateJwtToken(user);
            return Ok(new { user.Id, user.Username, user.Email, user.DisplayName, token });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log full exception for debugging
            Console.WriteLine($"Registration error: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return StatusCode(500, new { 
                message = "An error occurred during registration", 
                error = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="req">Login request with username/email and password</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User info and JWT token</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(req.UsernameOrEmail))
                return BadRequest(new { message = "Username or email is required" });
            if (string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Password is required" });

            var (user, token) = await _authService.LoginAsync(req.UsernameOrEmail, req.Password, ct);
            return Ok(new { user.Id, user.Username, user.Email, user.DisplayName, token });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
        }
    }
}


