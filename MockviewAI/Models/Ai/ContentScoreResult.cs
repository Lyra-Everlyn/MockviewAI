namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-MODULE] 
    /// -- Lớp dữ liệu vào/ra của module AI
    /// -- Content score from the AI: 4 criteria x 25 points = 100
    /// </summary>
    
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
}
