namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [SWITCH API]
    /// -- HTTP Handler gắn luồng xử lý đổi Key tự động chuyên biệt cho HttpClient của Gemini.
    /// </summary>
    public class GeminiKeyHandler : ApiKeyRotationHandler
    {
        public GeminiKeyHandler(GeminiKeyProvider provider) : base(provider) { }
    }
}
