using System.Text.Json;

namespace MockviewAI.Services.Security
{
    public class TurnstileService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public TurnstileService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _secretKey = config["Turnstile:SecretKey"] ?? throw new InvalidOperationException("Turnstile SecretKey is missing.");
        }

        /// <summary>
        /// Gọi API Cloudflare để kiểm tra tính hợp lệ của Token.
        /// </summary>
        /// <param name="token">Mã token gửi từ Frontend (cf-turnstile-response).</param>
        /// <param name="ipAddress">IP của người dùng (Cloudflare khuyến nghị truyền vào để tăng độ chính xác).</param>
        /// <returns>True nếu là người thật, False nếu là Bot hoặc Token không hợp lệ.</returns>
        public async Task<bool> VerifyAsync(string token, string? ipAddress)
        {
            // Nếu không có token gửi lên -> Chắc chắn là Bot gọi API trực tiếp không qua trình duyệt
            if (string.IsNullOrWhiteSpace(token)) return false;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _secretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", ipAddress ?? string.Empty)
            });

            var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);

            if (!response.IsSuccessStatusCode) return false;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);

            return document.RootElement.GetProperty("success").GetBoolean();
        }
    }
}
