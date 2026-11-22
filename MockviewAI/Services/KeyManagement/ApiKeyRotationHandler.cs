namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API] 
    /// Handler gắn API key cho mọi request của một HttpClient và xử lý thử lại:
    /// lỗi do key (401, 402, 403, 429) -> phạt key, đổi key gửi lại 1 lần; lỗi máy chủ (5xx) -> chờ 1,5 giây gửi lại 1 lần.
    /// 403 do bị chặn mạng / vùng không phải lỗi key: trả về ngay, không phạt key.
    /// Nội dung request phải đọc lại được (ByteArrayContent, StringContent, JsonContent)
    /// </summary>
    public abstract class ApiKeyRotationHandler : DelegatingHandler
    {
        #region Fields
        private const int ServerErrorRetryDelayMs = 1500;

        // Groq / Cloudflare trả 403 với câu này khi chặn theo mạng, VPN hoặc quốc gia
        private static readonly string[] NetworkBlockMarkers = { "network settings", "access denied", "unsupported_country", "not available in your", "location is not supported" };

        private readonly IApiKeyProvider _keyProvider;
        #endregion

        #region Constructor
        protected ApiKeyRotationHandler(IApiKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
        }
        #endregion

        #region Key strategy
        /// <summary>Gắn key theo chuẩn của nhà cung cấp; phải xoá key cũ trước vì request có thể được gửi lại.</summary>
        protected abstract void ApplyKey(HttpRequestMessage request, string key);
        #endregion

        #region Pipeline
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_keyProvider.HasKeys)
                return await base.SendAsync(request, cancellationToken);

            var currentKey = _keyProvider.GetNextKey();
            ApplyKey(request, currentKey);

            var response = await base.SendAsync(request, cancellationToken);
            int statusCode = (int)response.StatusCode;
            bool isServerError = statusCode is >= 500 and < 600;
            bool isKeyError = statusCode is 401 or 402 or 429
                              || (statusCode == 403 && !IsNetworkBlock(await ReadBodyAsync(response, cancellationToken)));

            if (!isKeyError && !isServerError)
                return response;

            if (isKeyError)
            {
                _keyProvider.MarkKeyAsExhausted(currentKey);
                if (_keyProvider.KeyCount < 2) return response;
                ApplyKey(request, _keyProvider.GetNextKey());
            }
            else
            {
                await Task.Delay(ServerErrorRetryDelayMs, cancellationToken);
            }

            response.Dispose();
            return await base.SendAsync(request, cancellationToken);
        }
        #endregion

        #region Helpers
        /// <summary>true khi body lỗi cho thấy bị chặn theo mạng / vùng, không phải key sai. Dùng chung cho các service gọi API.</summary>
        public static bool IsNetworkBlock(string? body)
        {
            if (string.IsNullOrEmpty(body)) return false;
            if (body.Contains("api key", StringComparison.OrdinalIgnoreCase) || body.Contains("api_key", StringComparison.OrdinalIgnoreCase))
                return false;
            return NetworkBlockMarkers.Any(m => body.Contains(m, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Đọc body và giữ trong bộ đệm, để service phía trên vẫn đọc lại được.</summary>
        private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                await response.Content.LoadIntoBufferAsync();
                return await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        #endregion
    }
}
