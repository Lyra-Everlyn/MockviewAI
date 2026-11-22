using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH] 
    /// Soạn gợi ý cho Whisper và prompt cho bước chuẩn hoá / gộp hai bản chép. Chỉnh thuật ngữ hay cách Gemini sửa: chỉ sửa file này.
    /// </summary>
    public class SpeechPromptBuilder : ISpeechPromptBuilder
    {
        #region Whisper prompt
        private const int MaxQuestionChars = 300;
        private const int MaxBrowserChars = 4000;

        public string BuildWhisperPrompt(string? question) => Clean(question, MaxQuestionChars);

        // Ngắn và không dấu để không kéo Whisper sang tiếng Việt hay bịa thêm nội dung
        public string WhisperTermsPrompt => "MockviewAI, C#, .NET, ASP.NET Core, Docker, Redis, REST API.";
        #endregion

        #region Normalizer prompt
        // Chỉ là gợi ý cho Gemini; danh sách dài đưa vào Whisper sẽ kéo từ thường thành thuật ngữ
        private static readonly string[] TechTerms =
        {
            "MockviewAI", "C#", ".NET", "ASP.NET Core", "MVC", "Entity Framework", "LINQ", "Java", "Spring Boot",
            "JavaScript", "TypeScript", "React", "Angular", "Vue", "Node.js", "Python", "PHP",
            "HTML", "CSS", "SQL", "MySQL", "SQL Server", "MongoDB", "Redis", "API", "REST", "GraphQL",
            "OOP", "SOLID", "Design Pattern", "Dependency Injection", "Interface", "Class", "Async/Await",
            "Git", "GitHub", "Docker", "CI/CD", "Unit Test", "Selenium", "Postman", "Jira",
            "Agile", "Scrum", "Sprint", "Backlog", "Use Case", "User Story", "BA", "QA", "Tester", "Deadline",
            "Speech-to-Text", "Software Engineer", "Backend", "Frontend"
        };

        public string NormalizerSystemPrompt =>
@"Bạn là công cụ chuẩn hoá văn bản chuyển từ giọng nói (Speech-to-Text) của ứng viên trong buổi phỏng vấn IT.
Ứng viên có thể trả lời bằng tiếng Việt, tiếng Anh, hoặc tiếng Việt chen thuật ngữ tiếng Anh.

Đầu vào có thể gồm HAI bản chép của CÙNG một đoạn ghi âm:
- BẢN A (Whisper): thường đúng thuật ngữ và dấu câu, nhưng đôi khi dịch nhầm sang ngôn ngữ khác hoặc bịa thêm câu.
- BẢN B (trình duyệt): thường đúng ngôn ngữ và đủ câu, nhưng hay nghe sai thuật ngữ, tên riêng, thiếu dấu câu.
Chỉ có BẢN A thì chỉ sửa BẢN A.

Nhiệm vụ:
1. Gộp thành MỘT bản đúng nhất với lời người nói. Hai bản khác ngôn ngữ thì tin ngôn ngữ của BẢN B.
   Câu chỉ có trong một bản: giữ nếu hợp ngữ cảnh, bỏ nếu rõ ràng là bịa thêm.
2. Sửa từ nhận diện sai khi phát âm GẦN GIỐNG và ngữ cảnh cho thấy rõ ý đúng.
   Ví dụ: 'xi thăng' -> 'C#', 'đót nét' -> '.NET', 'ếch pi ai' -> 'API', 'sô lít' -> 'SOLID',
   'đốc cơ' -> 'Docker', 'gít háp' -> 'GitHub', 'Mock view AI' / 'Mockpy.ai' -> 'MockviewAI'.
3. Viết đúng chính tả gốc của thuật ngữ, thương hiệu, tên riêng tiếng Anh; viết số, ngày giờ, tiền tệ cho dễ đọc.
4. Thêm hoặc sửa dấu câu, viết hoa đầu câu.

Thuật ngữ IT thường gặp (chỉ để tham khảo khi sửa): " + string.Join(", ", TechTerms) + @".

QUY TẮC NGHIÊM NGẶT:
- GIỮ NGUYÊN NGÔN NGỮ người nói đã dùng. Câu tiếng Anh giữ tiếng Anh, câu tiếng Việt giữ tiếng Việt.
  TUYỆT ĐỐI KHÔNG dịch, kể cả khi câu hỏi phỏng vấn viết bằng ngôn ngữ khác.
- Chỉ dùng nội dung có trong ít nhất một bản. Không thêm ý, không trả lời thay ứng viên,
  không sửa câu trả lời sai về kiến thức thành đúng.
- Trả về TOÀN BỘ lời nói, không rút gọn, không tóm tắt.
- CHỈ trả về văn bản kết quả, không lời chào, không giải thích, không ghi 'BẢN A'/'BẢN B', không đặt trong dấu ngoặc kép.
- KHÔNG thay một tên riêng / thương hiệu bằng một tên KHÁC có phát âm không giống.
- Giữ nguyên các con số và đơn vị người nói đã nói (2 triệu khác 2 tỷ).
- Câu hỏi chỉ là ngữ cảnh tham khảo, KHÔNG đưa nội dung câu hỏi vào kết quả.";

        public string BuildNormalizerUserPrompt(string rawText, string? browserText, string? question)
        {
            var q = Clean(question, MaxQuestionChars);
            var b = Clean(browserText, MaxBrowserChars);
            return (q.Length > 0 ? $"Câu hỏi phỏng vấn (chỉ để tham khảo, không dịch theo): {q}\n\n" : string.Empty)
                 + $"BẢN A (Whisper):\n{rawText.Trim()}"
                 + (b.Length > 0 ? $"\n\nBẢN B (trình duyệt):\n{b}" : string.Empty);
        }
        #endregion

        #region Helpers
        /// <summary>Gộp khoảng trắng và cắt ngắn để prompt gọn.</summary>
        private static string Clean(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var collapsed = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return collapsed.Length <= max ? collapsed : collapsed[..max];
        }
        #endregion
    }
}
