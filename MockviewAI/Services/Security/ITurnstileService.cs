namespace MockviewAI.Services.Security
{
    public interface ITurnstileService
    {
        // Call the Cloudflare API to verify the validity of the token
        Task<bool> VerifyAsync(string token, string? ipAddress);
    }
}
