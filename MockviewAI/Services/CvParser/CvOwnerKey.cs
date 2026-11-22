using System.Security.Cryptography;
using System.Text;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Mã tài khoản (băm email) làm khoá lưu CV trên trình duyệt, tránh ghi email thô.
    /// </summary>
    public static class CvOwnerKey
    {
        public static string For(string? email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Length == 0) return string.Empty;

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("mockviewai-cv:" + normalized));
            return Convert.ToHexString(hash, 0, 12).ToLowerInvariant();
        }
    }
}
