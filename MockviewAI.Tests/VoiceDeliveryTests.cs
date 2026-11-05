// Test Đợt 7 — âm lượng + giọng điệu trong điểm CÁCH NÓI (DeliveryAnalyzer) và nhãn hiển thị (VoiceMetrics).

using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class VoiceDeliveryTests
    {
        // ~110 từ trong 50 giây (~130 từ/phút), không từ đệm, không khoảng lặng dài -> 4 tiêu chí cũ đều 100
        private static string GoodText => string.Join(" ", Enumerable.Repeat("em đã làm dự án web bán hàng với nhóm bốn người", 11));

        private static InterviewAnswer Answer(double? level, double? quiet, double? pitch) => new()
        {
            Question = "q",
            Answer = GoodText,
            DurationSeconds = 50,
            VoiceLevelDb = level,
            QuietRatio = quiet,
            PitchVariation = pitch
        };

        [Fact]
        public void NoVoiceData_KeepsOldScoring()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(null, null, null) });
            Assert.False(r.HasVoiceData);
            Assert.Null(r.VolumeScore);
            Assert.Equal(100, r.Total);   // như trước Đợt 7: câu gõ chữ / buổi cũ không bị trừ
        }

        [Fact]
        public void GoodVoice_FullScore_NoVoiceTips()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(-24, 0.05, 3.0) });
            Assert.True(r.HasVoiceData);
            Assert.Equal(100, r.VolumeScore);
            Assert.Equal(100, r.ToneScore);
            Assert.Equal(100, r.Total);
            Assert.DoesNotContain(r.Tips, t => t.Contains("nhỏ") || t.Contains("đều đều"));
        }

        [Fact]
        public void QuietVoice_LowersScore_AndGivesTip()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(-44, 0.8, 3.0) });
            Assert.True(r.VolumeScore < 30);
            Assert.True(r.Total < 100);
            Assert.Contains(r.Tips, t => t.Contains("nói nhỏ"));
        }

        [Fact]
        public void MonotoneVoice_LowersToneScore_AndGivesTip()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(-24, 0.05, 0.6) });
            Assert.True(r.ToneScore < 30);
            Assert.Contains(r.Tips, t => t.Contains("đều đều"));
        }

        [Fact]
        public void VolumeWithoutPitch_StillScoresVolume_ToneIgnored()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(-24, 0.05, null) });
            Assert.Equal(100, r.VolumeScore);
            Assert.Null(r.ToneScore);
            Assert.Equal(100, r.Total);
        }

        [Fact]
        public void MixedAnswers_AverageOnlyThoseWithVoice()
        {
            var r = new DeliveryAnalyzer().Analyze(new[] { Answer(-44, 0.8, 3), Answer(null, null, null), Answer(-24, 0.0, 3) });
            Assert.Equal(0.4, r.QuietRatio!.Value, 3);
        }

        [Theory]
        [InlineData(-45.0, "Nhỏ")]
        [InlineData(-25.0, "Vừa")]
        [InlineData(-5.0, "Quá to")]
        [InlineData(null, "—")]
        public void LevelLabels_MatchJavaScript(double? db, string expected)
        {
            Assert.Equal(expected, VoiceMetrics.LevelLabel(db));
        }

        [Theory]
        [InlineData(0.8, "Đều đều")]
        [InlineData(3.0, "Tự nhiên")]
        [InlineData(7.0, "Nhấn nhá nhiều")]
        [InlineData(null, "—")]
        public void ToneLabels_MatchJavaScript(double? st, string expected)
        {
            Assert.Equal(expected, VoiceMetrics.ToneLabel(st));
        }

        [Fact]
        public void Sanitize_RejectsOutOfRangeValues()
        {
            Assert.Null(VoiceMetrics.CleanLevel(5));        // dBFS không thể > 0
            Assert.Null(VoiceMetrics.CleanRatio(1.5));
            Assert.Null(VoiceMetrics.CleanPitch(-1));
            Assert.Equal(-30.0, VoiceMetrics.CleanLevel(-30));
            Assert.Equal(0.25, VoiceMetrics.CleanRatio(0.25));
        }
    }
}
