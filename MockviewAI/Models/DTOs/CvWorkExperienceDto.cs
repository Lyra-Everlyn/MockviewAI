namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] 
    /// Một mục kinh nghiệm làm việc / thực tập.
    /// </summary>
    public class CvWorkExperienceDto
    {
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Chức danh.</summary>
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>Thời gian làm, dạng chữ (vd "06/2024 – nay").</summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>Công việc chính.</summary>
        public string MainResponsibilities { get; set; } = string.Empty;

        /// <summary>Thành tích nổi bật.</summary>
        public List<string> Achievements { get; set; } = new();
    }
}
