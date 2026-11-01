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
        /// Trả về API Key hợp lệ tiếp theo. Tự động bỏ qua các Key đang trong thời gian bị phạt.
        /// </summary>   
        string GetNextKey();

        /// <summary>
        /// Đưa một API Key vào danh sách đen tạm thời (1 phút) khi nhận mã lỗi 402, 429 từ AI provider.
        /// </summary>
        void MarkKeyAsExhausted(string key);
    }
}
