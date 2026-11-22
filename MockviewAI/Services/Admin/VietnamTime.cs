namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Đổi giờ UTC lưu trong database sang giờ Việt Nam (UTC+7) để hiển thị.
    /// Không dùng ToLocalTime() vì container Docker chạy giờ UTC.
    /// </summary>
    public static class VietnamTime
    {
        private static readonly TimeZoneInfo Zone = FindZone();

        /// <summary>Giờ trong database được lưu bằng DateTime.UtcNow nên coi là UTC.</summary>
        public static DateTime FromUtc(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        public static DateTime? FromUtc(DateTime? utc) => utc.HasValue ? FromUtc(utc.Value) : null;

        public static DateTime Today => FromUtc(DateTime.UtcNow).Date;

        private static TimeZoneInfo FindZone()
        {
            foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            // Máy không có dữ liệu múi giờ: Việt Nam không đổi giờ mùa hè nên cố định +7
            return TimeZoneInfo.CreateCustomTimeZone("VN+7", TimeSpan.FromHours(7), "Giờ Việt Nam", "Giờ Việt Nam");
        }
    }
}
