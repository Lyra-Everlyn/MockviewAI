using System.Globalization;
using System.Text;
using System.Text.Json;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.CvParser;

namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Đọc CV trình duyệt gửi lên (JSON camelCase từ localStorage) và rút gọn thành đoạn văn ngắn cho prompt.
    /// Dữ liệu từ trình duyệt không đáng tin: luôn làm sạch bằng CvDataSanitizer và giới hạn độ dài.
    /// </summary>
    public static class CvContextBuilder
    {
        #region Fields
        public const int MaxJsonChars = 40_000;
        private const int MaxContextChars = 3000;
        private const int MaxProjects = 4;
        private const int MaxJobs = 3;

        private static readonly JsonSerializerOptions BrowserJson = new() { PropertyNameCaseInsensitive = true };
        #endregion

        #region Public
        /// <summary>Rỗng khi không có CV, JSON hỏng hoặc CV trống.</summary>
        public static string FromBrowserJson(string? cvJson)
        {
            if (string.IsNullOrWhiteSpace(cvJson) || cvJson.Length > MaxJsonChars) return string.Empty;
            try
            {
                var data = CvDataSanitizer.Sanitize(JsonSerializer.Deserialize<CvParsedDataDto>(cvJson, BrowserJson));
                return CvDataSanitizer.IsEmpty(data) ? string.Empty : Build(data);
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        public static string Build(CvParsedDataDto cv)
        {
            var sb = new StringBuilder();
            AppendLine(sb, "Vị trí mục tiêu", cv.TargetJobTitle);
            if (cv.TotalYearsOfExperience > 0)
                AppendLine(sb, "Số năm kinh nghiệm", cv.TotalYearsOfExperience.ToString("0.#", CultureInfo.InvariantCulture));
            AppendLine(sb, "Kỹ năng", string.Join(", ", cv.HardSkills));

            foreach (var p in cv.Projects.Take(MaxProjects))
            {
                sb.Append("- Dự án: ").Append(p.Name);
                if (p.Role.Length > 0) sb.Append(" | vai trò: ").Append(p.Role);
                if (p.Technologies.Count > 0) sb.Append(" | công nghệ: ").Append(string.Join(", ", p.Technologies));
                if (p.Responsibilities.Count > 0) sb.Append(" | đã làm: ").Append(string.Join("; ", p.Responsibilities.Take(3)));
                else if (p.Description.Length > 0) sb.Append(" | mô tả: ").Append(CvDataSanitizer.Clip(p.Description, 300));
                sb.AppendLine();
            }

            foreach (var w in cv.WorkExperiences.Take(MaxJobs))
            {
                sb.Append("- Kinh nghiệm: ").Append(w.JobTitle);
                if (w.CompanyName.Length > 0) sb.Append(" tại ").Append(w.CompanyName);
                if (w.Duration.Length > 0) sb.Append(" (").Append(w.Duration).Append(')');
                if (w.MainResponsibilities.Length > 0) sb.Append(" | ").Append(CvDataSanitizer.Clip(w.MainResponsibilities, 300));
                sb.AppendLine();
            }

            var text = sb.ToString().Trim();
            return text.Length <= MaxContextChars ? text : text[..MaxContextChars];
        }
        #endregion

        #region Helpers
        private static void AppendLine(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) sb.Append(label).Append(": ").AppendLine(value);
        }
        #endregion
    }
}
