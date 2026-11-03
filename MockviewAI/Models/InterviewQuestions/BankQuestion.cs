// [QUESTION-BANK] Một câu hỏi trong bộ đề kèm "điểm B" (ý cốt lõi), các con đường đúng, lỗi hay gặp và câu trả lời mẫu.

namespace MockviewAI.Models.InterviewQuestions
{
    public class BankQuestion
    {
        public string Id { get; set; } = string.Empty;              // vd "dev-csharp-01"
        public string Interviewer { get; set; } = "nam";            // linh | nam | an
        public List<string> Positions { get; set; } = new();        // developer | tester | ba
        public List<string> Skills { get; set; } = new();           // chủ đề / kỹ năng của câu (để nhóm lọc, nối CV sau này)
        public string Level { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;

        // Phải đạt ít nhất số ý cốt lõi này mới được coi là "tới điểm B"
        public int MinCorePoints { get; set; } = 2;

        public List<CorePoint> CorePoints { get; set; } = new();
        public List<AnswerPath> Paths { get; set; } = new();
        public List<CommonMistake> Mistakes { get; set; } = new();

        // Chỉ dùng để kiểm chứng độ chính xác, KHÔNG gửi cho AI khi chấm thật
        public List<SampleAnswer> Samples { get; set; } = new();

        // Câu giới thiệu bản thân của vị trí (luôn là câu 1 của buổi)
        public bool IsIntro => Id.EndsWith("-gen-01", StringComparison.Ordinal);
    }
}
