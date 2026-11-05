// [VOICE] Đợt 7 (03/11/2026): ngưỡng + nhãn ÂM LƯỢNG / GIỌNG ĐIỆU phía server.
// PHẢI KHỚP với wwwroot/js/VoiceAnalysis.js (QUIET_DB, LOUD_DB, MONOTONE_ST, LIVELY_ST).
// Số đo do trình duyệt gửi lên (đo bằng Web Audio, không dùng AI); server chỉ kiểm khoảng hợp lệ rồi lưu.

namespace MockviewAI.Models
{
    public static class VoiceMetrics
    {
        public const double QuietDb = -38;      // dBFS: nhỏ hơn = khó nghe
        public const double LoudDb = -10;       // lớn hơn = dễ rè
        public const double MonotoneSt = 1.5;   // nửa cung: nhỏ hơn = giọng đều đều
        public const double LivelySt = 6;

        public static string LevelLabel(double? db) =>
            db == null ? "—" : db < QuietDb ? "Nhỏ" : db > LoudDb ? "Quá to" : "Vừa";

        public static string ToneLabel(double? semitones) =>
            semitones == null ? "—" : semitones < MonotoneSt ? "Đều đều" : semitones > LivelySt ? "Nhấn nhá nhiều" : "Tự nhiên";

        // Số liệu từ trình duyệt: ngoài khoảng hợp lý -> bỏ (null), không tin dữ liệu gửi lên
        public static double? CleanLevel(double? db) => db is >= -100 and <= 0 ? Math.Round(db.Value, 1) : null;
        public static double? CleanRatio(double? r) => r is >= 0 and <= 1 ? Math.Round(r.Value, 2) : null;
        public static double? CleanPitch(double? st) => st is >= 0 and <= 24 ? Math.Round(st.Value, 1) : null;

        // Một dòng mô tả cho trang Xem lại (null = câu không có số đo, vd gõ chữ)
        public static string? Describe(double? db, double? quietRatio, double? semitones)
        {
            if (db == null && quietRatio == null) return null;
            string quiet = quietRatio == null ? "" : $" · nói nhỏ {quietRatio.Value * 100:0}% thời gian";
            return $"Âm lượng: {LevelLabel(db)}{quiet} · ngữ điệu: {ToneLabel(semitones)}";
        }
    }
}
