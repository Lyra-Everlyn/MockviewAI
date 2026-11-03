// [AI-MODULE] Lỗi của bộ chấm AI, có thông báo tiếng Việt an toàn để hiển thị cho người dùng.

namespace MockviewAI.Services.Exceptions
{
    public class AiScoringException : Exception
    {
        public AiScoringException(string message, Exception? inner = null) : base(message, inner) { }

        public AiScoringException(string message, bool isConfigurationError) : base(message)
        {
            IsConfigurationError = isConfigurationError;
        }

        // true = lỗi do cấu hình (sai key, sai tên model...): bấm "Chấm lại" cũng không hết, phải sửa cấu hình
        public bool IsConfigurationError { get; }

        // true = lỗi tạm thời (quá tải 503, hết lượt 429, mất mạng, hết giờ chờ): thử model dự phòng / thử lại sau
        public bool IsTransient { get; init; }

        // true = model này không nhận yêu cầu (400) hoặc không tồn tại (404): thử model dự phòng khác
        public bool CanTryOtherModel { get; init; }
    }
}
