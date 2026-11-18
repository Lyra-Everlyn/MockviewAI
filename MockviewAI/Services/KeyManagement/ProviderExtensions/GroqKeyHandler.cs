namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [SWITCH API]
    /// -- HTTP Handler xoay vòng key cho HttpClient "GroqClient".
    /// -- Groq dùng chuẩn OpenAI: header "Authorization: Bearer {key}".
    /// </summary>
    public class GroqKeyHandler : ApiKeyRotationHandler
    {
        public GroqKeyHandler(GroqKeyProvider provider) : base(provider) { }

        #region Key strategy
        /// <inheritdoc />
        protected override void ApplyKey(HttpRequestMessage request, string key)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        }
        #endregion
    }
}
