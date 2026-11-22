namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Tách chữ có sẵn trong file PDF (không OCR).
    /// </summary>
    public interface ICvExtractionService
    {
        /// <summary>Chuỗi rỗng hoặc rất ngắn = PDF scan.</summary>
        string ExtractText(byte[] pdf);
    }
}
