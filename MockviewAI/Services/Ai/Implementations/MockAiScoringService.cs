// [AI-MODULE] MockAiScoringService
// Bộ chấm GIẢ: không cần API key, không tốn tiền.
// Dùng để dev giao diện/test khi chưa có key. Kết quả luôn gắn nhãn [MOCK].

using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    // Fake scorer: no API key, no cost. Same input always gives the same output.
    public class MockAiScoringService : IAiScoringService
    {
        private readonly IQuestionBankStore _bank;
        private readonly IOfflineAnswerScorer _offline;

        public MockAiScoringService(IQuestionBankStore bank, IOfflineAnswerScorer offline)
        {
            _bank = bank;
            _offline = offline;
        }

        public Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default)
        {
            var words = request.Answers.Sum(a => CountWords(a.Answer));
            var avg = request.Answers.Count == 0 ? 0 : words / request.Answers.Count;

            // Longer answers score a bit higher (capped) - only to make the UI look alive
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

        // [QUESTION-BANK][MOCK] Xếp loại dùng chung bộ chấm sơ bộ theo bộ đề (không phải AI), gắn nhãn [MOCK]
        private AnswerVerdict? MockVerdict(InterviewAnswer answer)
        {
            var question = _bank.Find(answer.QuestionId);
            if (question == null) return null;
            var verdict = _offline.Score(question, answer.ContentText);   // [FOLLOW-UP] gồm cả câu trả lời thêm
            verdict.Feedback = "[MOCK] " + verdict.Feedback;
            return verdict;
        }

        private static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
