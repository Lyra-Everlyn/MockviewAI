namespace MockviewAI.Models
{
    /// <summary>
    /// [VOICE] 
    /// Ngưỡng và nhãn âm lượng / giọng điệu phía server.
    /// Số đo do trình duyệt tính bằng Web Audio (không dùng AI); server chỉ kiểm khoảng hợp lệ rồi lưu.
    /// Phải khớp với wwwroot/js/VoiceAnalysis.js (QUIET_DB, LOUD_DB, MONOTONE_ST, LIVELY_ST).
    /// </summary>
    public static class VoiceMetrics
    {
        #region Ngưỡng
        /// <summary>dBFS: nhỏ hơn mức này là khó nghe.</summary>
        public const double QuietDb = -38;

        /// <summary>dBFS: lớn hơn mức này dễ bị rè.</summary>
        public const double LoudDb = -10;

        /// <summary>Nửa cung: nhỏ hơn mức này là giọng đều đều.</summary>
        public const double MonotoneSt = 1.5;

        /// <summary>Nửa cung: lớn hơn mức này là nhấn nhá nhiều.</summary>
        public const double LivelySt = 6;
        #endregion

        #region Nhãn hiển thị
        public static string LevelLabel(double? db) =>
            db == null ? "—" : db < QuietDb ? "Nhỏ" : db > LoudDb ? "Quá to" : "Vừa";

        public static string ToneLabel(double? semitones) =>
            semitones == null ? "—" : semitones < MonotoneSt ? "Đều đều" : semitones > LivelySt ? "Nhấn nhá nhiều" : "Tự nhiên";

        /// <summary>Một dòng mô tả cho trang Xem lại; null = câu không có số đo (vd gõ chữ).</summary>
        public static string? Describe(double? db, double? quietRatio, double? semitones)
        {
            if (db == null && quietRatio == null) return null;
            string quiet = quietRatio == null ? "" : $" · nói nhỏ {quietRatio.Value * 100:0}% thời gian";
            return $"Âm lượng: {LevelLabel(db)}{quiet} · ngữ điệu: {ToneLabel(semitones)}";
        }
        #endregion

        #region Làm sạch số đo từ trình duyệt
        // Ngoài khoảng hợp lý -> null, không tin dữ liệu gửi lên
        public static double? CleanLevel(double? db) => db is >= -100 and <= 0 ? Math.Round(db.Value, 1) : null;
        public static double? CleanRatio(double? r) => r is >= 0 and <= 1 ? Math.Round(r.Value, 2) : null;
        public static double? CleanPitch(double? st) => st is >= 0 and <= 24 ? Math.Round(st.Value, 1) : null;
        #endregion
    }
}
