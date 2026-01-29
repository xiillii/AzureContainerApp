using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using TasksApi.Services;

namespace TasksApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Username}", request.Username);

        var response = await _authService.LoginAsync(request);
        
        if (response == null)
        {
            _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "TasksApi" });
    }
}
