namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Số đếm của một lần soạn câu theo CV, chỉ để ghi log (phục vụ báo cáo nghiên cứu), không chứa nội dung CV.
    /// </summary>
    public class CvQuestionPipelineStats
    {
        public int Generated { get; set; }
        public int RuleRejected { get; set; }
        public int Passed { get; set; }
        public int FixRequested { get; set; }
        public int FixCalls { get; set; }
        public int Fixed { get; set; }
        public int Rejected { get; set; }
    }
}
