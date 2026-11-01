using System.Net;

namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API]
    /// -- HTTP Handler ngầm can thiệp vào các request gửi đi. 
    /// -- Đổi Key và thử lại ngay lập tức khi gặp lỗi 4xx. Tạm nghỉ 1.5s và thử lại khi gặp lỗi 5xx.
    /// </summary>
    public class ApiKeyRotationHandler : DelegatingHandler
    {
        private readonly IApiKeyProvider _keyProvider;

        public ApiKeyRotationHandler(IApiKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var currentKey = _keyProvider.GetNextKey();
            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", $"Bearer {currentKey}");

            var response = await base.SendAsync(request, cancellationToken);
            int statusCode = (int)response.StatusCode;
            bool isKeyError = statusCode is 401 or 402 or 403 or 429;
            bool isServerError = statusCode >= 500 && statusCode < 600;

            if (isKeyError || isServerError)
            {
                if (isKeyError)
                {
                    _keyProvider.MarkKeyAsExhausted(currentKey);

                    var newKey = _keyProvider.GetNextKey();
                    request.Headers.Remove("Authorization");
                    request.Headers.Add("Authorization", $"Bearer {newKey}");
                }

                if (isServerError)
                {
                    await Task.Delay(1500, cancellationToken);
                }

                response = await base.SendAsync(request, cancellationToken);
            }

            return response;
        }
    }
}
