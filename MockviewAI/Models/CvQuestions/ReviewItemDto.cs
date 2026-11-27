using System.Text.Json;

namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Kết quả duyệt một câu trong JSON của Groq. Thiếu index hoặc verdict lạ thì coi là reject.
    /// </summary>
    public class ReviewItemDto
    {
        public int? Index { get; set; }
        public string? Verdict { get; set; }
        public List<string>? Issues { get; set; }

        /// <summary>9 tiêu chí: grounded, no_assumption, understanding, role, persona, correct, level, safe, clear (false ở tiêu chí nào thì không cho pass).</summary>
        public Dictionary<string, JsonElement>? Criteria { get; set; }
    }
}
