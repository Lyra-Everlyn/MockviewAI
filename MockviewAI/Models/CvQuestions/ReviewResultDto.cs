namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// JSON người kiểm duyệt (Groq) trả về: { "reviews": [ ... ] }.
    /// </summary>
    public class ReviewResultDto
    {
        public List<ReviewItemDto>? Reviews { get; set; }
    }
}
