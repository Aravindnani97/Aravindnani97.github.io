using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AravindPortfolioMvc.Models;

namespace AravindPortfolioMvc.Services;

public class SupabasePortfolioService
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _publishableKey;
    private readonly string _secretKey;
    private readonly string _adminEmail;
    private readonly string _bucket;

    public SupabasePortfolioService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _url = (config["SUPABASE_URL"] ?? "").TrimEnd('/');
        _publishableKey = config["SUPABASE_PUBLISHABLE_KEY"] ?? "";
        _secretKey = config["SUPABASE_SECRET_KEY"] ?? "";
        _adminEmail = config["ADMIN_EMAIL"] ?? "";
        _bucket = config["SUPABASE_BUCKET"] ?? "portfolio";
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_url) &&
        !string.IsNullOrWhiteSpace(_publishableKey) &&
        !string.IsNullOrWhiteSpace(_secretKey) &&
        !string.IsNullOrWhiteSpace(_adminEmail);

    public async Task<bool> AuthenticateAdminAsync(string email, string password)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return false;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_url}/auth/v1/token?grant_type=password");

        request.Headers.TryAddWithoutValidation("apikey", _publishableKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { email, password }),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!doc.RootElement.TryGetProperty("user", out var user)) return false;
        if (!user.TryGetProperty("email", out var emailNode)) return false;

        var authenticatedEmail = emailNode.GetString();
        return string.Equals(authenticatedEmail, _adminEmail, StringComparison.OrdinalIgnoreCase);
    }

    public Task<List<PortfolioMedia>> GetPublishedAsync() =>
        QueryAsync("published=eq.true&order=sort_order.desc,created_at.desc");

    public Task<List<PortfolioMedia>> GetAllAsync() =>
        QueryAsync("order=sort_order.desc,created_at.desc");

    private async Task<List<PortfolioMedia>> QueryAsync(string query)
    {
        if (!IsConfigured) return new List<PortfolioMedia>();

        using var request = CreateSecretRequest(
            HttpMethod.Get,
            $"{_url}/rest/v1/portfolio_media?select=*&{query}");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<List<PortfolioMedia>>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<PortfolioMedia>();
    }

    public async Task<PortfolioMedia> UploadAsync(
        IFormFile file,
        string title,
        string? caption,
        string? category,
        bool published)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Supabase is not configured.");

        if (file.Length <= 0)
            throw new InvalidOperationException("Choose a file to upload.");

        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        if (!isImage && !isVideo)
            throw new InvalidOperationException("Only image and video files are allowed.");

        var maxBytes = isImage ? 20L * 1024 * 1024 : 150L * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new InvalidOperationException(isImage
                ? "Images must be 20 MB or smaller."
                : "Videos must be 150 MB or smaller.");

        var allowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif" };
        var allowedVideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".webm", ".mov", ".m4v" };

        var extension = Path.GetExtension(file.FileName);
        if ((isImage && !allowedImageExtensions.Contains(extension)) ||
            (isVideo && !allowedVideoExtensions.Contains(extension)))
            throw new InvalidOperationException("That file extension is not allowed.");

        var mediaType = isImage ? "image" : "video";
        var filePath = $"uploads/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var encodedPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));

        using (var upload = CreateSecretRequest(
            HttpMethod.Post,
            $"{_url}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{encodedPath}"))
        {
            upload.Headers.TryAddWithoutValidation("x-upsert", "false");

            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            streamContent.Headers.ContentLength = file.Length;
            upload.Content = streamContent;

            using var uploadResponse = await _http.SendAsync(upload);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var error = await uploadResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Upload failed: {error}");
            }
        }

        var publicUrl =
            $"{_url}/storage/v1/object/public/{Uri.EscapeDataString(_bucket)}/{encodedPath}";

        var payload = new
        {
            file_path = filePath,
            public_url = publicUrl,
            media_type = mediaType,
            title = string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(file.FileName)
                : title.Trim(),
            caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            published,
            sort_order = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        using var insert = CreateSecretRequest(
            HttpMethod.Post,
            $"{_url}/rest/v1/portfolio_media");
        insert.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        insert.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var insertResponse = await _http.SendAsync(insert);
        if (!insertResponse.IsSuccessStatusCode)
        {
            await DeleteStorageObjectAsync(filePath);
            var error = await insertResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Metadata save failed: {error}");
        }

        var inserted = JsonSerializer.Deserialize<List<PortfolioMedia>>(
            await insertResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return inserted?.FirstOrDefault()
            ?? throw new InvalidOperationException("Upload completed but metadata could not be read.");
    }

    public async Task SetPublishedAsync(long id, bool published)
    {
        using var request = CreateSecretRequest(
            HttpMethod.Patch,
            $"{_url}/rest/v1/portfolio_media?id=eq.{id}");
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { published }),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(long id)
    {
        var item = await GetByIdAsync(id);
        if (item is null) return;

        await DeleteStorageObjectAsync(item.FilePath);

        using var request = CreateSecretRequest(
            HttpMethod.Delete,
            $"{_url}/rest/v1/portfolio_media?id=eq.{id}");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<PortfolioMedia?> GetByIdAsync(long id)
    {
        using var request = CreateSecretRequest(
            HttpMethod.Get,
            $"{_url}/rest/v1/portfolio_media?select=*&id=eq.{id}&limit=1");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var items = JsonSerializer.Deserialize<List<PortfolioMedia>>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return items?.FirstOrDefault();
    }

    private async Task DeleteStorageObjectAsync(string filePath)
    {
        var encodedPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));

        using var request = CreateSecretRequest(
            HttpMethod.Delete,
            $"{_url}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{encodedPath}");

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateSecretRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", _secretKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        return request;
    }
}
