namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [USER CV] 
    /// Một chứng chỉ / giải thưởng.
    /// </summary>
    public class CvCertificationDto
    {
        public string Title { get; set; } = string.Empty;

        /// <summary>Đơn vị cấp.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Năm nhận; null nếu CV không ghi.</summary>
        public string? Year { get; set; }
    }
}
