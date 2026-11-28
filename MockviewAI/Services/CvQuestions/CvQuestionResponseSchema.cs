namespace MockviewAI.Services.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Khuôn JSON (responseSchema) gửi kèm yêu cầu soạn câu: Gemini buộc phải trả đúng cấu trúc này.
    /// Lý do: chỉ dùng responseMimeType=application/json thì bản lite đôi khi viết thiếu dấu ngoặc kép
    /// (vd <c>minCorePoints": 2</c>), đo 27/11 là 3/10 lần -> cả lượt soạn bị bỏ.
    /// </summary>
    public static class CvQuestionResponseSchema
    {
        private static object Str => new { type = "STRING" };
        private static object StrList => new { type = "ARRAY", items = new { type = "STRING" } };

        public static object Value { get; } = new
        {
            type = "OBJECT",
            properties = new
            {
                questions = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            question = Str,
                            cvEvidence = Str,
                            skills = StrList,
                            level = Str,
                            minCorePoints = new { type = "INTEGER" },
                            corePoints = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new { id = Str, text = Str, keywords = StrList, minHits = new { type = "INTEGER" }, followUp = Str },
                                    required = new[] { "id", "text", "keywords", "minHits", "followUp" }
                                }
                            },
                            paths = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        id = Str,
                                        category = new { type = "STRING", @enum = new[] { "dung", "hoi_dac_biet" } },
                                        summary = Str,
                                        signals = StrList,
                                        coversCorePoints = StrList
                                    },
                                    required = new[] { "id", "category", "summary", "coversCorePoints" }
                                }
                            },
                            mistakes = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new { id = Str, summary = Str, signals = StrList },
                                    required = new[] { "id", "summary" }
                                }
                            }
                        },
                        required = new[] { "question", "cvEvidence", "level", "minCorePoints", "corePoints", "paths", "mistakes" }
                    }
                }
            },
            required = new[] { "questions" }
        };
    }
}
