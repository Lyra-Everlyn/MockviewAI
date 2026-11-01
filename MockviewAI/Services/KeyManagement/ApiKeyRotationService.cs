namespace MockviewAI.Services.KeyManagement
{
    /// <summary>
    /// [SWITCH API]
    /// -- Dịch vụ quản lý vòng đời API Key bằng thuật toán Round-Robin. 
    /// -- Đảm bảo an toàn đa luồng và tự động cách ly các Key bị lỗi quota (cooldown 1 phút).
    /// </summary>

    public class ApiKeyRotationService : IApiKeyProvider
    {
        private readonly string[] _keys;
        private int _currentIndex = 0;
        private readonly object _lock = new object();

        private readonly Dictionary<string, DateTime> _exhaustedKeys = new();
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(1);

        public ApiKeyRotationService(IConfiguration configuration, string providerName)
        {
            _keys = configuration.GetSection($"{providerName}:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        }


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
                        if (DateTime.UtcNow < expiryTime)
                        {
                            continue;
                        }
                        else
                        {
                            _exhaustedKeys.Remove(key);
                        }
                    }

                    return key;
                }

                var fallbackKey = _keys[_currentIndex];
                _currentIndex = (_currentIndex + 1) % _keys.Length;
                return fallbackKey;
            }
        }


        public void MarkKeyAsExhausted(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            lock (_lock)
            {
                _exhaustedKeys[key] = DateTime.UtcNow.Add(_cooldownPeriod);
            }
        }
    }
}
