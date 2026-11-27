namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Các câu người soạn trả về và model thực sự đã soạn (để ghi vào DB phục vụ báo cáo).
    /// </summary>
    /// <param name="Drafts">Câu đọc được từ JSON (chưa qua luật code).</param>
    /// <param name="Model">Tên model Gemini đã trả lời.</param>
    public record CvGenerationResult(List<CvQuestionDraft> Drafts, string Model);
}
