namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [QUESTION-BANK] 4 nhãn ghi nhận cách tiếp cận ("nhiều đường đến điểm B"), không phải thang điểm.
    /// Luôn dùng hằng số này, không gõ chuỗi tay.
    /// </summary>
    public static class AnswerCategory
    {
        #region Mã nhãn
        /// <summary>Đi đúng công thức thông thường.</summary>
        public const string Correct = "dung";

        /// <summary>Cùng công thức nhưng cách làm / giải thích khác thường.</summary>
        public const string AlternativePath = "hoi_dac_biet";

        /// <summary>Hướng đi riêng (ngoài bộ đề) nhưng vẫn đúng bản chất.</summary>
        public const string Special = "dac_biet";

        /// <summary>Không đúng bản chất.</summary>
        public const string Wrong = "sai";

        /// <summary>Mã "con đường" khi câu trả lời không khớp đường / lỗi nào trong bộ đề.</summary>
        public const string OutOfBank = "ngoai_bo";

        public static readonly string[] All = { Correct, AlternativePath, Special, Wrong };
        #endregion

        #region Hiển thị
        public static bool IsValid(string? category) => category != null && All.Contains(category);

        public static string DisplayName(string? category) => category switch
        {
            Correct => "Đúng",
            AlternativePath => "Hơi đặc biệt",
            Special => "Đặc biệt",
            Wrong => "Sai",
            _ => string.Empty
        };

        /// <summary>Một dòng giải thích cạnh nhãn, để người dùng không hiểu nhầm là thang điểm.</summary>
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
        #endregion
    }
}
