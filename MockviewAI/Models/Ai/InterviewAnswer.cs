namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-MODULE]
    /// -- Lớp dữ liệu vào/ra của module AI
    /// -- One question + the candidate's answer (text from speech-to-text or typed)
    /// </summary>
    // 
    public class InterviewAnswer
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        // How long the candidate spoke (seconds). Null when the answer was typed.
        public double? DurationSeconds { get; set; }

        // Silent gaps inside the answer, in seconds (optional, from audio analysis)
        public List<double> PausesSeconds { get; set; } = new();
    }
}
