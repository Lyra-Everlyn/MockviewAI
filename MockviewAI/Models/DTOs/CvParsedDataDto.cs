namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] 
    /// Dữ liệu CV sau khi AI phân tích; người dùng sửa lại trước khi chốt, trình duyệt lưu dạng JSON.
    /// </summary>
    public class CvParsedDataDto
    {
        #region Thông tin chung
        public string FullName { get; set; } = string.Empty;

        /// <summary>Chức danh mục tiêu ghi trong CV.</summary>
        public string TargetJobTitle { get; set; } = string.Empty;

        /// <summary>Mục tiêu nghề nghiệp.</summary>
        public string CareerObjective { get; set; } = string.Empty;

        /// <summary>Tổng số năm kinh nghiệm.</summary>
        public double TotalYearsOfExperience { get; set; }
        #endregion

        #region Chi tiết
        public List<CvWorkExperienceDto> WorkExperiences { get; set; } = new();
        public List<CvProjectDto> Projects { get; set; } = new();

        /// <summary>Kỹ năng chuyên môn (ngôn ngữ, framework, công cụ).</summary>
        public List<string> HardSkills { get; set; } = new();

        /// <summary>Kỹ năng mềm.</summary>
        public List<string> SoftSkills { get; set; } = new();

        public List<CvEducationDto> Educations { get; set; } = new();
        public List<CvCertificationDto> CertificationsAndAwards { get; set; } = new();
        #endregion
    }
}
