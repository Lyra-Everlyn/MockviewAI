// [AI-INTERVIEW] Soạn email kết quả buổi luyện gửi cho người dùng (chỉ khi họ tick đồng ý ở trang Tạo buổi luyện).
// Mọi chữ do người dùng/AI tạo ra đều được HTML-encode trước khi đưa vào email (chống chèn mã).
// Nếu điểm thấp hoặc dấu hiệu lo lắng cao: thêm lời hỏi thăm và đề nghị hỗ trợ.

using System.Globalization;
using System.Net;
using System.Text;

namespace MockviewAI.Services.Emails
{
    public class ResultEmailModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }
        public int TotalScore { get; set; }
        public int? ContentScore { get; set; }       // null: AI lỗi, chấm dự phòng
        public int DeliveryScore { get; set; }
        public int AnxietyIndicator { get; set; }    // 0 = chưa đủ dữ liệu
        public List<string> AnxietyReasons { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
    }

    public static class ResultEmailBuilder
    {
        private const int LowScore = 50;
        private const int HighAnxiety = 7;

        public static string BuildSubject(ResultEmailModel m) =>
            $"[MockviewAI] Kết quả buổi luyện phỏng vấn {m.Position} ({Date(m)})";

        public static bool NeedsSupport(ResultEmailModel m) =>
            m.TotalScore < LowScore || m.AnxietyIndicator >= HighAnxiety;

        public static string BuildHtml(ResultEmailModel m)
        {
            string name = string.IsNullOrWhiteSpace(m.FirstName) ? "bạn" : m.FirstName.Trim();
            var sb = new StringBuilder();

            sb.Append("<div style=\"font-family:'Segoe UI',Arial,sans-serif;max-width:600px;margin:0 auto;color:#1f2a44;line-height:1.6\">");
            sb.Append("<div style=\"background:#2949ff;color:#fff;padding:20px 24px;border-radius:12px 12px 0 0\">");
            sb.Append("<div style=\"font-size:20px;font-weight:700\">MockviewAI</div>");
            sb.Append("<div style=\"opacity:.9;font-size:14px\">Kết quả buổi luyện phỏng vấn thử</div></div>");
            sb.Append("<div style=\"border:1px solid #e3e8f5;border-top:0;padding:24px;border-radius:0 0 12px 12px\">");

            sb.Append($"<p>Chào {E(name)},</p>");
            sb.Append($"<p>Cảm ơn bạn đã hoàn thành buổi luyện phỏng vấn vị trí <strong>{E(m.Position)}</strong> " +
                      $"với người phỏng vấn {E(m.InterviewerName)} ngày {Date(m)} ({m.QuestionCount} câu). " +
                      "Dưới đây là kết quả tham khảo của bạn.</p>");

            // Điểm
            sb.Append("<table style=\"width:100%;border-collapse:collapse;margin:16px 0\">");
            sb.Append(Row("Điểm tổng (tham khảo)", $"<strong style=\"font-size:20px;color:#2949ff\">{m.TotalScore}/100</strong>"));
            sb.Append(Row("Nội dung câu trả lời", m.ContentScore is int c ? $"{c}/100" : "Chưa có (AI đang bận)"));
            sb.Append(Row("Cách nói", $"{m.DeliveryScore}/100"));
            if (m.AnxietyIndicator > 0)
                sb.Append(Row("Dấu hiệu lo lắng (ước tính)", $"{m.AnxietyIndicator}/10 &middot; {E(LevelText(m.AnxietyIndicator))}"));
            sb.Append("</table>");

            if (m.AnxietyIndicator > 0)
            {
                string reasons = m.AnxietyReasons.Count > 0 ? " Một số dấu hiệu hệ thống ghi nhận: " + E(string.Join("; ", m.AnxietyReasons)) + "." : "";
                sb.Append("<p style=\"font-size:13px;color:#55607a\">Chỉ số lo lắng được ước tính tự động từ cách nói của bạn " +
                          "(từ đệm, khoảng lặng, tốc độ...), không phải đánh giá y tế." + reasons + "</p>");
            }

            if (!string.IsNullOrWhiteSpace(m.Summary))
                sb.Append($"<h3 style=\"margin:20px 0 6px\">Nhận xét chung</h3><p>{E(m.Summary)}</p>");

            sb.Append(ListSection("Điểm mạnh", m.Strengths));
            sb.Append(ListSection("Nên cải thiện", m.Improvements));
            sb.Append(ListSection("Gợi ý về cách nói", m.DeliveryTips));

            if (NeedsSupport(m))
            {
                sb.Append("<div style=\"background:#fff8e6;border:1px solid #f5d98b;border-radius:10px;padding:14px 16px;margin:20px 0\">");
                sb.Append("<strong>Chúng tôi có thể hỗ trợ bạn thêm.</strong>");
                sb.Append("<p style=\"margin:6px 0 0\">Kết quả lần này cho thấy bạn vẫn còn nhiều thử thách" +
                          (m.AnxietyIndicator >= HighAnxiety ? " và có dấu hiệu khá căng thẳng khi trả lời" : "") +
                          ". Điều này rất bình thường ở giai đoạn đầu luyện phỏng vấn. " +
                          "Bạn có cần chúng tôi hỗ trợ không ạ? Chỉ cần trả lời email này, đội ngũ MockviewAI sẽ liên hệ " +
                          "và gợi ý cách luyện phù hợp với bạn.</p>");
                if (m.AnxietyIndicator >= HighAnxiety)
                    sb.Append("<p style=\"margin:6px 0 0;font-size:13px;color:#55607a\">Nếu cảm giác lo lắng kéo dài hoặc ảnh hưởng đến sinh hoạt, " +
                              "bạn nên trao đổi với người thân, giảng viên hoặc chuyên gia tư vấn.</p>");
                sb.Append("</div>");
            }
            else
            {
                sb.Append("<p>Bạn đang làm rất tốt. Hãy tiếp tục luyện đều đặn để tự tin hơn trong buổi phỏng vấn thật nhé.</p>");
            }

            sb.Append("<p style=\"margin-top:24px\">Trân trọng,<br>Đội ngũ MockviewAI</p>");
            sb.Append("<hr style=\"border:0;border-top:1px solid #e3e8f5;margin:20px 0\">");
            sb.Append("<p style=\"font-size:12px;color:#8a93a8\">Bạn nhận email này vì đã chọn \"Gửi kết quả về email\" khi tạo buổi luyện. " +
                      "Điểm số chỉ mang tính tham khảo. Email chỉ được gửi cho những buổi luyện bạn đã đồng ý nhận.</p>");
            sb.Append("</div></div>");

            return sb.ToString();
        }

        private static string Date(ResultEmailModel m) => m.StartedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        private static string LevelText(int level) => level <= 3 ? "Thấp" : level <= 6 ? "Trung bình" : "Cao";

        private static string Row(string label, string valueHtml) =>
            "<tr><td style=\"padding:8px 0;border-bottom:1px solid #eef1fa;color:#55607a\">" + label +
            "</td><td style=\"padding:8px 0;border-bottom:1px solid #eef1fa;text-align:right\">" + valueHtml + "</td></tr>";

        private static string ListSection(string title, List<string> items)
        {
            if (items == null || items.Count == 0) return string.Empty;
            var sb = new StringBuilder($"<h3 style=\"margin:20px 0 6px\">{E(title)}</h3><ul style=\"padding-left:20px;margin:0\">");
            foreach (var item in items) sb.Append("<li>").Append(E(item)).Append("</li>");
            sb.Append("</ul>");
            return sb.ToString();
        }

        private static string E(string text) => WebUtility.HtmlEncode(text ?? string.Empty);
    }
}
