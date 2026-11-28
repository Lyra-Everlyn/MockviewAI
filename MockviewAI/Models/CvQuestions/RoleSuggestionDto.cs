namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [IT-ROLES]
    /// Vị trí gợi ý từ CV, trả về trang Tạo buổi (người dùng vẫn đổi được).
    /// </summary>
    public class RoleSuggestionDto
    {
        /// <summary>Khoá trong ItRoleCatalog.</summary>
        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        /// <summary>Lý do ngắn (tiếng Việt) để người dùng hiểu vì sao được gợi ý.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>ai = Gemini gợi ý; keyword = đoán bằng từ khoá (AI lỗi / chậm).</summary>
        public string Source { get; set; } = string.Empty;
    }
}
