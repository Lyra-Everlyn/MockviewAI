// [QUESTION-BANK] Một lỗi hay gặp: câu trả lời khớp lỗi này thì xếp loại "sai".

namespace MockviewAI.Models.InterviewQuestions
{
    public class CommonMistake
    {
        public string Id { get; set; } = string.Empty;          // loi1, loi2, ...
        public string Summary { get; set; } = string.Empty;
        public string Why { get; set; } = string.Empty;

        // Chấm SƠ BỘ khi AI lỗi: cụm từ đặc trưng của lỗi này (bị phủ định như "không nuốt lỗi" thì không tính)
        public List<string> Signals { get; set; } = new();
    }
}
