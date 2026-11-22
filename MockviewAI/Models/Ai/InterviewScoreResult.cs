namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Kết quả chấm cuối cùng của một buổi, hiển thị dưới nhãn "Điểm tham khảo".
    /// </summary>
    public class InterviewScoreResult
    {
        /// <summary>Điểm nội dung (AI hoặc sơ bộ theo bộ đề).</summary>
        public ContentScoreResult Content { get; set; } = new();

        /// <summary>Điểm cách nói (tính bằng code).</summary>
        public DeliveryScoreResult Delivery { get; set; } = new();

        /// <summary>round(0.6 × nội dung + 0.4 × cách nói); chấm dự phòng thì bằng điểm cách nói.</summary>
        public int Total { get; set; }

        /// <summary>false = AI không phản hồi, hệ thống chấm dự phòng chỉ tính cách nói.</summary>
        public bool ContentAvailable { get; set; } = true;

        /// <summary>[QUESTION-BANK] Cách chấm, xem <see cref="ScoringModes"/>.</summary>
        public string ScoringMode { get; set; } = ScoringModes.Ai;

        /// <summary>Dấu hiệu lo lắng ước tính 1–10; 0 = chưa đủ dữ liệu.</summary>
        public int AnxietyIndicator { get; set; }

        /// <summary>Lý do đi kèm dấu hiệu lo lắng.</summary>
        public List<string> AnxietyReasons { get; set; } = new();
    }
}
