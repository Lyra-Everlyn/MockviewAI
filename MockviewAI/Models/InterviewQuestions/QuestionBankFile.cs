namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Toàn bộ file Data/InterviewQuestions/question-bank.v1.json.
    /// </summary>
    public class QuestionBankFile
    {
        /// <summary>Phiên bản bộ đề, lưu vào InterviewSession.QuestionBankVersion.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Định nghĩa 4 loại câu trả lời.</summary>
        public List<CategoryDefinition> Categories { get; set; } = new();

        public List<BankQuestion> Questions { get; set; } = new();
    }
}
