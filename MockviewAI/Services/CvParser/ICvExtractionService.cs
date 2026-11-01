namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV]
    /// -- Dịch vụ tách và làm sạch data có trong CV người dùng
    /// </summary>
    public interface ICvExtractionService
    {
        Task<string> ExtractAndCleanTextAsync(IFormFile file);
    }
}
