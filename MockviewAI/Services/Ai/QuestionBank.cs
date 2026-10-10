// [AI-INTERVIEW] Ngân hàng câu hỏi mẫu và thông tin 3 người phỏng vấn.
// Nhóm có thể sửa/thêm câu hỏi ngay trong file này. {position} sẽ được thay bằng vị trí ứng tuyển.

namespace MockviewAI.Services.Ai
{
    public static class QuestionBank
    {
        // Vị trí ứng tuyển cho phép (khóa gửi từ form -> tên hiển thị)
        public static readonly Dictionary<string, string> Positions = new()
        {
            ["developer"] = "Lập trình viên",
            ["tester"] = "Kiểm thử viên (Tester)",
            ["ba"] = "Chuyên viên Phân tích (BA)"
        };

        // Người phỏng vấn: khóa -> (tên, vai trò)
        public static readonly Dictionary<string, (string Name, string Role)> Interviewers = new()
        {
            ["linh"] = ("Linh", "Nhân sự"),
            ["nam"] = ("Nam", "Kỹ thuật"),
            ["an"] = ("An", "Quản lý")
        };

        // Mỗi người phỏng vấn có 10 câu. Câu 1 luôn là "giới thiệu bản thân".
        private static readonly Dictionary<string, string[]> Questions = new()
        {
            ["linh"] = new[]
            {
                "Hãy giới thiệu ngắn về bản thân bạn.",
                "Vì sao bạn muốn ứng tuyển vị trí {position}?",
                "Điểm mạnh lớn nhất của bạn là gì?",
                "Điểm yếu của bạn là gì và bạn đang cải thiện thế nào?",
                "Hãy kể về một lần bạn làm việc nhóm và vai trò của bạn.",
                "Bạn xử lý thế nào khi có bất đồng với đồng đội?",
                "Hãy kể về một lần bạn thất bại và bài học rút ra.",
                "Bạn học kỹ năng mới như thế nào?",
                "Bạn mong muốn gì ở công việc đầu tiên?",
                "Bạn có câu hỏi nào cho chúng tôi không?"
            },
            ["nam"] = new[]
            {
                "Hãy giới thiệu ngắn về bản thân và kỹ năng chuyên môn của bạn.",
                "Bạn đã làm dự án nào liên quan đến vị trí {position}? Hãy mô tả ngắn.",
                "Công nghệ hoặc công cụ nào bạn dùng thành thạo nhất? Vì sao?",
                "Hãy kể về một lỗi khó mà bạn từng gặp và cách bạn xử lý.",
                "Bạn kiểm tra chất lượng công việc của mình bằng cách nào?",
                "Bạn dùng Git hoặc công cụ quản lý phiên bản trong nhóm như thế nào?",
                "Làm sao bạn giải thích một vấn đề kỹ thuật cho người không chuyên?",
                "Bạn cập nhật kiến thức công nghệ mới bằng cách nào?",
                "Nếu được cải thiện một thứ trong dự án gần nhất, bạn chọn gì?",
                "Bạn muốn phát triển thêm kỹ năng kỹ thuật nào trong 1 năm tới?"
            },
            ["an"] = new[]
            {
                "Hãy giới thiệu ngắn về bản thân và mục tiêu nghề nghiệp của bạn.",
                "Nếu deadline gấp mà công việc chưa xong, bạn sẽ làm gì?",
                "Bạn sắp xếp thứ tự ưu tiên khi có nhiều việc cùng lúc thế nào?",
                "Đồng đội không làm đúng phần việc của họ, bạn xử lý ra sao?",
                "Bạn nhận góp ý phê bình từ quản lý như thế nào? Cho ví dụ.",
                "Nếu bạn không đồng ý với quyết định của quản lý, bạn làm gì?",
                "Hãy kể về lần bạn chủ động đề xuất một ý tưởng.",
                "Bạn làm việc dưới áp lực như thế nào?",
                "Sau 3 năm bạn muốn mình ở vị trí nào?",
                "Điều gì khiến bạn chọn công ty của chúng tôi?"
            }
        };

        // Lấy N câu đầu tiên của người phỏng vấn, thay {position} bằng tên vị trí
        public static List<string> GetQuestions(string interviewerKey, string positionName, int count)
        {
            var all = Questions.TryGetValue(interviewerKey, out var list) ? list : Questions["linh"];
            return all.Take(count)
                      .Select(q => q.Replace("{position}", positionName))
                      .ToList();
        }
    }
}
