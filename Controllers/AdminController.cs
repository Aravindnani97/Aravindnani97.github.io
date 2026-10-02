using System.Security.Claims;
using AravindPortfolioMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AravindPortfolioMvc.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly SupabasePortfolioService _portfolio;

    public AdminController(SupabasePortfolioService portfolio)
    {
        _portfolio = portfolio;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index));

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Configured = _portfolio.IsConfigured;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (!await _portfolio.AuthenticateAdminAsync(email, password))
        {
            ModelState.AddModelError("", "Invalid admin sign-in.");
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Configured = _portfolio.IsConfigured;
            return View();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return LocalRedirect(
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index), "Admin")!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Configured = _portfolio.IsConfigured;
        var items = await _portfolio.GetAllAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 157286400)]
    [RequestSizeLimit(157286400)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        string title,
        string? caption,
        string? category,
        bool published = true)
    {
        try
        {
            await _portfolio.UploadAsync(file, title, caption, category, published);
            TempData["Success"] = "Media uploaded successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPublished(long id, bool published)
    {
        await _portfolio.SetPublishedAsync(id, published);
        TempData["Success"] = published ? "Media published." : "Media hidden.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _portfolio.DeleteAsync(id);
        TempData["Success"] = "Media deleted.";
        return RedirectToAction(nameof(Index));
    }
}
