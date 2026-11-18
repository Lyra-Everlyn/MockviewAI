using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH]
    /// SpeechPromptBuilder
    /// -- Soạn gợi ý cho Whisper và prompt cho bước chuẩn hoá bản chép.
    /// -- Muốn chỉnh danh sách thuật ngữ hay cách Gemini sửa lỗi: chỉ sửa file này.
    /// -- Bản sửa 17/11: Whisper chỉ nhận câu hỏi (gợi ý NGẮN). Danh sách thuật ngữ dài làm Whisper "kéo" từ thường
    ///    sang thuật ngữ (vd: "Co-working" -> "Core Working" do có "ASP.NET Core") nên chuyển sang bước Gemini.
    /// </summary>
    public class SpeechPromptBuilder : ISpeechPromptBuilder
    {
        #region Whisper prompt
        private const int MaxQuestionChars = 300;

        /// <inheritdoc />
        public string BuildWhisperPrompt(string? question)
        {
            // Chỉ đưa câu hỏi (tiếng Việt có dấu, có dấu câu): giúp Whisper giữ đúng ngôn ngữ và cách chấm câu
            // mà không ép từ ngữ theo một danh sách cố định.
            return Clean(question, MaxQuestionChars);
        }
        #endregion

        #region Normalizer prompt
        // Thuật ngữ hay bị nghe sai khi nói tiếng Việt lẫn tiếng Anh — chỉ dùng làm GỢI Ý cho Gemini
        private static readonly string[] TechTerms =
        {
            "C#", ".NET", "ASP.NET Core", "MVC", "Entity Framework", "LINQ", "Java", "Spring Boot",
            "JavaScript", "TypeScript", "React", "Angular", "Vue", "Node.js", "Python", "PHP",
            "HTML", "CSS", "SQL", "MySQL", "SQL Server", "MongoDB", "Redis", "API", "REST", "GraphQL",
            "OOP", "SOLID", "Design Pattern", "Dependency Injection", "Interface", "Class", "Async/Await",
            "Git", "GitHub", "Docker", "CI/CD", "Unit Test", "Selenium", "Postman", "Jira",
            "Agile", "Scrum", "Sprint", "Backlog", "Use Case", "User Story", "BA", "QA", "Tester", "Deadline"
        };

        /// <inheritdoc />
        public string NormalizerSystemPrompt =>
@"Bạn là công cụ chuẩn hoá văn bản chuyển từ giọng nói (Speech-to-Text) của ứng viên trong buổi phỏng vấn bằng tiếng Việt.
Văn bản đầu vào thường nghe sai các từ tiếng Anh, tên riêng, thương hiệu, thuật ngữ và con số.

Nhiệm vụ:
1. Sửa từ bị nhận diện sai khi phát âm GẦN GIỐNG và ngữ cảnh cho thấy rõ ý đúng.
   Ví dụ: 'xi thăng' -> 'C#', 'đót nét' -> '.NET', 'ếch pi ai' -> 'API', 'sô lít' -> 'SOLID',
   'đốc cơ' -> 'Docker', 'gít háp' -> 'GitHub'.
2. Viết đúng chính tả gốc của thuật ngữ, thương hiệu, tên riêng tiếng Anh; viết số, ngày giờ, tiền tệ cho dễ đọc.
3. Thêm hoặc sửa dấu câu, viết hoa đầu câu.

Thuật ngữ IT thường gặp (chỉ để tham khảo khi sửa): " + string.Join(", ", TechTerms) + @".

QUY TẮC NGHIÊM NGẶT:
- CHỈ trả về đúng văn bản đã sửa, không lời chào, không giải thích, không đặt trong dấu ngoặc kép.
- KHÔNG thêm ý, không bớt ý, không trả lời thay ứng viên, không sửa câu trả lời sai về kiến thức thành đúng.
- KHÔNG thay một tên riêng / thương hiệu bằng một tên KHÁC có phát âm không giống.
  Không chắc thì giữ nguyên cách viết gần nhất với âm đã nghe.
- Giữ nguyên các con số và đơn vị người nói đã nói (2 triệu khác 2 tỷ).
- Câu hỏi chỉ là ngữ cảnh tham khảo, KHÔNG đưa nội dung câu hỏi vào kết quả.
- Nếu văn bản đã đúng, trả về y nguyên.";

        /// <inheritdoc />
        public string BuildNormalizerUserPrompt(string rawText, string? question)
        {
            var q = Clean(question, MaxQuestionChars);
            return (q.Length > 0 ? $"Câu hỏi phỏng vấn (chỉ để tham khảo): {q}\n\n" : string.Empty)
                 + $"Văn bản cần chuẩn hoá:\n{rawText.Trim()}";
        }
        #endregion

        #region Helpers
        /// <summary>Gộp khoảng trắng và cắt ngắn để prompt gọn, không vượt giới hạn.</summary>
        private static string Clean(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var collapsed = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return collapsed.Length <= max ? collapsed : collapsed[..max];
        }
        #endregion
    }
}
