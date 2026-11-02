// Tìm thư mục bộ đề thật (MockviewAI/Data/InterviewQuestions) từ thư mục chạy test, để test dùng đúng file JSON nhóm đang sửa.

using MockviewAI.Services.InterviewQuestions.Implementations;

namespace MockviewAI.Tests
{
    internal static class TestData
    {
        private static readonly Lazy<JsonQuestionBankStore> LazyStore = new(() => new JsonQuestionBankStore(BankFolder()));

        public static JsonQuestionBankStore Store => LazyStore.Value;

        public static string BankFolder()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "MockviewAI", "Data", "InterviewQuestions");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("Không tìm thấy MockviewAI/Data/InterviewQuestions từ " + AppContext.BaseDirectory);
        }
    }
}
