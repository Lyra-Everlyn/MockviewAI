namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [KEY-ROTATION] 
    /// Luân phiên key kiểu vòng tròn (round-robin), an toàn đa luồng, phạt key lỗi 1 phút.
    /// Nguồn key: danh sách "{Provider}:ApiKeys" (biến môi trường {Provider}__ApiKeys__0..N);
    /// danh sách trống thì dùng key đơn "{Provider}:ApiKey".
    /// Đăng ký Singleton qua lớp con (GeminiKeyProvider, GroqKeyProvider).
    /// </summary>
    public class ApiKeyRotationService : IApiKeyProvider
    {
        #region Fields
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(1);
        private readonly object _lock = new object();

        private readonly string[] _keys;
        private int _currentIndex = 0;

        /// <summary>Key đang bị phạt -> thời điểm hết phạt (UTC).</summary>
        private readonly Dictionary<string, DateTime> _exhaustedKeys = new();
        #endregion

        #region Constructor
        /// <summary>Đọc key của một nhà cung cấp ("Gemini", "Groq"), bỏ giá trị rỗng và trùng lặp.</summary>
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

                // Mọi key đều đang bị phạt: vẫn trả key kế tiếp để request được thử (nhà cung cấp sẽ báo lỗi rõ ràng)
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
