using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Services.Exceptions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Đọc chữ trong PDF bằng PdfPig (không cần thư viện native, chạy được trên Docker Linux).
    /// </summary>
    public class CvExtractionService : ICvExtractionService
    {
        #region Fields
        private static readonly Regex InlineSpaces = new(@"[ \t ]+", RegexOptions.Compiled);
        private static readonly Regex ManyBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

        private readonly CvOptions _options;
        private readonly ILogger<CvExtractionService> _logger;
        #endregion

        #region Constructor
        public CvExtractionService(IOptions<CvOptions> options, ILogger<CvExtractionService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ICvExtractionService
        public string ExtractText(byte[] pdf)
        {
            try
            {
                using var document = PdfDocument.Open(pdf);
                var builder = new StringBuilder();

                foreach (var page in document.GetPages().Take(Math.Max(1, _options.MaxPages)))
                {
                    builder.AppendLine(ContentOrderTextExtractor.GetText(page));
                    builder.AppendLine();
                }

                return Clean(builder.ToString());
            }
            catch (PdfDocumentEncryptedException ex)
            {
                throw new CvException("CV đang được đặt mật khẩu. Hãy bỏ mật khẩu rồi tải lên lại.", ex);
            }
            catch (Exception ex) when (ex is not CvException)
            {
                _logger.LogWarning(ex, "Could not read CV PDF ({Bytes} bytes)", pdf.Length);
                throw new CvException("Không đọc được file PDF này. Hãy xuất lại CV ra PDF rồi thử lại.", ex);
            }
        }
        #endregion

        #region Helpers
        private static string Clean(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var lines = input.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(line => InlineSpaces.Replace(line, " ").Trim());

            return ManyBlankLines.Replace(string.Join("\n", lines), "\n\n").Trim();
        }
        #endregion
    }
}
