namespace MockviewAI.Services.Speech
{
    /// <summary>
    /// [SPEECH] 
    /// Đoán nhanh văn bản là tiếng Việt hay tiếng Anh theo tỉ lệ từ mang dấu tiếng Việt.
    /// </summary>
    public static class VietnameseText
    {
        private const string Letters = "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ";
        private const int MinWords = 4;

        // Tiếng Việt thường có 40–60% từ mang dấu; tiếng Anh gần như 0%
        private const double VietnameseRatio = 0.25;
        private const double EnglishRatio = 0.08;

        /// <summary>Tỉ lệ từ có dấu tiếng Việt; -1 khi quá ít từ để kết luận.</summary>
        public static double AccentedWordRatio(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;
            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < MinWords) return -1;
            int accented = words.Count(w => w.ToLowerInvariant().IndexOfAny(Letters.ToCharArray()) >= 0);
            return accented / (double)words.Length;
        }

        public static bool LooksVietnamese(string? text) => AccentedWordRatio(text) >= VietnameseRatio;

        public static bool LooksEnglish(string? text)
        {
            var ratio = AccentedWordRatio(text);
            return ratio >= 0 && ratio < EnglishRatio;
        }
    }
}
