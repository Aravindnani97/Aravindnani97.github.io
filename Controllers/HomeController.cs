using AravindPortfolioMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AravindPortfolioMvc.Controllers;

public class HomeController : Controller
{
    private readonly SupabasePortfolioService _portfolio;

    public HomeController(SupabasePortfolioService portfolio)
    {
        _portfolio = portfolio;
    }

    public async Task<IActionResult> Index()
    {
        var published = await _portfolio.GetPublishedAsync();
        return View(published.Take(4).ToList());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
