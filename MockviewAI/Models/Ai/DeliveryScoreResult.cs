namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// // [AI-MODULE] 
    /// -- Lớp dữ liệu vào/ra của module AI
    /// -- Delivery score computed by plain code (no AI), 0-100
    /// </summary>
    
    public class DeliveryScoreResult
    {
        public double WordsPerMinute { get; set; }     // 0 when duration unknown
        public int FillerCount { get; set; }
        public double FillersPer100Words { get; set; }
        public double AverageAnswerWords { get; set; }
        public double LongestPauseSeconds { get; set; }

        public int PaceScore { get; set; }
        public int FillerScore { get; set; }
        public int LengthScore { get; set; }
        public int PauseScore { get; set; }
        public int Total { get; set; }                  // 0-100
        public List<string> Tips { get; set; } = new();
    }
}
