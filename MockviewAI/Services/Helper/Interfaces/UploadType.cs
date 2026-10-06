namespace MockviewAI.Services.Helper.Interfaces
{
    public enum UploadType
    {
        General,    // Giữ nguyên bản gốc (dùng cho file thường, ảnh không cần crop)
        Avatar,     // Cắt vuông, tập trung vào khuôn mặt
        Banner,     // Cắt hình chữ nhật dài (tính năng tương lai)
        Thumbnail   // Cắt thu nhỏ (tính năng tương lai)
    }
}