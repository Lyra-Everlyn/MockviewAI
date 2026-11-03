// [QUESTION-BANK] Toàn bộ file question-bank.v1.json: phiên bản, 4 loại câu trả lời, danh sách câu hỏi.

namespace MockviewAI.Models.InterviewQuestions
{
    public class QuestionBankFile
    {
        public string Version { get; set; } = string.Empty;
        public List<CategoryDefinition> Categories { get; set; } = new();
        public List<BankQuestion> Questions { get; set; } = new();
    }
}
