namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Bộ key Groq (Groq__ApiKeys__0..N hoặc Groq__ApiKey), dùng cho Whisper chuyển giọng nói thành chữ.
    /// </summary>
    public class GroqKeyProvider : ApiKeyRotationService
    {
        public GroqKeyProvider(IConfiguration config) : base(config, "Groq") { }
    }
}
