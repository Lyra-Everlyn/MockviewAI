namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] 
    /// Một dự án (cá nhân, môn học, công ty).
    /// </summary>
    public class CvProjectDto
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Bối cảnh dự án: cá nhân, môn học, công ty...</summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>Vai trò của người dùng trong dự án.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Thời gian, dạng chữ.</summary>
        public string Duration { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>Việc người dùng đảm nhận.</summary>
        public List<string> Responsibilities { get; set; } = new();

        /// <summary>Công nghệ sử dụng.</summary>
        public List<string> Technologies { get; set; } = new();
    }
}
