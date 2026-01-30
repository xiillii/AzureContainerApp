using Microsoft.AspNetCore.Mvc;
using TasksWeb.Services;

namespace TasksWeb.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (_authService.IsAuthenticated)
        {
            return RedirectToAction("Index", "Tasks");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.ErrorMessage = "Username and password are required";
            return View();
        }

        try
        {
            var success = await _authService.LoginAsync(username, password);
            
            if (success)
            {
                return RedirectToAction("Index", "Tasks");
            }
            
            ViewBag.ErrorMessage = "Invalid username or password";
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            ViewBag.ErrorMessage = $"Login failed: {ex.Message}";
            return View();
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _authService.Logout();
        return RedirectToAction("Login");
    }
}
