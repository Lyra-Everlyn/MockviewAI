// [QUESTION-BANK] 4 nhãn GHI NHẬN CÁCH TIẾP CẬN ("nhiều đường đến điểm B"), không phải thang điểm.
// Dùng hằng số, không gõ chuỗi tay (giống SessionStatus).

namespace MockviewAI.Models.Ai
{
    public static class AnswerCategory
    {
        public const string Correct = "dung";                 // Đi đúng công thức thông thường
        public const string AlternativePath = "hoi_dac_biet"; // Cùng công thức nhưng cách làm / giải thích khác thường
        public const string Special = "dac_biet";             // Hướng đi riêng (ngoài bộ đề) nhưng vẫn đúng bản chất
        public const string Wrong = "sai";                    // Không đúng bản chất

        // Mã "con đường" khi câu trả lời không khớp đường / lỗi nào trong bộ đề
        public const string OutOfBank = "ngoai_bo";

        public static readonly string[] All = { Correct, AlternativePath, Special, Wrong };

        public static bool IsValid(string? category) => category != null && All.Contains(category);

        public static string DisplayName(string? category) => category switch
        {
            Correct => "Đúng",
            AlternativePath => "Hơi đặc biệt",
            Special => "Đặc biệt",
            Wrong => "Sai",
            _ => string.Empty
        };

        // Một dòng giải thích hiện cạnh nhãn, để người dùng không hiểu nhầm là thang điểm
        public static string Description(string? category) => category switch
        {
            Correct => "Bạn đi đúng hướng thông thường.",
            AlternativePath => "Cùng hướng chuẩn nhưng bạn có cách làm / cách giải thích riêng.",
            Special => "Bạn có hướng đi riêng, khác cách thông thường nhưng vẫn đúng bản chất.",
            Wrong => "Câu trả lời chưa đúng bản chất câu hỏi.",
            _ => string.Empty
        };

        public static string Icon(string? category) => category switch
        {
            Correct => "✓",
            AlternativePath => "↻",
            Special => "★",
            Wrong => "✗",
            _ => string.Empty
        };
    }
}
