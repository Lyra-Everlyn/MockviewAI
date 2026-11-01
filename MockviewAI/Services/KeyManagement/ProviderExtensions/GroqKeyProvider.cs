namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    public class GroqKeyProvider : ApiKeyRotationService
    {
        public GroqKeyProvider(IConfiguration config) : base(config, "Groq") { }
    }
}
