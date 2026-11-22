namespace MockviewAI.Services.FileStorage.Interfaces
{
    /// <summary>
    /// [FILE-STORAGE] 
    /// Kiểu cắt ảnh khi tải lên.
    /// </summary>
    public enum UploadType
    {
        /// <summary>Giữ nguyên bản gốc.</summary>
        General,

        /// <summary>Vuông 500×500, tập trung vào khuôn mặt (ảnh đại diện).</summary>
        Avatar,

        /// <summary>Chưa dùng: chữ nhật 1200×400.</summary>
        Banner,

        /// <summary>Chưa dùng: vuông 250×250.</summary>
        Thumbnail
    }
}
