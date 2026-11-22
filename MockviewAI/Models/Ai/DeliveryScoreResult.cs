namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Điểm cách nói 0–100, tính bằng code (không dùng AI).
    /// </summary>
    public class DeliveryScoreResult
    {
        #region Số đo
        /// <summary>Tốc độ nói (từ/phút); 0 nếu không biết thời lượng.</summary>
        public double WordsPerMinute { get; set; }

        /// <summary>Số từ đệm trong các câu trả lời.</summary>
        public int FillerCount { get; set; }

        /// <summary>Từ đệm trên 100 từ.</summary>
        public double FillersPer100Words { get; set; }

        /// <summary>Độ dài trung bình một câu trả lời (từ).</summary>
        public double AverageAnswerWords { get; set; }

        /// <summary>Khoảng lặng dài nhất (giây).</summary>
        public double LongestPauseSeconds { get; set; }
        #endregion

        #region Điểm thành phần
        /// <summary>Điểm tốc độ nói.</summary>
        public int PaceScore { get; set; }

        /// <summary>Điểm từ đệm.</summary>
        public int FillerScore { get; set; }

        /// <summary>Điểm độ dài câu trả lời.</summary>
        public int LengthScore { get; set; }

        /// <summary>Điểm khoảng lặng.</summary>
        public int PauseScore { get; set; }
        #endregion

        #region [VOICE] Giọng nói (chỉ có khi nói bằng micro)
        /// <summary>true = trình duyệt đo được giọng nói.</summary>
        public bool HasVoiceData { get; set; }

        /// <summary>Âm lượng cả buổi (dBFS).</summary>
        public double? VoiceLevelDb { get; set; }

        /// <summary>Tỉ lệ 0–1 thời gian nói nhỏ.</summary>
        public double? QuietRatio { get; set; }

        /// <summary>Độ lên xuống giọng (nửa cung).</summary>
        public double? PitchVariation { get; set; }

        /// <summary>Điểm âm lượng; null = không có số đo.</summary>
        public int? VolumeScore { get; set; }

        /// <summary>Điểm giọng điệu; null = không có số đo.</summary>
        public int? ToneScore { get; set; }
        #endregion

        #region Tổng hợp
        /// <summary>Điểm cách nói 0–100.</summary>
        public int Total { get; set; }

        /// <summary>Lời khuyên cách nói.</summary>
        public List<string> Tips { get; set; } = new();
        #endregion
    }
}
