namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API]
    /// -- Dịch vụ quản lý vòng đời API Key bằng thuật toán Round-Robin.
    /// -- Đảm bảo an toàn đa luồng và tự động cách ly các Key bị lỗi quota (cooldown 1 phút).
    /// -- Nguồn key: danh sách "{Provider}:ApiKeys" (biến môi trường {Provider}__ApiKeys__0..N);
    ///    nếu danh sách trống thì dùng key đơn "{Provider}:ApiKey" (biến môi trường {Provider}__ApiKey).
    /// </summary>
    public class ApiKeyRotationService : IApiKeyProvider
    {
        private readonly string[] _keys;
        private int _currentIndex = 0;
        private readonly object _lock = new object();

        private readonly Dictionary<string, DateTime> _exhaustedKeys = new();
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(1);

        #region Constructor
        /// <summary>
        /// Đọc danh sách key của một nhà cung cấp (vd: "Gemini", "Groq") từ cấu hình.
        /// Bỏ các giá trị rỗng và trùng lặp.
        /// </summary>
        public ApiKeyRotationService(IConfiguration configuration, string providerName)
        {
            var list = configuration.GetSection($"{providerName}:ApiKeys").Get<string[]>() ?? Array.Empty<string>();

            if (list.All(string.IsNullOrWhiteSpace))
            {
                var single = configuration[$"{providerName}:ApiKey"];
                list = string.IsNullOrWhiteSpace(single) ? Array.Empty<string>() : new[] { single };
            }

            _keys = list
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        #endregion

        #region IApiKeyProvider
        /// <inheritdoc />
        public bool HasKeys => _keys.Length > 0;

        /// <inheritdoc />
        public int KeyCount => _keys.Length;

        /// <inheritdoc />
        public string GetNextKey()
        {
            if (_keys.Length == 0) return string.Empty;

            lock (_lock)
            {
                for (int i = 0; i < _keys.Length; i++)
                {
                    var key = _keys[_currentIndex];
                    _currentIndex = (_currentIndex + 1) % _keys.Length;

                    if (_exhaustedKeys.TryGetValue(key, out var expiryTime))
                    {
                        if (DateTime.UtcNow < expiryTime) continue;
                        _exhaustedKeys.Remove(key);
                    }

                    return key;
                }

                // Mọi key đều đang bị phạt: vẫn trả về key kế tiếp để request được thử (provider sẽ báo lỗi rõ ràng)
                var fallbackKey = _keys[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _keys.Length;
                return fallbackKey;
            }
        }

        /// <inheritdoc />
        public void MarkKeyAsExhausted(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            lock (_lock)
            {
                _exhaustedKeys[key] = DateTime.UtcNow.Add(_cooldownPeriod);
            }
        }
        #endregion
    }
}
