namespace FinanceTracker.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Models.ViewModels;
using FinanceTracker.Web.Services.Interfaces;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUserRepository userRepository, ILogger<AccountController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>Renders the login page.</summary>
    /// <param name="returnUrl">Optional URL to redirect to after successful login.</param>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>Authenticates the user with username and password.</summary>
    /// <remarks>On success, sets a persistent authentication cookie valid for 7 days.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userRepository.GetByUserNameAsync(model.UserName);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user {UserName}", model.UserName);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        _logger.LogInformation("User {UserName} logged in successfully", user.UserName);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new("DisplayName", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    /// <summary>Renders the registration page.</summary>
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new RegisterViewModel());
    }

    /// <summary>Creates a new user account.</summary>
    /// <remarks>Usernames must be unique. Passwords are hashed with BCrypt.</remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            UserName = model.UserName,
            DisplayName = model.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        var created = await _userRepository.CreateAsync(user);
        if (!created)
        {
            _logger.LogWarning("Registration failed: username {UserName} already taken", model.UserName);
            ModelState.AddModelError("UserName", "Username is already taken.");
            return View(model);
        }

        _logger.LogInformation("New user registered: {UserName}", model.UserName);

        TempData["SuccessMessage"] = "Registration successful! Please log in.";
        return RedirectToAction("Login");
    }

    /// <summary>Signs the current user out and clears the session cookie.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User {UserName} logged out", User.Identity?.Name);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
