using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [IT-ROLES]
    /// Đọc CV và gợi ý một vị trí ngành IT phù hợp nhất trong ItRoleCatalog. Không bao giờ ném lỗi: AI lỗi thì đoán bằng từ khoá.
    /// </summary>
    public interface IRoleSuggester
    {
        /// <param name="cvJson">CV trên trình duyệt (JSON), được làm sạch và rút gọn trước khi gửi AI.</param>
        /// <returns>null nếu CV rỗng / hỏng.</returns>
        Task<RoleSuggestionDto?> SuggestAsync(string? cvJson, CancellationToken ct = default);
    }
}
