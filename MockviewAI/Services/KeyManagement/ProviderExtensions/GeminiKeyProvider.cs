namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [SWITCH API]
    /// -- Provider quản lý danh sách Key chuyên biệt khởi tạo từ cấu hình "Gemini:ApiKeys".
    /// </summary>
    public class GeminiKeyProvider : ApiKeyRotationService
    {
        public GeminiKeyProvider(IConfiguration config) : base(config, "Gemini") { }
    }
}
