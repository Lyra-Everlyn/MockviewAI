// Test đọc phần "answers" (xếp loại từng câu) trong phản hồi Gemini. Phần thiếu / hỏng không được làm mất điểm 4 tiêu chí.

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MockviewAI.Services.Ai.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class GeminiResponseParserVerdictTests
    {
        private readonly GeminiResponseParser _parser = new(NullLogger<GeminiResponseParser>.Instance);

        // Bọc chuỗi JSON của AI vào khung phản hồi của Gemini: candidates[0].content.parts[0].text
        private static string Envelope(string aiJson) =>
            JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = aiJson } } } } } });

        [Fact]
        public void ReadsVerdicts()
        {
            var json = "{\"relevance\":20,\"structure\":18,\"specificity\":15,\"clarity\":20,\"summary\":\"ok\",\"strengths\":[],\"improvements\":[]," +
                       "\"answers\":[{\"questionId\":\"dev-csharp-01\",\"category\":\"hoi_dac_biet\",\"matchedPath\":\"vong1\"," +
                       "\"metCorePoints\":[\"c1\",\"c2\"],\"missingCorePoints\":[\"c3\"],\"feedback\":\"Tốt\"}]}";
            var result = _parser.Parse(Envelope(json));
            Assert.Equal(73, result.Total);
            var v = Assert.Single(result.Verdicts);
            Assert.Equal("dev-csharp-01", v.QuestionId);
            Assert.Equal("vong1", v.MatchedPath);
            Assert.Equal(new[] { "c1", "c2" }, v.MetCorePoints);
        }

        [Theory]
        [InlineData("")]
        [InlineData(",\"answers\":\"không phải mảng\"")]
        [InlineData(",\"answers\":[1,2,3]")]
        public void MissingOrBrokenVerdicts_KeepScores(string answersPart)
        {
            var json = "{\"relevance\":10,\"structure\":10,\"specificity\":10,\"clarity\":10,\"summary\":\"x\"" + answersPart + "}";
            var result = _parser.Parse(Envelope(json));
            Assert.Equal(40, result.Total);
            Assert.Empty(result.Verdicts);
        }
    }
}
