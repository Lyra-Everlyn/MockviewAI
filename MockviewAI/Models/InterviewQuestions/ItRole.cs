namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [IT-ROLES]
    /// Một vị trí ứng tuyển ngành IT. Câu kỹ năng mềm (Linh, An) dùng chung mọi vị trí;
    /// câu kỹ thuật (Nam) lấy từ bộ đề: câu riêng của vị trí (question-bank.it-roles), hoặc của vị trí gốc nếu HasTechBank; không có thì AI soạn theo Focus.
    /// </summary>
    public class ItRole
    {
        /// <summary>Khoá gửi từ form, vd "devops".</summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>Tên hiển thị, lưu vào InterviewSession.Position.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Nhóm hiển thị ở trang Tạo buổi.</summary>
        public string Group { get; init; } = string.Empty;

        /// <summary>Vị trí gốc trong bộ đề (developer | tester | ba) để lấy câu giới thiệu, câu kỹ năng mềm và câu dự phòng.</summary>
        public string BankBase { get; init; } = "developer";

        /// <summary>Câu kỹ thuật của vị trí gốc (BankBase) đã hợp vị trí này (backend, fullstack dùng câu của developer).</summary>
        public bool HasTechBank { get; init; }

        /// <summary>Mảng kiến thức chính, đưa vào prompt khi AI soạn câu kỹ thuật.</summary>
        public string Focus { get; init; } = string.Empty;

        /// <summary>Từ khoá (đã bỏ dấu, chữ thường) để đoán vị trí từ CV khi AI không trả lời.</summary>
        public string[] Keywords { get; init; } = Array.Empty<string>();
    }
}
