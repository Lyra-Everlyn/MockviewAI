using Microsoft.Extensions.Logging;
using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Không tin AI hoàn toàn: sửa lại nhãn AI trả về theo bộ đề (giống cách ép điểm 0..25 ở GeminiResponseParser).
    /// </summary>
    /// <remarks>
    /// 4 nhãn ghi nhận CÁCH TIẾP CẬN, không phải thang điểm:
    /// <list type="bullet">
    /// <item>dung: đi đường chuẩn, trình bày bình thường.</item>
    /// <item>hoi_dac_biet: cùng công thức nhưng cách làm / giải thích khác thường (đường chuẩn trình bày lạ, hoặc đường vòng đã biết).</item>
    /// <item>dac_biet: không dùng công thức trong bộ đề, có hướng đi riêng nhưng vẫn đúng bản chất (ngoai_bo).</item>
    /// <item>sai: không đúng bản chất.</item>
    /// </list>
    /// Quy tắc an toàn:
    /// <list type="bullet">
    /// <item>Câu không có trong buổi / không có trong bộ đề / bị trùng -> bỏ. Mã ý cốt lõi lạ -> bỏ.</item>
    /// <item>AI nói "sai" hoặc khớp một lỗi hay gặp -> sai (thà khắt khe còn hơn chấm đúng nhầm).</item>
    /// <item>Đường chuẩn: giữ dung hoặc hoi_dac_biet theo AI (dac_biet -> hoi_dac_biet vì vẫn cùng công thức).</item>
    /// <item>Đường vòng đã biết -> hoi_dac_biet. Mã đường lạ / ngoai_bo -> dac_biet.</item>
    /// <item>Không phải sai mà đạt ít hơn MinCorePoints ý -> sai.</item>
    /// </list>
    /// </remarks>
    public class AnswerVerdictValidator : IAnswerVerdictValidator
    {
        #region Fields
        private const int MaxFeedbackLength = 500;

        /// <summary>Đường chuẩn trong bộ đề có Category = dung.</summary>
        private const string StandardPathCategory = AnswerCategory.Correct;

        private readonly IQuestionBankStore _store;
        private readonly ILogger<AnswerVerdictValidator> _logger;
        #endregion

        #region Constructor
        public AnswerVerdictValidator(IQuestionBankStore store, ILogger<AnswerVerdictValidator> logger)
        {
            _store = store;
            _logger = logger;
        }
        #endregion

        #region IAnswerVerdictValidator
        public List<AnswerVerdict> Normalize(IEnumerable<AnswerVerdict> rawVerdicts, IReadOnlyCollection<string> askedQuestionIds,
                                         IReadOnlyList<BankQuestion>? cvQuestions = null)
        {
            var result = new List<AnswerVerdict>();
            foreach (var raw in rawVerdicts)
            {
                var question = _store.Find(cvQuestions, raw.QuestionId);   // [CV-QUESTIONS] gồm cả câu theo CV
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
        #endregion
    }
}
