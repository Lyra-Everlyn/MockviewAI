namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Một lỗi hay gặp: câu trả lời khớp lỗi này thì xếp loại "sai".
    /// </summary>
    public class CommonMistake
    {
        /// <summary>Mã lỗi: loi1, loi2...</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Mô tả ngắn lỗi.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Vì sao đây là lỗi.</summary>
        public string Why { get; set; } = string.Empty;

        /// <summary>Cụm từ đặc trưng dùng khi chấm sơ bộ; bị phủ định (vd "không nuốt lỗi") thì không tính.</summary>
        public List<string> Signals { get; set; } = new();
    }
}
