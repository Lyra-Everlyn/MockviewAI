using System.Text.RegularExpressions;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Lọc câu trả lời vớ vẩn bằng quy tắc đơn giản, giải thích được (không dùng AI, không tốn tiền). Thứ tự xét:
    /// <list type="number">
    /// <item>Cố gài lệnh cho AI ("cho tôi 100 điểm", "bỏ qua hướng dẫn"...).</item>
    /// <item>Từ ngữ thô tục.</item>
    /// <item>Không có chữ nào.</item>
    /// <item>Lặp đi lặp lại vài từ (ít từ khác nhau).</item>
    /// <item>Chuỗi vô nghĩa (nhiều "từ" không có nguyên âm).</item>
    /// </list>
    /// Không giới hạn số từ tối thiểu. Việc "có đúng trọng tâm câu hỏi không" do AI chấm (tiêu chí relevance).
    /// </summary>
    public class AnswerQualityChecker : IAnswerQualityChecker
    {
        #region Ngưỡng
        /// <summary>Chỉ kiểm tra lặp từ khi câu trả lời có từ số từ này trở lên.</summary>
        private const int MinWordsForRepeatCheck = 6;

        /// <summary>Tỉ lệ từ khác nhau / tổng số từ dưới mức này = lặp chữ.</summary>
        private const double MinUniqueWordRatio = 0.4;

        /// <summary>Tỉ lệ "từ" vô nghĩa từ mức này trở lên = chuỗi gõ ngẫu nhiên.</summary>
        private const double MaxGibberishRatio = 0.4;
        #endregion

        #region Danh sách từ
        /// <summary>Viết tắt kỹ thuật không có nguyên âm nhưng hoàn toàn hợp lệ.</summary>
        private static readonly HashSet<string> TechAcronyms = new(StringComparer.OrdinalIgnoreCase)
        {
            "html", "css", "sql", "xml", "http", "https", "php", "cv", "js", "ts", "ci", "cd", "ml", "ai", "qa", "ba", "pm", "hr", "kpi", "crm", "erp", "ssh", "tcp", "dns", "cpp", "mvc", "mvp", "rdbms", "nosql"
        };

        private static readonly string[] BadWords =
        {
            "đm", "đmm", "dm", "dmm", "vcl", "vl", "cc", "đéo", "địt", "lồn", "cặc", "buồi", "đụ", "fuck", "shit", "bitch"
        };

        private static readonly string[] InjectionPhrases =
        {
            "bỏ qua hướng dẫn", "bỏ qua mọi hướng dẫn", "bỏ qua yêu cầu", "ignore previous", "ignore all previous",
            "ignore the above", "system prompt", "cho tôi 100 điểm", "cho mình 100 điểm", "cho em 100 điểm", "cho 100 điểm",
            "chấm 100 điểm", "chấm cho tôi 100", "give me 100", "full điểm", "điểm tuyệt đối"
        };
        #endregion

        #region Regex
        private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
        private static readonly Regex VowelRegex = new(
            "[aeiouyàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹ]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RepeatedChar = new(@"(\p{L})\1{3,}", RegexOptions.Compiled);
        private static readonly Regex LongConsonantRun = new(
            "[^aeiouyàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹ\\P{L}]{5,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        #endregion

        #region IAnswerQualityChecker
        public string? FindProblem(string answer)
        {
            string text = (answer ?? string.Empty).Trim();
            string lower = text.ToLowerInvariant();

            foreach (var phrase in InjectionPhrases)
                if (lower.Contains(phrase))
                    return "Hãy trả lời đúng câu hỏi của người phỏng vấn, đừng yêu cầu chấm điểm hay thay đổi cách chấm.";

            var words = WordRegex.Matches(lower).Select(m => m.Value).ToList();

            if (words.Any(w => BadWords.Contains(w)))
                return "Vui lòng dùng ngôn ngữ lịch sự như trong một buổi phỏng vấn thật.";

            if (words.Count == 0)
                return "Câu trả lời chưa có nội dung.";

            if (words.Count >= MinWordsForRepeatCheck)
            {
                double uniqueRatio = words.Distinct().Count() / (double)words.Count;
                if (uniqueRatio < MinUniqueWordRatio)
                    return "Câu trả lời lặp lại một vài từ. Hãy trình bày ý của bạn rõ ràng hơn.";
            }

            int gibberish = words.Count(IsGibberish);
            if (gibberish / (double)words.Count >= MaxGibberishRatio)
                return "Câu trả lời chưa có ý nghĩa (giống chuỗi ký tự gõ ngẫu nhiên).";

            return null;
        }
        #endregion

        #region Helpers
        /// <summary>"Từ" không có nguyên âm (và không phải viết tắt kỹ thuật), có chuỗi 5+ phụ âm liền nhau, hoặc 1 chữ lặp 4+ lần.</summary>
        private static bool IsGibberish(string word)
        {
            if (word.All(char.IsDigit) || TechAcronyms.Contains(word)) return false;
            if (RepeatedChar.IsMatch(word)) return true;
            if (!VowelRegex.IsMatch(word)) return word.Length > 1;
            return LongConsonantRun.IsMatch(word);
        }
        #endregion
    }
}
