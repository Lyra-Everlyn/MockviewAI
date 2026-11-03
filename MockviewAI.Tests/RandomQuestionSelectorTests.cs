// Test chọn câu hỏi: câu 1 là giới thiệu, không trùng, đúng số câu 5..10, chỉ lấy câu của người phỏng vấn + vị trí đã chọn.

using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class RandomQuestionSelectorTests
    {
        private static RandomQuestionSelector NewSelector(int seed = 1) => new(TestData.Store, new Random(seed));

        [Theory]
        [InlineData("nam", "developer", 5)]
        [InlineData("nam", "developer", 7)]
        [InlineData("nam", "developer", 10)]
        [InlineData("nam", "tester", 10)]
        [InlineData("nam", "ba", 10)]
        [InlineData("linh", "developer", 10)]
        [InlineData("linh", "tester", 6)]
        [InlineData("linh", "ba", 10)]
        [InlineData("an", "developer", 10)]
        [InlineData("an", "tester", 10)]
        [InlineData("an", "ba", 5)]
        public void ReturnsRequestedCount_IntroFirst_NoDuplicates(string interviewer, string position, int count)
        {
            var picked = NewSelector().Select(interviewer, position, count);
            Assert.Equal(count, picked.Count);
            Assert.True(picked[0].IsIntro);
            Assert.Equal(picked.Count, picked.Select(q => q.Id).Distinct().Count());
            Assert.All(picked, q => Assert.Contains(position, q.Positions));
            Assert.All(picked, q => Assert.Equal(interviewer, q.Interviewer));
        }

        [Fact]
        public void DifferentSessions_CanGetDifferentQuestions()
        {
            // Lập trình viên có hơn 10 câu -> 2 lần rút 5 câu thường khác nhau (ngoài câu giới thiệu)
            var first = NewSelector(1).Select("nam", "developer", 5).Select(q => q.Id).ToList();
            var others = Enumerable.Range(2, 5).Select(seed => NewSelector(seed).Select("nam", "developer", 5).Select(q => q.Id).ToList());
            Assert.All(others, o => Assert.Equal(first[0], o[0]));            // câu giới thiệu luôn đứng đầu
            Assert.Contains(others, o => !o.SequenceEqual(first));           // nhưng các câu sau thay đổi
        }

        [Fact]
        public void InterviewerWithoutBank_ReturnsEmpty_SoOldQuestionsAreUsed()
        {
            // Người phỏng vấn không có trong bộ đề -> rỗng -> InterviewService dùng câu hỏi cũ (QuestionBank.cs)
            Assert.Empty(NewSelector().Select("khong-ton-tai", "developer", 5));
        }

        [Fact]
        public void UnknownPosition_ReturnsEmpty()
        {
            Assert.Empty(NewSelector().Select("nam", "designer", 5));
        }
    }
}
