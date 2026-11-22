namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Định nghĩa một loại câu trả lời trong bộ đề.
    /// </summary>
    public class CategoryDefinition
    {
        /// <summary>Mã loại: dung | hoi_dac_biet | dac_biet | sai.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên hiển thị.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Giải thích ý nghĩa của loại.</summary>
        public string Meaning { get; set; } = string.Empty;
    }
}
