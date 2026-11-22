namespace MockviewAI.Services.Profiles
{
    /// <summary>
    /// [PROFILE] 
    /// Chỉ nhận ảnh JPG / PNG / WebP tối đa 5 MB. Kiểm tra cả đuôi file lẫn chữ ký nhị phân ở đầu file,
    /// nên đổi tên file khác thành .jpg cũng bị chặn.
    /// </summary>
    public class AvatarFileValidator : IAvatarFileValidator
    {
        #region Fields
        public const long MaxBytes = 5 * 1024 * 1024;

        private const int HeaderBytes = 12;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] RiffSignature = "RIFF"u8.ToArray();
        private static readonly byte[] WebpSignature = "WEBP"u8.ToArray();
        #endregion

        #region IAvatarFileValidator
        public async Task<string?> ValidateAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file.Length == 0)
                return "File ảnh rỗng. Vui lòng chọn ảnh khác.";

            if (file.Length > MaxBytes)
                return $"Ảnh đại diện tối đa {MaxBytes / (1024 * 1024)} MB. Vui lòng chọn ảnh nhỏ hơn.";

            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
                return "Chỉ hỗ trợ ảnh JPG, PNG hoặc WebP.";

            var head = new byte[HeaderBytes];
            int read;
            await using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
            }

            return IsKnownImage(head.AsSpan(0, read)) ? null : "File không phải ảnh hợp lệ. Hãy chọn ảnh JPG, PNG hoặc WebP.";
        }
        #endregion

        #region Helpers
        /// <summary>JPEG: FF D8 FF · PNG: 89 50 4E 47 0D 0A 1A 0A · WebP: "RIFF" + 4 byte độ dài + "WEBP".</summary>
        private static bool IsKnownImage(ReadOnlySpan<byte> head) =>
            head.StartsWith(JpegSignature)
            || head.StartsWith(PngSignature)
            || (head.Length >= HeaderBytes && head.StartsWith(RiffSignature) && head.Slice(8, 4).SequenceEqual(WebpSignature));
        #endregion
    }
}
