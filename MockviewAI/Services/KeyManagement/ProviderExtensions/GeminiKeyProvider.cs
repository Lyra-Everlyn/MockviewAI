namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Bộ key Gemini (Gemini__ApiKeys__0..N hoặc Gemini__ApiKey). Dùng chung cho chấm điểm, CV, STT chuẩn hoá, nhà tuyển dụng.
    /// </summary>
    public class GeminiKeyProvider : ApiKeyRotationService
    {
        public GeminiKeyProvider(IConfiguration config) : base(config, "Gemini") { }
    }
}
