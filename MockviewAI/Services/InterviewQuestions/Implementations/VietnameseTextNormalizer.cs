using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Chuẩn hoá chữ để so khớp từ khoá: chữ thường, bỏ dấu tiếng Việt (đ -> d),
    /// bỏ ký tự đặc biệt (giữ # và +), tách thành từ.
    /// "Bắt lỗi", "bat loi", "BẮT LỖI" đều thành ["bat", "loi"]; "C#" vẫn là "c#".
    /// </summary>
    public static class VietnameseTextNormalizer
    {
        private static readonly Regex NonWord = new("[^a-z0-9#+]+", RegexOptions.Compiled);

        public static string[] Tokenize(string? text)
        {
            string lower = (text ?? string.Empty).ToLowerInvariant().Replace('đ', 'd');
            var sb = new StringBuilder(lower.Length);

            // Tách chữ và dấu (FormD) rồi bỏ phần dấu
            foreach (char ch in lower.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return NonWord.Replace(sb.ToString(), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
