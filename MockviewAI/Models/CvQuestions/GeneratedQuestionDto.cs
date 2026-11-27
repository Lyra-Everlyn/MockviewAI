using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Một câu trong JSON người soạn trả về. Không có Id / Interviewer: hai trường này do code gán.
    /// </summary>
    public class GeneratedQuestionDto
    {
        public string? Question { get; set; }
        public string? CvEvidence { get; set; }
        public List<string>? Skills { get; set; }
        public string? Level { get; set; }
        public int MinCorePoints { get; set; }
        public List<CorePoint>? CorePoints { get; set; }
        public List<AnswerPath>? Paths { get; set; }
        public List<CommonMistake>? Mistakes { get; set; }
    }
}
