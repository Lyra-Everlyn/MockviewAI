using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-MODULE] 
    /// InterviewScoringService
    /// -- Điểm vào chính của module AI: gộp điểm nội dung (60%) + cách nói (40%).
    /// -- Nhóm bạn chỉ cần gọi ScoreAsync(ScoreRequest) từ phòng phỏng vấn. Điểm luôn hiển thị là 'Điểm tham khảo'.
    /// -- Khi AI lỗi: gọi ScoreDeliveryOnly (chấm dự phòng, chỉ tính cách nói).
    /// -- Combines AI content score (60%) + code-based delivery score (40%)
    /// </summary>

    public class InterviewScoringService : IInterviewScoringService
    {
        private const double ContentWeight = 0.6;
        private const double DeliveryWeight = 0.4;

        private readonly IAiScoringService _ai;
        private readonly IDeliveryAnalyzer _delivery;
        private readonly IAnxietyEstimator _anxiety;
        private readonly IAnswerVerdictValidator _verdicts;
        private readonly IQuestionBankStore _bank;
        private readonly IOfflineAnswerScorer _offline;

        public InterviewScoringService(IAiScoringService ai, IDeliveryAnalyzer delivery, IAnxietyEstimator anxiety,
                                       IAnswerVerdictValidator verdicts, IQuestionBankStore bank, IOfflineAnswerScorer offline)
        {
            _ai = ai;
            _delivery = delivery;
            _anxiety = anxiety;
            _verdicts = verdicts;
            _bank = bank;
            _offline = offline;
        }

        public async Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default)
        {
            var delivery = _delivery.Analyze(AllAnswers(request));
            int answeredCount = request.Answers.Count + request.InvalidAnswers.Count;

            // Mọi câu đều không có ý nghĩa: không cần gọi AI, phần nội dung = 0
            var content = request.Answers.Count == 0
                ? new ContentScoreResult
                {
                    Summary = "Các câu trả lời chưa có ý nghĩa nên phần nội dung được tính 0 điểm. Hãy trả lời đúng trọng tâm câu hỏi ở lần luyện sau."
                }
                : await _ai.ScoreContentAsync(request, ct);

            // [QUESTION-BANK] Kiểm tra lại xếp loại của AI theo bộ đề; câu bị bộ lọc đánh dấu không có ý nghĩa -> "sai"
            var askedIds = request.Answers.Where(a => a.QuestionId != null).Select(a => a.QuestionId!).ToList();
            content.Verdicts = askedIds.Count == 0 ? new List<AnswerVerdict>() : _verdicts.Normalize(content.Verdicts, askedIds);
            foreach (var invalid in request.InvalidAnswers.Where(a => a.QuestionId != null))
            {
                content.Verdicts.Add(new AnswerVerdict
                {
                    QuestionId = invalid.QuestionId!,
                    Category = AnswerCategory.Wrong,
                    MatchedPath = AnswerCategory.OutOfBank,
                    Feedback = "Câu trả lời chưa có ý nghĩa nên được tính sai."
                });
            }

            // Câu không có ý nghĩa tính SAI: điểm nội dung chỉ tính trên phần câu hợp lệ
            content.ValidAnswerRatio = answeredCount == 0 ? 1.0 : request.Answers.Count / (double)answeredCount;
            if (request.InvalidAnswers.Count > 0 && request.Answers.Count > 0)
                content.Improvements.Insert(0,
                    $"{request.InvalidAnswers.Count} câu trả lời chưa có ý nghĩa nên được tính 0 điểm. Hãy trả lời đúng trọng tâm câu hỏi.");

            // content.Total is already 0-100 (4 criteria x 25)
            int total = (int)Math.Round(content.Total * ContentWeight + delivery.Total * DeliveryWeight);

            return Build(request, content, delivery, total, contentAvailable: true);
        }

        public InterviewScoreResult ScoreWithoutAi(ScoreRequest request)
        {
            var delivery = _delivery.Analyze(AllAnswers(request));
            bool hasBankQuestions = request.Answers.Concat(request.InvalidAnswers).Any(a => _bank.Find(a.QuestionId) != null);

            if (!hasBankQuestions)
            {
                // Câu hỏi kiểu cũ: không có bộ đề để so -> chỉ tính cách nói như trước
                var deliveryOnly = new ContentScoreResult
                {
                    Summary = "AI đang bận nên chưa nhận xét được nội dung. Điểm này chỉ tính phần cách nói. Hãy luyện lại sau để có nhận xét đầy đủ."
                };
                var old = Build(request, deliveryOnly, delivery, delivery.Total, contentAvailable: false);
                old.ScoringMode = ScoringModes.DeliveryOnly;
                return old;
            }

            // [QUESTION-BANK] Chấm SƠ BỘ theo bộ đề bằng code: mỗi câu -> nhãn + ý đạt / thiếu; điểm nội dung = độ phủ ý cốt lõi
            var verdicts = new List<AnswerVerdict>();
            var coverages = new List<double>();
            foreach (var answer in request.Answers)
            {
                var question = _bank.Find(answer.QuestionId);
                if (question == null) continue;
                var verdict = _offline.Score(question, answer.Answer);
                verdicts.Add(verdict);
                coverages.Add(question.CorePoints.Count == 0 ? 0 : verdict.MetCorePoints.Count / (double)question.CorePoints.Count);
            }
            foreach (var invalid in request.InvalidAnswers.Where(a => _bank.Find(a.QuestionId) != null))
            {
                verdicts.Add(new AnswerVerdict
                {
                    QuestionId = invalid.QuestionId!,
                    Category = AnswerCategory.Wrong,
                    MatchedPath = AnswerCategory.OutOfBank,
                    Feedback = "Câu trả lời chưa có ý nghĩa nên được tính sai."
                });
            }

            // 4 tiêu chí cùng lấy theo độ phủ ý cốt lõi (code không đo được bố cục / diễn đạt như AI)
            int criterion = (int)Math.Round((coverages.Count == 0 ? 0 : coverages.Average()) * 25);
            int answeredCount = request.Answers.Count + request.InvalidAnswers.Count;
            int goodCount = verdicts.Count(v => v.Category != AnswerCategory.Wrong);

            var missing = verdicts
                .SelectMany(v => (_bank.Find(v.QuestionId)?.CorePoints ?? new()).Where(c => v.MissingCorePoints.Contains(c.Id)))
                .Select(c => c.Text).Distinct().Take(3)
                .Select(text => "Bổ sung ý: " + text);

            var content = new ContentScoreResult
            {
                Relevance = criterion,
                Structure = criterion,
                Specificity = criterion,
                Clarity = criterion,
                ValidAnswerRatio = answeredCount == 0 ? 1.0 : request.Answers.Count / (double)answeredCount,
                Summary = "AI đang bận nên phần nội dung được chấm SƠ BỘ theo bộ đề (so các ý cốt lõi), không dùng AI. " +
                          "Cách chấm này chưa nhận ra được câu trả lời \"Đặc biệt\" (hướng đi mới); hãy luyện lại khi AI hoạt động để có nhận xét chi tiết.",
                Strengths = goodCount > 0 ? new List<string> { $"Bạn đã chạm đủ ý chính ở {goodCount}/{answeredCount} câu." } : new List<string>(),
                Improvements = missing.ToList(),
                Verdicts = verdicts
            };

            int total = (int)Math.Round(content.Total * ContentWeight + delivery.Total * DeliveryWeight);
            var result = Build(request, content, delivery, total, contentAvailable: true);
            result.ScoringMode = ScoringModes.BankRules;
            return result;
        }

        // Cách nói đo trên mọi câu đã nói (kể cả câu không có ý nghĩa)
        private static List<InterviewAnswer> AllAnswers(ScoreRequest request) =>
            request.Answers.Concat(request.InvalidAnswers).ToList();

        private InterviewScoreResult Build(ScoreRequest request, ContentScoreResult content, DeliveryScoreResult delivery, int total, bool contentAvailable)
        {
            var (level, reasons) = _anxiety.Estimate(delivery, request.QuestionCount, request.SkippedCount);
            return new InterviewScoreResult
            {
                Content = content,
                Delivery = delivery,
                Total = total,
                ContentAvailable = contentAvailable,
                AnxietyIndicator = level,
                AnxietyReasons = reasons
            };
        }
    }
}
