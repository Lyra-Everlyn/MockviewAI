namespace MockviewAI.Services.Speech
{
    /// <summary>
    /// [SPEECH] 
    /// Định dạng ghi âm được chấp nhận (khớp với định dạng Groq Whisper đọc được) và đuôi file tương ứng
    /// (Whisper dựa vào đuôi file để giải mã). Chrome / Edge / Firefox ghi audio/webm (opus), Safari ghi audio/mp4 (aac).
    /// </summary>
    public static class AudioFormats
    {
        #region Định dạng hỗ trợ
        private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["audio/webm"] = ".webm",
            ["video/webm"] = ".webm",
            ["audio/ogg"] = ".ogg",
            ["audio/mp4"] = ".mp4",
            ["audio/x-m4a"] = ".m4a",
            ["audio/m4a"] = ".m4a",
            ["audio/aac"] = ".m4a",
            ["audio/mpeg"] = ".mp3",
            ["audio/mp3"] = ".mp3",
            ["audio/wav"] = ".wav",
            ["audio/x-wav"] = ".wav",
            ["audio/wave"] = ".wav",
            ["audio/flac"] = ".flac"
        };
        #endregion

        #region API
        /// <summary>Lấy đuôi file từ Content-Type (bỏ phần tham số như ";codecs=opus").</summary>
        /// <param name="mediaType">Content-Type đã bỏ tham số, chữ thường.</param>
        /// <returns>false nếu định dạng không được hỗ trợ.</returns>
        public static bool TryGetExtension(string? contentType, out string mediaType, out string extension)
        {
            mediaType = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
            return Extensions.TryGetValue(mediaType, out extension!);
        }
        #endregion
    }
}
