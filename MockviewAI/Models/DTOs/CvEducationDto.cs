namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] 
    /// Một mục học vấn.
    /// </summary>
    public class CvEducationDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;

        /// <summary>Bằng cấp (Cử nhân, Kỹ sư...).</summary>
        public string Degree { get; set; } = string.Empty;

        /// <summary>Điểm trung bình, dạng chữ; null nếu CV không ghi.</summary>
        public string? Gpa { get; set; }
    }
}
