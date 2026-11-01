namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV]
    /// -- Sub class
    /// </summary>
    
    public class CvEducationDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string? Gpa { get; set; }
    }
}
