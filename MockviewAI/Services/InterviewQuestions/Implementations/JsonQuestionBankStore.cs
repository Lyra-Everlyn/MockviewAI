// [QUESTION-BANK] Đọc question-bank.v1.json MỘT LẦN khi khởi động (đăng ký Singleton).
// Nhóm sửa bộ đề bằng cách sửa file JSON rồi chạy tools/question-bank/validate_bank.py (hoặc dotnet test); không cần sửa code.

using System.Text.Json;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    public class JsonQuestionBankStore : IQuestionBankStore
    {
        public const string BankFileName = "question-bank.v1.json";

        // Web defaults: tên thuộc tính camelCase trong JSON <-> PascalCase trong C#
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly Dictionary<string, BankQuestion> _byId;

        public string Version { get; }
        public IReadOnlyList<BankQuestion> Questions { get; }

        // folder: thư mục chứa file JSON (vd <ContentRoot>/Data/InterviewQuestions)
        public JsonQuestionBankStore(string folder)
        {
            string path = Path.Combine(folder, BankFileName);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Không tìm thấy file bộ đề: {path}");

            var bank = JsonSerializer.Deserialize<QuestionBankFile>(File.ReadAllText(path), JsonOptions)
                       ?? throw new InvalidOperationException($"File bộ đề rỗng hoặc sai định dạng: {path}");

            Version = bank.Version;
            Questions = bank.Questions;
            // ToDictionary báo lỗi ngay nếu trùng mã câu -> phát hiện sớm khi sửa JSON sai
            _byId = bank.Questions.ToDictionary(q => q.Id, StringComparer.Ordinal);
        }

        public BankQuestion? Find(string? questionId) =>
            questionId != null && _byId.TryGetValue(questionId, out var q) ? q : null;

        public IReadOnlyList<BankQuestion> ForInterview(string interviewerKey, string positionKey) =>
            Questions.Where(q => q.Interviewer == interviewerKey && q.Positions.Contains(positionKey)).ToList();
    }
}
