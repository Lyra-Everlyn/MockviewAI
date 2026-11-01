using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using UglyToad.PdfPig;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV]
    /// </summary>
    
    public class CvExtractionService : ICvExtractionService
    {
        public async Task<string> ExtractAndCleanTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            string rawText = string.Empty;

            if (extension == ".pdf")
            {
                using var stream = file.OpenReadStream();
                using var document = PdfDocument.Open(stream);
                var textBuilder = new StringBuilder();
                foreach (var page in document.GetPages())
                {
                    textBuilder.Append(page.Text);
                }
                rawText = textBuilder.ToString();
            }
            else if (extension == ".jpg" || extension == ".png" || extension == ".jpeg")
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Lưu ý: Cần có thư mục 'tessdata' chứa file ngôn ngữ vie.traineddata, eng.traineddata
                using var engine = new TesseractEngine(@"./tessdata", "vie+eng", EngineMode.Default);
                using var img = Pix.LoadFromMemory(fileBytes);
                using var page = engine.Process(img);
                rawText = page.GetText();
            }

            return CleanText(rawText);
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var cleaned = Regex.Replace(input, @"\s+", " ");
            return cleaned.Trim();
        }
    }
}
