namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Handler xoay key cho HttpClient "GroqClient".
    /// Groq dùng chuẩn OpenAI: header "Authorization: Bearer {key}" (gán đè nên không cần xoá key cũ).
    /// </summary>
    public class GroqKeyHandler : ApiKeyRotationHandler
    {
        public GroqKeyHandler(GroqKeyProvider provider) : base(provider) { }

        /// <inheritdoc />
        protected override void ApplyKey(HttpRequestMessage request, string key)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        }
    }
}
