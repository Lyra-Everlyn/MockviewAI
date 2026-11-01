namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV]
    /// -- Sub class
    /// </summary>
    
    public class CvWorkExperienceDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string MainResponsibilities { get; set; } = string.Empty;
        public List<string> Achievements { get; set; } = new();
    }
}
