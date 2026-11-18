namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API]
    /// -- HTTP Handler ngầm gắn API Key vào mọi request gửi đi của một HttpClient.
    /// -- Lỗi do key (401, 402, 403, 429): đánh dấu key bị phạt, đổi key khác và thử lại 1 lần.
    /// -- Lỗi máy chủ (5xx): chờ 1.5 giây rồi thử lại 1 lần.
    /// -- Mỗi nhà cung cấp gắn key theo cách riêng (Bearer, x-goog-api-key...) qua <see cref="ApplyKey"/> (Open/Closed).
    /// LƯU Ý: request được gửi lại nguyên vẹn, nên nội dung phải đọc lại được
    /// (ByteArrayContent, StringContent, JsonContent...) — KHÔNG dùng StreamContent.
    /// </summary>
    public abstract class ApiKeyRotationHandler : DelegatingHandler
    {
        private const int ServerErrorRetryDelayMs = 1500;
        private readonly IApiKeyProvider _keyProvider;

        protected ApiKeyRotationHandler(IApiKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
        }

        #region Key strategy
        /// <summary>
        /// Gắn API Key vào request theo đúng chuẩn của nhà cung cấp.
        /// Phải XOÁ key cũ (nếu có) trước khi gắn key mới, vì request có thể được gửi lại.
        /// </summary>
        protected abstract void ApplyKey(HttpRequestMessage request, string key);
        #endregion

        #region Pipeline
        /// <summary>
        /// Gắn key, gửi request và xử lý thử lại theo mã lỗi.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Không có key: gửi nguyên request, để dịch vụ phía trên kiểm tra HasKeys và báo lỗi cấu hình
            if (!_keyProvider.HasKeys)
                return await base.SendAsync(request, cancellationToken);

            var currentKey = _keyProvider.GetNextKey();
            ApplyKey(request, currentKey);

            var response = await base.SendAsync(request, cancellationToken);
            int statusCode = (int)response.StatusCode;
            bool isKeyError = statusCode is 401 or 402 or 403 or 429;
            bool isServerError = statusCode is >= 500 and < 600;

            if (!isKeyError && !isServerError)
                return response;

            if (isKeyError)
            {
                _keyProvider.MarkKeyAsExhausted(currentKey);

                // Chỉ có 1 key: thử lại cũng sẽ lỗi y hệt -> trả kết quả ngay
                if (_keyProvider.KeyCount < 2)
                    return response;

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
    }
}
