namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Quản lý và luân phiên nhiều API key của một nhà cung cấp AI,
    /// để dàn đều hạn mức và tự bỏ qua key đang bị giới hạn.
    /// </summary>
    public interface IApiKeyProvider
    {
        /// <summary>Có ít nhất một key. Service gọi AI kiểm tra cờ này để báo lỗi cấu hình rõ ràng.</summary>
        bool HasKeys { get; }

        /// <summary>Số key đang quản lý; dưới 2 thì đổi key thử lại là vô ích.</summary>
        int KeyCount { get; }

        /// <summary>Key kế tiếp theo vòng tròn, bỏ qua key đang bị phạt. Chuỗi rỗng khi không có key.</summary>
        string GetNextKey();

        /// <summary>Phạt key 1 phút khi nhà cung cấp trả 401, 402, 403 hoặc 429.</summary>
        void MarkKeyAsExhausted(string key);
    }
}
