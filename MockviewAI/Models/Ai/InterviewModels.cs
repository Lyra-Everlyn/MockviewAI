// [AI-MODULE] InterviewModels
// Các lớp dữ liệu vào/ra của module AI.
// ScoreRequest (vào) -> InterviewScoreResult (ra) gồm Content (AI) + Delivery (code) + Total.

namespace MockviewAI.Models.Ai
{
    // One question + the candidate's answer (text from speech-to-text or typed)
    public class InterviewAnswer
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        // How long the candidate spoke (seconds). Null when the answer was typed.
        public double? DurationSeconds { get; set; }

        // Silent gaps inside the answer, in seconds (optional, from audio analysis)
        public List<double> PausesSeconds { get; set; } = new();
    }

    // Input for scoring a whole practice session
    public class ScoreRequest
    {
        public string Position { get; set; } = string.Empty;   // e.g. "Lập trình viên"
        public List<InterviewAnswer> Answers { get; set; } = new();
    }

    // Content score from the AI: 4 criteria x 25 points = 100
    public class ContentScoreResult
    {
        public int Relevance { get; set; }     // Trả lời đúng câu hỏi
        public int Structure { get; set; }     // Bố cục, mạch lạc
        public int Specificity { get; set; }   // Ví dụ, số liệu cụ thể
        public int Clarity { get; set; }       // Rõ ràng, dễ hiểu

        public int Total => Relevance + Structure + Specificity + Clarity;

        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
    }

    // Delivery score computed by plain code (no AI), 0-100
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

    // Final combined score shown to the user (always labelled "Điểm tham khảo")
    public class InterviewScoreResult
    {
        public ContentScoreResult Content { get; set; } = new();
        public DeliveryScoreResult Delivery { get; set; } = new();
        public int Total { get; set; }                  // round(0.6*content + 0.4*delivery)
    }
}
