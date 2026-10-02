using AravindPortfolioMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AravindPortfolioMvc.Controllers;

public class GalleryController : Controller
{
    private readonly SupabasePortfolioService _portfolio;

    public GalleryController(SupabasePortfolioService portfolio)
    {
        _portfolio = portfolio;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Configured = _portfolio.IsConfigured;
        var items = await _portfolio.GetPublishedAsync();
        return View(items);
    }
}
