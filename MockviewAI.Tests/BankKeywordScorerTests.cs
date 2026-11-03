// Test chấm SƠ BỘ theo bộ đề (không dùng AI): an toàn trước (câu Sai không được thành đúng), không chấm oan câu đúng.

using MockviewAI.Models.Ai;
using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class BankKeywordScorerTests
    {
        private readonly BankKeywordScorer _scorer = new();
        private readonly JsonQuestionBankStore _store = TestData.Store;

        // 4 câu mẫu gốc (đợt 1) đã được dùng để chỉnh từ khoá -> phải chấm đúng 100%.
        // 4 câu mẫu mới (đợt 2) KHÔNG dùng để chỉnh -> chỉ đo, và giữ mức hiện tại làm sàn (không được tệ hơn).
        private static readonly HashSet<string> TuningStyles = new()
        {
            "đầy đủ theo công thức chuẩn", "đường vòng đã biết", "cách tiếp cận riêng", "mắc lỗi hay gặp"
        };

        private static bool IsTuning(MockviewAI.Models.InterviewQuestions.SampleAnswer s) => TuningStyles.Contains(s.Style);

        [Fact]
        public void WrongSamples_AreNeverScoredAsCorrect()
        {
            foreach (var q in _store.Questions)
                foreach (var s in q.Samples.Where(s => IsTuning(s) && s.Category == AnswerCategory.Wrong))
                    Assert.True(_scorer.Score(q, s.Answer).Category == AnswerCategory.Wrong, $"{q.Id}: câu mẫu SAI bị chấm thành đúng");
        }

        [Fact]
        public void CorrectSamples_AreNotScoredAsWrong()
        {
            // Đúng / Hơi đặc biệt / Đặc biệt đều là câu trả lời đúng bản chất -> chấm sơ bộ không được ra "sai"
            foreach (var q in _store.Questions)
                foreach (var s in q.Samples.Where(s => IsTuning(s) && s.Category != AnswerCategory.Wrong))
                    Assert.True(_scorer.Score(q, s.Answer).Category != AnswerCategory.Wrong, $"{q.Id}: câu mẫu '{s.Category}' bị chấm oan là sai");
        }

        [Theory]
        // Đo ngày 02/11 trên câu mẫu mới (chưa chỉnh từ khoá theo chúng):
        //   Nam  : lọt 3/35 câu Sai, chấm oan 1/105 câu đúng
        //   Linh : lọt 1/12,        chấm oan 1/36
        //   An   : lọt 0/12,        chấm oan 0/36
        [InlineData("nam", 3, 1)]
        [InlineData("linh", 1, 1)]
        [InlineData("an", 0, 0)]
        public void HeldOutSamples_StayAtOrAboveMeasuredBaseline(string interviewer, int maxLeaked, int maxWronglyFailed)
        {
            int leaked = 0, wronglyFailed = 0;
            foreach (var q in _store.Questions.Where(q => q.Interviewer == interviewer))
                foreach (var s in q.Samples.Where(s => !IsTuning(s)))
                {
                    bool scoredWrong = _scorer.Score(q, s.Answer).Category == AnswerCategory.Wrong;
                    if (s.Category == AnswerCategory.Wrong && !scoredWrong) leaked++;
                    if (s.Category != AnswerCategory.Wrong && scoredWrong) wronglyFailed++;
                }
            Assert.True(leaked <= maxLeaked, $"{interviewer}: câu Sai lọt thành đúng {leaked} (sàn: {maxLeaked})");
            Assert.True(wronglyFailed <= maxWronglyFailed, $"{interviewer}: câu đúng bị chấm oan {wronglyFailed} (sàn: {maxWronglyFailed})");
        }

        [Fact]
        public void NegatedMistake_IsNotAMistake()
        {
            var q = _store.Find("dev-csharp-01")!;
            var v = _scorer.Score(q, "Em không nuốt lỗi: dùng try catch bắt exception cụ thể, ghi log rồi giải phóng tài nguyên trong finally.");
            Assert.NotEqual(AnswerCategory.Wrong, v.Category);
        }

        [Fact]
        public void AnswerWithoutVietnameseDiacritics_StillMatches()
        {
            var q = _store.Find("dev-csharp-01")!;
            var v = _scorer.Score(q, "Em dung try catch, bat dung loai exception, ghi log lai va dong ket noi trong finally");
            Assert.Equal(AnswerCategory.Correct, v.Category);
            Assert.Equal(new[] { "c1", "c2", "c3" }, v.MetCorePoints);
        }

        [Fact]
        public void KnownAlternativePath_IsUnusual()
        {
            var q = _store.Find("dev-csharp-01")!;
            var v = _scorer.Score(q, "Em viết một middleware xử lý lỗi tập trung, ghi log và trả trang lỗi thân thiện cho người dùng.");
            Assert.Equal(AnswerCategory.AlternativePath, v.Category);
        }

        [Fact]
        public void OffTopicAnswer_IsWrong_WithMissingPoints()
        {
            var q = _store.Find("dev-sql-01")!;
            var v = _scorer.Score(q, "Hai cái này giống nhau, chỉ là cách viết khác thôi ạ.");
            Assert.Equal(AnswerCategory.Wrong, v.Category);
            Assert.NotEmpty(v.MissingCorePoints);
            Assert.StartsWith(BankKeywordScorer.FeedbackPrefix, v.Feedback);
        }

        [Theory]
        [InlineData("Bắt LỖI bằng C#!", new[] { "bat", "loi", "bang", "c#" })]
        [InlineData("Đường dẫn /api/users/5", new[] { "duong", "dan", "api", "users", "5" })]
        public void Normalizer_RemovesDiacritics_KeepsCSharp(string input, string[] expected)
        {
            Assert.Equal(expected, VietnameseTextNormalizer.Tokenize(input));
        }
    }
}
