namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Handler xoay key cho HttpClient "GeminiClient".
    /// Key đi qua header "x-goog-api-key", KHÔNG đặt trên URL để tránh lộ trong log.
    /// </summary>
    public class GeminiKeyHandler : ApiKeyRotationHandler
    {
        private const string HeaderName = "x-goog-api-key";

        public GeminiKeyHandler(GeminiKeyProvider provider) : base(provider) { }

        /// <inheritdoc />
        protected override void ApplyKey(HttpRequestMessage request, string key)
        {
            request.Headers.Remove(HeaderName);
            request.Headers.Add(HeaderName, key);
        }
    }
}
