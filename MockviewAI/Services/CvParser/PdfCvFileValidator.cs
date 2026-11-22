using Microsoft.Extensions.Options;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Chỉ nhận PDF: kiểm tra đuôi file và chữ ký "%PDF-" trong 1024 byte đầu.
    /// </summary>
    public class PdfCvFileValidator : ICvFileValidator
    {
        #region Fields
        private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
        private const int HeaderSearchBytes = 1024;

        private readonly CvOptions _options;
        private readonly ILogger<PdfCvFileValidator> _logger;
        #endregion

        #region Constructor
        public PdfCvFileValidator(IOptions<CvOptions> options, ILogger<PdfCvFileValidator> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ICvFileValidator
        public async Task<string?> ValidateAsync(IFormFile? file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return "Vui lòng chọn một file CV.";

            if (file.Length > _options.MaxFileBytes)
                return $"File CV tối đa {_options.MaxFileBytes / (1024 * 1024)} MB. Vui lòng chọn file nhỏ hơn.";

            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return "Chỉ hỗ trợ CV định dạng PDF.";

            // Một số trang (vd TopCV) xuất PDF có vài byte thừa trước "%PDF-"
            var head = new byte[(int)Math.Min(HeaderSearchBytes, file.Length)];
            int read;
            await using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
            }

            if (head.AsSpan(0, read).IndexOf(PdfSignature) < 0)
            {
                _logger.LogWarning("Rejected CV upload: no %PDF- header. ContentType={ContentType}, first bytes={Hex}",
                    file.ContentType, Convert.ToHexString(head.AsSpan(0, Math.Min(16, read))));
                return "File không phải PDF hợp lệ. Hãy xuất CV ra PDF rồi tải lên lại.";
            }

            return null;
        }
        #endregion
    }
}
