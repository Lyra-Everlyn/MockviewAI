namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [SWITCH API]
    /// -- HTTP Handler xoay vòng key cho HttpClient "GeminiClient".
    /// -- Gemini API gốc nhận key qua header "x-goog-api-key" (KHÔNG đặt key trên URL để tránh lộ trong log).
    /// </summary>
    public class GeminiKeyHandler : ApiKeyRotationHandler
    {
        private const string HeaderName = "x-goog-api-key";

        public GeminiKeyHandler(GeminiKeyProvider provider) : base(provider) { }

        #region Key strategy
        /// <inheritdoc />
        protected override void ApplyKey(HttpRequestMessage request, string key)
        {
            request.Headers.Remove(HeaderName);
            request.Headers.Add(HeaderName, key);
        }
        #endregion
    }
}
