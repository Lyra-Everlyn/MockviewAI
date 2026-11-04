// [LIVE-MODE] Đợt 6 (03/11/2026): 2 chế độ luyện phỏng vấn (theo góp ý của thầy).
//   practice = Luyện tập: được xoá ghi âm, gõ chữ, nghe lại, nhắc 1 lần khi câu trả lời vô nghĩa (như trước).
//   live     = Thực chiến: nói 1 lần từ đầu đến cuối như phỏng vấn thật — có thời gian suy nghĩ, giới hạn thời gian
//              trả lời, không xoá / gõ / sửa, nghe lại câu hỏi 1 lần, bỏ qua hoặc vô nghĩa = SAI ngay.

namespace MockviewAI.Models
{
    public static class InterviewModes
    {
        public const string Practice = "practice";
        public const string Live = "live";

        // Luật Thực chiến (đổi số ở đây là đủ; giao diện đọc từ InterviewRoomViewModel)
        public const int LiveThinkSeconds = 15;        // thời gian suy nghĩ trước khi micro tự bật
        public const int LiveMaxAnswerSeconds = 120;   // tối đa thời gian trả lời, hết giờ tự gửi
        public const int LiveMaxReplays = 1;           // số lần được nghe lại câu hỏi

        // Mã lạ / trống -> Luyện tập (an toàn: không bao giờ vô tình bật Thực chiến)
        public static string Normalize(string? mode) => mode == Live ? Live : Practice;

        public static bool IsLive(string? mode) => mode == Live;

        public static string DisplayName(string? mode) => IsLive(mode) ? "Thực chiến" : "Luyện tập";
    }
}
