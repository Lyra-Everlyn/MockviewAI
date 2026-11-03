// [QUESTION-BANK] Chọn câu hỏi: câu 1 luôn là câu giới thiệu (*-gen-01), các câu còn lại rút NGẪU NHIÊN
// trong bộ của người phỏng vấn + vị trí. Bộ đề nhiều hơn số câu của buổi -> mỗi lần luyện gặp câu khác nhau.

using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    public class RandomQuestionSelector : IQuestionSelector
    {
        private readonly IQuestionBankStore _store;
        private readonly Random _random;

        // random: truyền Random có seed khi viết test để kết quả lặp lại được
        public RandomQuestionSelector(IQuestionBankStore store, Random? random = null)
        {
            _store = store;
            _random = random ?? Random.Shared;   // Random.Shared an toàn khi nhiều request cùng lúc
        }

        public List<BankQuestion> Select(string interviewerKey, string positionKey, int count)
        {
            var pool = _store.ForInterview(interviewerKey, positionKey);
            var chosen = new List<BankQuestion>();
            if (pool.Count == 0 || count <= 0) return chosen;

            var intro = pool.FirstOrDefault(q => q.IsIntro);
            if (intro != null) chosen.Add(intro);

            var rest = pool.Where(q => q != intro).ToList();
            // Trộn Fisher-Yates rồi lấy đủ số câu còn thiếu
            for (int i = rest.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (rest[i], rest[j]) = (rest[j], rest[i]);
            }
            chosen.AddRange(rest.Take(count - chosen.Count));
            return chosen;
        }
    }
}
