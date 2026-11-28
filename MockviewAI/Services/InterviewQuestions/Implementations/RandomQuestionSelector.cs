using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Câu 1 luôn là câu giới thiệu (*-gen-01), các câu còn lại rút ngẫu nhiên
    /// trong bộ của người phỏng vấn + vị trí. Bộ đề nhiều hơn số câu của buổi nên mỗi lần luyện gặp câu khác nhau.
    /// [IT-ROLES] Vị trí mới (DevOps, Game...): câu riêng của vị trí trước, thiếu thì bù câu của vị trí gốc.
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
        public List<BankQuestion> Select(string interviewerKey, string positionKey, int count, string? basePositionKey = null)
        {
            var own = _store.ForInterview(interviewerKey, positionKey);
            var basePool = basePositionKey != null && basePositionKey != positionKey
                ? _store.ForInterview(interviewerKey, basePositionKey).Where(q => !own.Contains(q)).ToList()
                : new List<BankQuestion>();
            var chosen = new List<BankQuestion>();
            if ((own.Count == 0 && basePool.Count == 0) || count <= 0) return chosen;

            // Câu giới thiệu: của vị trí nếu có, không thì của vị trí gốc
            var intro = own.FirstOrDefault(q => q.IsIntro) ?? basePool.FirstOrDefault(q => q.IsIntro);
            if (intro != null) chosen.Add(intro);

            // [IT-ROLES] Câu riêng của vị trí trước (trộn), thiếu mới bù câu vị trí gốc (trộn)
            chosen.AddRange(Shuffle(own.Where(q => q != intro).ToList()).Take(count - chosen.Count));
            chosen.AddRange(Shuffle(basePool.Where(q => q != intro).ToList()).Take(count - chosen.Count));
            return chosen;
        }

        /// <summary>Trộn Fisher-Yates.</summary>
        private List<BankQuestion> Shuffle(List<BankQuestion> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
        #endregion
    }
}
