using MockviewAI.Models.Entities;

namespace MockviewAI.Models
{
    /// <summary>
    /// [FOLLOW-UP] 
    /// Luật câu hỏi nối tiếp: thứ tự hỏi, câu đang chờ, giới hạn số câu hỏi thêm mỗi buổi.
    /// Câu trả lời thêm được gộp vào câu chính khi chấm nội dung.
    /// </summary>
    public static class FollowUps
    {
        /// <summary>Tối đa câu hỏi thêm trong một buổi (mỗi câu chính tối đa 1) để buổi không kéo quá dài.</summary>
        public const int MaxPerSession = 3;

        /// <summary>Thứ tự hỏi theo số câu; câu hỏi thêm đứng ngay sau câu chính cùng số.</summary>
        public static IEnumerable<SessionAnswer> InOrder(IEnumerable<SessionAnswer> answers) =>
            answers.OrderBy(a => a.OrderNo).ThenBy(a => a.IsFollowUp ? 1 : 0).ThenBy(a => a.Id);

        /// <summary>Câu đang chờ trả lời; null = đã hết câu.</summary>
        public static SessionAnswer? Next(IEnumerable<SessionAnswer> answers) =>
            InOrder(answers).FirstOrDefault(a => a.AnsweredAt == null);

        /// <summary>Chỉ các câu chính (đếm số câu, chấm điểm, dải băng tiến độ).</summary>
        public static IEnumerable<SessionAnswer> Main(IEnumerable<SessionAnswer> answers) =>
            answers.Where(a => !a.IsFollowUp);

        /// <summary>Còn được hỏi thêm sau câu này không: câu chính thuộc bộ đề, chưa từng hỏi thêm, chưa vượt giới hạn buổi.</summary>
        public static bool CanAskAfter(SessionAnswer answered, IEnumerable<SessionAnswer> all)
        {
            if (answered.IsFollowUp || answered.QuestionId == null) return false;
            var list = all.ToList();
            if (list.Any(a => a.FollowUpOf == answered.QuestionId)) return false;
            return list.Count(a => a.IsFollowUp) < MaxPerSession;
        }
    }
}
