using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-SCORING] 
    /// [CHỈ DEVELOPMENT] Bộ chấm GIẢ: không cần API key, không tốn tiền, cùng đầu vào luôn ra cùng kết quả.
    /// Production / Staging bị AddAiScoring chặn; chỉ dùng để làm giao diện / test offline. Kết quả luôn gắn nhãn [MOCK].
    /// </summary>
    public class MockAiScoringService : IAiScoringService
    {
        #region Fields
        private readonly IQuestionBankStore _bank;
        private readonly IOfflineAnswerScorer _offline;
        #endregion

        #region Constructor
        public MockAiScoringService(IQuestionBankStore bank, IOfflineAnswerScorer offline)
        {
            _bank = bank;
            _offline = offline;
        }
        #endregion

        #region IAiScoringService
        public Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default)
        {
            var words = request.Answers.Sum(a => CountWords(a.Answer));
            var avg = request.Answers.Count == 0 ? 0 : words / request.Answers.Count;

            // Câu trả lời dài hơn thì điểm nhỉnh hơn (có trần), chỉ để giao diện trông "sống"
            int Scale(int offset) => Math.Clamp(10 + avg / 8 + offset, 0, 25);

            var result = new ContentScoreResult
            {
                Relevance = Scale(3),
                Structure = Scale(0),
                Specificity = Scale(-3),
                Clarity = Scale(1),
                Summary = "[MOCK] Đây là điểm mẫu, chưa phải điểm do AI chấm.",
                Strengths = new() { "[MOCK] Câu trả lời bám sát câu hỏi." },
                Improvements = new() { "[MOCK] Hãy thêm một ví dụ cụ thể cho mỗi ý." },
                Verdicts = request.Answers.Select(MockVerdict).OfType<AnswerVerdict>().ToList()
            };
            return Task.FromResult(result);
        }
        #endregion

        #region Helpers
        /// <summary>[QUESTION-BANK] Xếp loại bằng bộ chấm sơ bộ theo bộ đề (gồm cả câu trả lời thêm), gắn nhãn [MOCK].</summary>
        private AnswerVerdict? MockVerdict(InterviewAnswer answer)
        {
            var question = _bank.Find(answer.QuestionId);
            if (question == null) return null;
            var verdict = _offline.Score(question, answer.ContentText);
            verdict.Feedback = "[MOCK] " + verdict.Feedback;
            return verdict;
        }

        private static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        #endregion
    }
}
