namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API]
    /// -- Cung cấp cơ chế quản lý và luân phiên nhiều API Key khi gọi tới các dịch vụ AI.
    /// -- Giúp tối ưu hóa hạn mức, tránh bị block và đảm bảo tính liên tục của hệ thống.
    /// </summary>
    public interface IApiKeyProvider
    {
        /// <summary>
        /// true nếu cấu hình có ít nhất một API Key (danh sách ApiKeys hoặc ApiKey đơn).
        /// Dịch vụ gọi AI kiểm tra cờ này để báo lỗi cấu hình rõ ràng thay vì gửi request không có key.
        /// </summary>
        bool HasKeys { get; }

        /// <summary>
        /// Số lượng API Key đang được quản lý (dùng để quyết định có nên đổi key và thử lại hay không).
        /// </summary>
        int KeyCount { get; }

        /// <summary>
        /// Trả về API Key hợp lệ tiếp theo. Tự động bỏ qua các Key đang trong thời gian bị phạt.
        /// Trả về chuỗi rỗng khi không có key nào được cấu hình.
        /// </summary>
        string GetNextKey();

        /// <summary>
        /// Đưa một API Key vào danh sách đen tạm thời (1 phút) khi nhận mã lỗi 401, 402, 403, 429 từ AI provider.
        /// </summary>
        void MarkKeyAsExhausted(string key);
    }
}
