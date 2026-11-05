// [FOLLOW-UP] Đợt 8 (03/11/2026): CÂU HỎI NỐI TIẾP (góp ý của thầy: câu hỏi có "dynamic" không).
// Cách A, KHÔNG phụ thuộc AI: mỗi ý cốt lõi trong bộ đề có sẵn một câu hỏi thêm. Ngay khi người dùng gửi câu trả lời,
// code chấm sơ bộ theo bộ đề (BankKeywordScorer, < 1 ms); câu trả lời chưa tới "điểm B" -> người phỏng vấn hỏi thêm
// về ý còn thiếu ĐẦU TIÊN. Câu trả lời thêm được gộp vào câu chính khi chấm nội dung (có cơ hội bổ sung ý).
// Đổi luật ở đây (số câu hỏi thêm tối đa).

using MockviewAI.Models.Entities;

namespace MockviewAI.Models
{
    public static class FollowUps
    {
        // Tối đa số câu hỏi thêm trong một buổi (mỗi câu chính tối đa 1 câu hỏi thêm) -> buổi không bị kéo quá dài
        public const int MaxPerSession = 3;

        // Thứ tự hỏi: theo số câu; câu hỏi thêm đứng ngay sau câu chính cùng số
        public static IEnumerable<SessionAnswer> InOrder(IEnumerable<SessionAnswer> answers) =>
            answers.OrderBy(a => a.OrderNo).ThenBy(a => a.IsFollowUp ? 1 : 0).ThenBy(a => a.Id);

        // Câu đang chờ trả lời (null = đã hết câu)
        public static SessionAnswer? Next(IEnumerable<SessionAnswer> answers) =>
            InOrder(answers).FirstOrDefault(a => a.AnsweredAt == null);

        // Chỉ các câu chính (dùng để đếm số câu, chấm điểm, dải băng tiến độ)
        public static IEnumerable<SessionAnswer> Main(IEnumerable<SessionAnswer> answers) =>
            answers.Where(a => !a.IsFollowUp);

        // Còn được hỏi thêm cho câu này không: câu chính thuộc bộ đề, chưa từng được hỏi thêm, chưa vượt giới hạn buổi
        public static bool CanAskAfter(SessionAnswer answered, IEnumerable<SessionAnswer> all)
        {
            if (answered.IsFollowUp || answered.QuestionId == null) return false;
            var list = all.ToList();
            if (list.Any(a => a.FollowUpOf == answered.QuestionId)) return false;
            return list.Count(a => a.IsFollowUp) < MaxPerSession;
        }
    }
}
