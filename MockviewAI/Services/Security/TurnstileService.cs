using System.Text.Json;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Gọi API siteverify của Cloudflare Turnstile.
    /// </summary>
    public class TurnstileService : ITurnstileService
    {
        #region Fields
        private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly ILogger<TurnstileService> _logger;
        #endregion

        #region Constructor
        public TurnstileService(HttpClient httpClient, IConfiguration config, ILogger<TurnstileService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _secretKey = config["Turnstile:SecretKey"] ?? throw new InvalidOperationException("Turnstile SecretKey is missing.");
        }
        #endregion

        #region ITurnstileService
        public async Task<bool> VerifyAsync(string token, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _secretKey,
                ["response"] = token,
                ["remoteip"] = ipAddress ?? string.Empty
            });

            try
            {
                using var response = await _httpClient.PostAsync(VerifyUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Turnstile verify returned HTTP {Status}", (int)response.StatusCode);
                    return false;
                }

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return document.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Turnstile verify failed");
                return false;
            }
        }
        #endregion
    }
}
