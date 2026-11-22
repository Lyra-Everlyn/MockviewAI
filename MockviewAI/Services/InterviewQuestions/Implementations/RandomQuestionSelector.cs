using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Câu 1 luôn là câu giới thiệu (*-gen-01), các câu còn lại rút ngẫu nhiên
    /// trong bộ của người phỏng vấn + vị trí. Bộ đề nhiều hơn số câu của buổi nên mỗi lần luyện gặp câu khác nhau.
    /// </summary>
    public class RandomQuestionSelector : IQuestionSelector
    {
        #region Fields
        private readonly IQuestionBankStore _store;
        private readonly Random _random;
        #endregion

        #region Constructor
        /// <param name="random">Truyền Random có seed khi viết test để kết quả lặp lại được; mặc định Random.Shared (an toàn đa luồng).</param>
        public RandomQuestionSelector(IQuestionBankStore store, Random? random = null)
        {
            _store = store;
            _random = random ?? Random.Shared;
        }
        #endregion

        #region IQuestionSelector
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
        #endregion
    }
}
