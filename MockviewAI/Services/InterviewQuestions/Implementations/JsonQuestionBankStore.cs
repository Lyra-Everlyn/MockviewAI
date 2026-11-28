using System.Text.Json;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Đọc question-bank.v1.json (+ question-bank.it-roles.v1.json nếu có) một lần khi khởi động (Singleton).
    /// Sửa bộ đề = sửa file JSON rồi chạy tools/question-bank/validate_bank.py (hoặc dotnet test), không cần sửa code.
    /// </summary>
    public class JsonQuestionBankStore : IQuestionBankStore
    {
        #region Fields
        public const string BankFileName = "question-bank.v1.json";

        /// <summary>
        /// [IT-ROLES] Câu kỹ thuật cho các vị trí IT ngoài developer / tester / ba (Nam hỏi). Không có file thì bỏ qua
        /// (các vị trí đó quay về AI soạn câu theo vị trí như trước). Phiên bản bộ đề (Version) vẫn lấy từ file chính.
        /// </summary>
        public const string RoleBankFileName = "question-bank.it-roles.v1.json";

        /// <summary>camelCase trong JSON &lt;-&gt; PascalCase trong C#.</summary>
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly Dictionary<string, BankQuestion> _byId;
        #endregion

        #region Constructor
        /// <param name="folder">Thư mục chứa file JSON, vd &lt;ContentRoot&gt;/Data/InterviewQuestions.</param>
        /// <exception cref="InvalidOperationException">Thiếu file, file rỗng / sai định dạng, hoặc trùng mã câu.</exception>
        public JsonQuestionBankStore(string folder)
        {
            string path = Path.Combine(folder, BankFileName);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Không tìm thấy file bộ đề: {path}");

            var bank = Load(path);
            var questions = new List<BankQuestion>(bank.Questions);

            string rolePath = Path.Combine(folder, RoleBankFileName);
            if (File.Exists(rolePath)) questions.AddRange(Load(rolePath).Questions);

            Version = bank.Version;
            Questions = questions;

            // ToDictionary báo lỗi ngay nếu trùng mã câu (kể cả giữa 2 file) -> phát hiện sớm khi sửa JSON sai
            _byId = questions.ToDictionary(q => q.Id, StringComparer.Ordinal);
        }

        private static QuestionBankFile Load(string path) =>
            JsonSerializer.Deserialize<QuestionBankFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"File bộ đề rỗng hoặc sai định dạng: {path}");
        #endregion

        #region IQuestionBankStore
        public string Version { get; }
        public IReadOnlyList<BankQuestion> Questions { get; }

        public BankQuestion? Find(string? questionId) =>
            questionId != null && _byId.TryGetValue(questionId, out var q) ? q : null;

        public IReadOnlyList<BankQuestion> ForInterview(string interviewerKey, string positionKey) =>
            Questions.Where(q => q.Interviewer == interviewerKey && q.Positions.Contains(positionKey)).ToList();
        #endregion
    }
}
