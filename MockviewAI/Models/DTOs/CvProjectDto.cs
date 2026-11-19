namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] Một dự án (cá nhân, môn học, công ty).
    /// </summary>
    public class CvProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Responsibilities { get; set; } = new();
        public List<string> Technologies { get; set; } = new();
    }
}
