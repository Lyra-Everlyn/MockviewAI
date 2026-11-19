namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] Dữ liệu CV sau khi AI phân tích.
    /// </summary>
    public class CvParsedDataDto
    {
        #region Thông tin chung
        public string FullName { get; set; } = string.Empty;
        public string TargetJobTitle { get; set; } = string.Empty;
        public string CareerObjective { get; set; } = string.Empty;
        public double TotalYearsOfExperience { get; set; }
        #endregion

        #region Chi tiết
        public List<CvWorkExperienceDto> WorkExperiences { get; set; } = new();
        public List<CvProjectDto> Projects { get; set; } = new();
        public List<string> HardSkills { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();
        public List<CvEducationDto> Educations { get; set; } = new();
        public List<CvCertificationDto> CertificationsAndAwards { get; set; } = new();
        #endregion
    }
}
