namespace MockviewAI.Services.KeyManagement.ProviderExtensions
{
    public class GroqKeyHandler : ApiKeyRotationHandler
    {
        public GroqKeyHandler(GroqKeyProvider provider) : base(provider) { }
    }
}
