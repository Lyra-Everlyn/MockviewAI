using MockviewAI.Models.DTOs;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Phân tích CV và chốt CV sau khi người dùng kiểm tra.
    /// </summary>
    public class CvService : ICvService
    {
        #region Fields
        private readonly ICvExtractionService _extractor;
        private readonly ICvParsingService _parser;
        private readonly ICvStore _store;
        private readonly ILogger<CvService> _logger;
        #endregion

        #region Constructor
        public CvService(ICvExtractionService extractor, ICvParsingService parser, ICvStore store, ILogger<CvService> logger)
        {
            _extractor = extractor;
            _parser = parser;
            _store = store;
            _logger = logger;
        }
        #endregion

        #region ICvService
        public async Task<CvParsedDataDto> AnalyzeAsync(string email, IFormFile file, CancellationToken ct = default)
        {
            EnsureEmail(email);

            byte[] pdf;
            using (var buffer = new MemoryStream((int)file.Length))
            {
                await file.CopyToAsync(buffer, ct);
                pdf = buffer.ToArray();
            }

            var data = await _parser.ParseAsync(pdf, _extractor.ExtractText(pdf), ct);
            if (CvDataSanitizer.IsEmpty(data))
                throw new CvException("Không tìm thấy thông tin trong file này. Hãy kiểm tra lại đây có phải CV của bạn không.");

            await _store.SavePendingAsync(email, data, ct);
            _logger.LogInformation("CV analyzed: {Skills} skills, {Jobs} work experiences, {Schools} educations",
                data.HardSkills.Count, data.WorkExperiences.Count, data.Educations.Count);
            return data;
        }

        public async Task<CvParsedDataDto?> ConfirmAsync(string email, CvParsedDataDto edited, string? hardSkillsCsv, CancellationToken ct = default)
        {
            EnsureEmail(email);

            var pending = await _store.GetPendingAsync(email, ct);
            if (pending == null) return null;

            // Chỉ nhận các trường có trên form; phần còn lại lấy từ bản chờ phía server
            pending.FullName = edited?.FullName ?? string.Empty;
            pending.TargetJobTitle = edited?.TargetJobTitle ?? string.Empty;
            pending.CareerObjective = edited?.CareerObjective ?? string.Empty;
            pending.HardSkills = CvDataSanitizer.SplitSkills(hardSkillsCsv);

            await _store.RemovePendingAsync(email, ct);
            return CvDataSanitizer.Sanitize(pending);
        }
        #endregion

        #region Helpers
        private static void EnsureEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new CvException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }
        #endregion
    }
}
