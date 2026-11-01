namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV]
    /// -- Tóm tắt thông tin của 1 bộ CV của người dùng
    /// -- Main class
    /// </summary>
    
    public class CvParsedDataDto
    {
        // --- THÔNG TIN CÁ NHÂN ---
        public string FullName { get; set; } = string.Empty;
        public string TargetJobTitle { get; set; } = string.Empty;


        // --- MỤC TIÊU NGHỀ NGHIỆP ---
        public string CareerObjective { get; set; } = string.Empty;


        // --- KINH NGHIỆM LÀM VIỆC ---
        public double TotalYearsOfExperience { get; set; }
        public List<CvWorkExperienceDto> WorkExperiences { get; set; } = new();


        // --- KỸ NĂNG CHUYÊN MÔN & MỀM ---
        public List<string> HardSkills { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();


        // --- TRÌNH ĐỘ HỌC VẤN & BẰNG CẤP / CHỨNG CHỈ / GIẢI THƯỞNG ---
        public List<CvEducationDto> Educations { get; set; } = new();
        public List<CvCertificationDto> CertificationsAndAwards { get; set; } = new();
    }
}
