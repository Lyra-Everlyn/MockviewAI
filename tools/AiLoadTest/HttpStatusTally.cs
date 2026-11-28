using System.Collections.Concurrent;
using System.Text;

namespace AiLoadTest
{
    /// <summary>Đếm mã HTTP mà Gemini / Groq trả về theo từng model (mỗi lần gửi thật, kể cả lần xoay khoá gửi lại).</summary>
    public class HttpStatusTally
    {
        private readonly ConcurrentDictionary<string, int> _counts = new();

        public void Add(string provider, string model, int status) =>
            _counts.AddOrUpdate($"{provider,-7} {model,-28} HTTP {status}", 1, (_, n) => n + 1);

        public string Report()
        {
            var sb = new StringBuilder();
            foreach (var kv in _counts.OrderBy(k => k.Key)) sb.AppendLine($"{kv.Key}  x{kv.Value}");
            return sb.Length == 0 ? "(không có lệnh gọi nào)\n" : sb.ToString();
        }
    }
}
