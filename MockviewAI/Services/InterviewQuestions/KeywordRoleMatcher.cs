using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Implementations;

namespace MockviewAI.Services.InterviewQuestions
{
    /// <summary>
    /// [IT-ROLES]
    /// Đoán vị trí từ tóm tắt CV bằng từ khoá (không gọi AI): dùng khi AI gợi ý vị trí lỗi / chậm.
    /// Từ khoá trong chức danh mục tiêu tính gấp 3 lần từ khoá ở phần còn lại. Không khớp gì -> "developer".
    /// </summary>
    public static class KeywordRoleMatcher
    {
        public const string DefaultRoleKey = "developer";
        private const int TitleWeight = 3;

        /// <param name="targetTitle">Chức danh / vị trí mục tiêu trong CV (có thể rỗng).</param>
        /// <param name="cvText">Toàn bộ tóm tắt CV.</param>
        public static ItRole Match(string? targetTitle, string? cvText)
        {
            string title = Normalize(targetTitle);
            string body = Normalize(cvText);

            ItRole? best = null;
            int bestScore = 0;
            foreach (var role in ItRoleCatalog.All)
            {
                int score = role.Keywords.Sum(k => (title.Contains(" " + k + " ") ? TitleWeight : 0) + (body.Contains(" " + k + " ") ? 1 : 0));
                if (score > bestScore)
                {
                    best = role;
                    bestScore = score;
                }
            }
            return best ?? ItRoleCatalog.Find(DefaultRoleKey)!;
        }

        /// <summary>Bỏ dấu, chữ thường, các từ cách nhau 1 dấu cách, có dấu cách hai đầu để so cả cụm từ.</summary>
        private static string Normalize(string? text) => " " + string.Join(' ', VietnameseTextNormalizer.Tokenize(text)) + " ";
    }
}
