using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Đăng ký bộ chấm nội dung theo cấu hình Ai:Provider.
    /// Production / Staging BẮT BUỘC dùng Gemini: thiếu cấu hình thì dừng app ngay khi khởi động,
    /// không âm thầm chuyển sang chấm giả. Development cho phép Mock để chạy thử khi không có key.
    /// </summary>
    public static class AiScoringExtensions
    {
        public const string ProviderGemini = "Gemini";
        public const string ProviderMock = "Mock";

        #region Đăng ký
        /// <summary>Dùng: var aiProvider = builder.Services.AddAiScoring(builder.Configuration, builder.Environment);</summary>
        /// <returns>Tên bộ chấm thực tế, để ghi log khi khởi động.</returns>
        /// <exception cref="InvalidOperationException">Ngoài Development mà không cấu hình Gemini đầy đủ.</exception>
        public static string AddAiScoring(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            string provider = (config["Ai:Provider"] ?? string.Empty).Trim();
            bool useGemini = string.Equals(provider, ProviderGemini, StringComparison.OrdinalIgnoreCase);

            if (!useGemini && !env.IsDevelopment())
            {
                string shown = provider.Length == 0 ? "(trống)" : provider;
                throw new InvalidOperationException(
                    $"Ai:Provider = '{shown}' nhưng môi trường '{env.EnvironmentName}' bắt buộc dùng Gemini. " +
                    "Hãy đặt biến môi trường Ai__Provider=Gemini (chấm điểm giả chỉ dùng khi Development).");
            }

            if (useGemini)
            {
                EnsureGeminiConfigured(config);

                services.AddSingleton<IScoringPromptBuilder, ScoringPromptBuilder>();
                services.AddSingleton<IAiResponseParser, GeminiResponseParser>();
                // Gọi qua HttpClient "GeminiClient" (đăng ký ở Program.cs, có GeminiKeyHandler xoay vòng key)
                services.AddScoped<IAiScoringService, GeminiAiScoringService>();
                return ProviderGemini;
            }

            services.AddScoped<IAiScoringService, MockAiScoringService>();
            return ProviderMock;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Kiểm tra các khoá Gemini bắt buộc, báo tên biến môi trường còn thiếu (dạng Dokploy, dấu __).
        /// Không bao giờ đưa giá trị key vào thông báo lỗi.
        /// </summary>
        private static void EnsureGeminiConfigured(IConfiguration config)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(config["Gemini:Model"])) missing.Add("Gemini__Model");

            // Cùng nguồn key với GeminiKeyProvider: danh sách Gemini:ApiKeys, nếu trống thì Gemini:ApiKey
            var keyList = config.GetSection("Gemini:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
            bool hasKey = keyList.Any(k => !string.IsNullOrWhiteSpace(k))
                          || !string.IsNullOrWhiteSpace(config["Gemini:ApiKey"]);
            if (!hasKey) missing.Add("Gemini__ApiKeys__0 (hoặc Gemini__ApiKey)");

            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "Thiếu cấu hình chấm điểm AI: " + string.Join(", ", missing) + ". Hãy thêm vào tab Environment trên Dokploy.");
        }
        #endregion
    }
}
