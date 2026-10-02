using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using AravindPortfolioMvc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<SupabasePortfolioService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "__Host-AravindPortfolioAdmin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Portfolio-Version"] = "cinematic-v2";
    context.Response.Headers["Cache-Control"] = context.Request.Path.StartsWithSegments("/css") || context.Request.Path.StartsWithSegments("/js") ? "public,max-age=3600" : "no-cache,no-store,must-revalidate";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    var supabaseUrl = builder.Configuration["SUPABASE_URL"];
    var supabaseOrigin = string.IsNullOrWhiteSpace(supabaseUrl) ? "" : new Uri(supabaseUrl).GetLeftPart(UriPartial.Authority);
    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'self'; img-src 'self' data: blob: {supabaseOrigin}; media-src 'self' blob: {supabaseOrigin}; " +
        "style-src 'self' 'unsafe-inline'; script-src 'self'; font-src 'self'; connect-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/robots.txt", (HttpContext context) =>
{
    var root = $"{context.Request.Scheme}://{context.Request.Host}";
    var text = $"User-agent: *\nAllow: /\nDisallow: /Admin\nSitemap: {root}/sitemap.xml\n";
    return Results.Text(text, "text/plain");
});

app.MapGet("/sitemap.xml", (HttpContext context) =>
{
    var root = $"{context.Request.Scheme}://{context.Request.Host}";
    var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
              $"<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
              $"  <url><loc>{root}/</loc><changefreq>weekly</changefreq><priority>1.0</priority></url>\n" +
              $"  <url><loc>{root}/Gallery</loc><changefreq>weekly</changefreq><priority>0.8</priority></url>\n" +
              "</urlset>";
    return Results.Text(xml, "application/xml");
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();
