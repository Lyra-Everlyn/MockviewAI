// [QUESTION-BANK] Không tin AI hoàn toàn (giống cách ép điểm 0..25 ở GeminiResponseParser).
// 4 nhãn GHI NHẬN CÁCH TIẾP CẬN (không phải thang điểm):
//   dung          = đi đúng công thức thông thường (đường chuẩn), trình bày bình thường
//   hoi_dac_biet  = CÙNG công thức nhưng cách làm / giải thích khác thường (đường chuẩn trình bày lạ, hoặc đường vòng đã biết)
//   dac_biet      = KHÔNG dùng công thức trong bộ đề, có hướng đi riêng nhưng vẫn đúng bản chất (ngoai_bo)
//   sai           = không đúng bản chất
// Quy tắc an toàn:
//   - Câu không có trong buổi / không có trong bộ đề / bị trùng -> bỏ. Mã ý cốt lõi lạ -> bỏ.
//   - AI nói "sai" hoặc khớp một lỗi hay gặp -> "sai" (thà khắt khe còn hơn chấm đúng nhầm).
//   - Đường chuẩn: giữ "dung" hoặc "hoi_dac_biet" theo AI ("dac_biet" -> "hoi_dac_biet" vì vẫn cùng công thức).
//   - Đường vòng đã biết -> "hoi_dac_biet".
//   - Mã đường lạ / ngoai_bo -> "dac_biet".
//   - Không phải "sai" mà đạt ít hơn MinCorePoints ý -> "sai".

using Microsoft.Extensions.Logging;
using MockviewAI.Models.Ai;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    public class AnswerVerdictValidator : IAnswerVerdictValidator
    {
        private const int MaxFeedbackLength = 500;
        private const string StandardPathCategory = AnswerCategory.Correct;   // đường chuẩn trong bộ đề có Category = dung

        private readonly IQuestionBankStore _store;
        private readonly ILogger<AnswerVerdictValidator> _logger;

        public AnswerVerdictValidator(IQuestionBankStore store, ILogger<AnswerVerdictValidator> logger)
        {
            _store = store;
            _logger = logger;
        }

        public List<AnswerVerdict> Normalize(IEnumerable<AnswerVerdict> rawVerdicts, IReadOnlyCollection<string> askedQuestionIds)
        {
            var result = new List<AnswerVerdict>();
            foreach (var raw in rawVerdicts)
            {
                var question = _store.Find(raw.QuestionId);
                if (question == null || !askedQuestionIds.Contains(raw.QuestionId) || result.Any(r => r.QuestionId == raw.QuestionId))
                {
                    _logger.LogWarning("AI verdict ignored for unknown/duplicate question {QuestionId}", raw.QuestionId);
                    continue;
                }

                var coreIds = question.CorePoints.Select(c => c.Id).ToList();
                var met = raw.MetCorePoints.Where(coreIds.Contains).Distinct().ToList();
                string aiCategory = (raw.Category ?? string.Empty).Trim().ToLowerInvariant();
                string pathId = (raw.MatchedPath ?? string.Empty).Trim();

                var path = question.Paths.FirstOrDefault(p => p.Id == pathId);
                bool isMistake = question.Mistakes.Any(m => m.Id == pathId);
                if (path == null && !isMistake) pathId = AnswerCategory.OutOfBank;

                string category;
                if (aiCategory == AnswerCategory.Wrong || isMistake)
                {
                    category = AnswerCategory.Wrong;
                }
                else if (path != null)
                {
                    bool isStandardPath = path.Category == StandardPathCategory;
                    category = isStandardPath && aiCategory == AnswerCategory.Correct
                        ? AnswerCategory.Correct
                        : AnswerCategory.AlternativePath;   // đường chuẩn trình bày khác thường, hoặc đường vòng đã biết
                    if (isStandardPath && !AnswerCategory.IsValid(aiCategory))
                        category = AnswerCategory.Correct;   // AI không ghi loại hợp lệ -> theo đường chuẩn
                    if (met.Count == 0) met = path.CoversCorePoints.Where(coreIds.Contains).ToList();   // AI quên liệt kê ý
                }
                else
                {
                    category = AnswerCategory.Special;
                }

                // Chưa tới điểm B
                if (category != AnswerCategory.Wrong && met.Count > 0 && met.Count < question.MinCorePoints)
                    category = AnswerCategory.Wrong;

                if (category != aiCategory)
                    _logger.LogWarning("AI verdict for {QuestionId} changed from '{AiCategory}' to '{Category}' (path {Path}, met {Met})",
                        raw.QuestionId, aiCategory, category, pathId, met.Count);

                var feedback = (raw.Feedback ?? string.Empty).Trim();
                result.Add(new AnswerVerdict
                {
                    QuestionId = raw.QuestionId,
                    Category = category,
                    MatchedPath = pathId,
                    MetCorePoints = met,
                    MissingCorePoints = coreIds.Where(id => !met.Contains(id)).ToList(),
                    Feedback = feedback.Length > MaxFeedbackLength ? feedback[..MaxFeedbackLength] : feedback
                });
            }
            return result;
        }
    }
}
