// [AI-MODULE] AnswerQualityChecker
// Lọc câu trả lời vớ vẩn bằng quy tắc đơn giản, giải thích được (không dùng AI, không tốn tiền):
//   1. Quá ngắn (< 5 từ)            2. Lặp đi lặp lại vài từ (ít từ khác nhau)
//   3. Chuỗi vô nghĩa (nhiều "từ" không có nguyên âm)   4. Từ ngữ thô tục
//   5. Cố gài lệnh cho AI ("cho tôi 100 điểm", "bỏ qua hướng dẫn"...)
// Việc "có đúng trọng tâm câu hỏi hay không" do AI chấm nội dung đánh giá thêm (tiêu chí relevance).
// Các ngưỡng nằm ngay đầu file để dễ chỉnh khi test thật.

using System.Text.RegularExpressions;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    public class AnswerQualityChecker : IAnswerQualityChecker
    {
        private const int MinWords = 5;
        private const int MinWordsForRepeatCheck = 6;
        private const double MinUniqueWordRatio = 0.4;
        private const double MaxGibberishRatio = 0.4;

        // Từ viết tắt kỹ thuật không có nguyên âm nhưng hoàn toàn hợp lệ
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

        private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
        private static readonly Regex VowelRegex = new(
            "[aeiouyàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹ]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LongConsonantRun = new(
            "[^aeiouyàáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹ\\P{L}]{5,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            if (words.Count < MinWords)
                return "Câu trả lời quá ngắn. Hãy nói đầy đủ hơn, ít nhất một vài câu.";

            if (words.Count >= MinWordsForRepeatCheck)
            {
                double uniqueRatio = words.Distinct().Count() / (double)words.Count;
                if (uniqueRatio < MinUniqueWordRatio)
                    return "Câu trả lời lặp lại một vài từ. Hãy trình bày ý của bạn rõ ràng hơn.";
            }

            int gibberish = words.Count(IsGibberish);
            if (gibberish / (double)words.Count >= MaxGibberishRatio)
                return "Hệ thống chưa hiểu câu trả lời của bạn. Hãy nói rõ ràng hơn hoặc gõ lại câu trả lời.";

            return null;
        }

        // "Từ" không có nguyên âm (và không phải viết tắt kỹ thuật), hoặc có chuỗi 5+ phụ âm liền nhau
        private static bool IsGibberish(string word)
        {
            if (word.All(char.IsDigit) || TechAcronyms.Contains(word)) return false;
            if (!VowelRegex.IsMatch(word)) return word.Length > 1;
            return LongConsonantRun.IsMatch(word);
        }
    }
}
