using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MockviewAI.Models.Ai;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;
using MockviewAI.Services.Interviews.Conversation;
using MockviewAI.Services.Speech.Interfaces;

namespace AiLoadTest
{
    /// <summary>
    /// Giả lập MỘT buổi phỏng vấn, gọi đúng các service AI của app theo thứ tự như người dùng thật:
    /// gợi ý vị trí -> soạn câu (CV + vị trí) -> mỗi câu trả lời: [Whisper] + chuẩn hoá + người phỏng vấn -> chấm điểm.
    /// Không đụng DB / Redis. Mỗi bước ghi "AI" (chạy được) hoặc "dự phòng" (app sẽ dùng đường dự phòng).
    /// </summary>
    public class SessionSimulator
    {
        private static readonly string[] Interviewers = { "nam", "linh", "an" };
        private static readonly Dictionary<string, (string Name, string Role)> Personas = new()
        {
            ["nam"] = ("Nam", "Tech Lead"), ["linh"] = ("Linh", "HR"), ["an"] = ("An", "Project Manager")
        };

        // Câu trả lời mẫu: bản "thô" có lỗi nghe nhầm như Whisper, bản trình duyệt đúng hơn
        private static readonly (string Raw, string Browser)[] SampleAnswers =
        {
            ("Dạ em làm bách en với xi sáp và dùng rê đít để cát dữ liệu, em tách phần đọc và ghi cho dễ bảo trì", "dạ em làm backend với C# và dùng Redis để cache dữ liệu, em tách phần đọc và ghi cho dễ bảo trì"),
            ("Trong đồ án nhóm em phụ trách phần đăng nhập, em gặp lỗi phiên đăng nhập bị mất nên em đọc log rồi sửa cấu hình cookie", "trong đồ án nhóm em phụ trách phần đăng nhập, em gặp lỗi phiên đăng nhập bị mất nên em đọc log rồi sửa cấu hình cookie"),
            ("Khi deadline gấp em sẽ báo trưởng nhóm sớm và đề xuất cắt bớt phạm vi thay vì làm thêm giờ liên tục", "khi deadline gấp em sẽ báo trưởng nhóm sớm và đề xuất cắt bớt phạm vi thay vì làm thêm giờ liên tục"),
            ("em dùng git rì bết để giữ lịch sử commit gọn còn merge thì giữ nguyên nhánh", "em dùng git rebase để giữ lịch sử commit gọn còn merge thì giữ nguyên nhánh"),
            ("Dạ em chưa làm nhiều nhưng em nghĩ là sẽ kiểm tra lại dữ liệu đầu vào trước", "dạ em chưa làm nhiều nhưng em nghĩ là sẽ kiểm tra lại dữ liệu đầu vào trước"),
        };

        private readonly IServiceProvider _root;
        private readonly LoadTestOptions _options;
        private readonly StepStats _stats;
        private readonly byte[]? _audio;

        public SessionSimulator(IServiceProvider root, LoadTestOptions options, StepStats stats, byte[]? audio)
        {
            _root = root;
            _options = options;
            _stats = stats;
            _audio = audio;
        }

        public async Task RunAsync(int index, string cvJson, CancellationToken ct)
        {
            using var scope = _root.CreateScope();   // mỗi buổi một scope như một request web
            var sp = scope.ServiceProvider;
            string interviewer = Interviewers[index % Interviewers.Length];
            var rnd = new Random(index);

            // 1. Gợi ý vị trí từ CV
            var sw = Stopwatch.StartNew();
            var suggestion = await sp.GetRequiredService<IRoleSuggester>().SuggestAsync(cvJson, ct);
            _stats.Add("1. Gợi ý vị trí", suggestion?.Source == "ai", sw.ElapsedMilliseconds);
            var role = ItRoleCatalog.Find(suggestion?.Key) ?? ItRoleCatalog.Find("developer")!;

            // 2. Soạn câu theo CV (+ câu kỹ thuật theo vị trí khi Nam và vị trí chưa có bộ đề)
            var picked = sp.GetRequiredService<IQuestionSelector>().Select(interviewer, role.Key, _options.Answers, role.BankBase);
            var ctx = new CvQuestionContext
            {
                PositionKey = role.Key, PositionName = role.Name, PositionFocus = role.Focus, InterviewerKey = interviewer,
                CvSummary = CvContextBuilder.FromBrowserJson(cvJson),
                RoleQuestionCount = interviewer == "nam" && sp.GetRequiredService<IQuestionBankStore>().NeedsAiTechQuestions(role) ? picked.Count - 1 : 0,
                ExistingQuestions = picked.Select(q => q.Question).ToList()
            };
            sw.Restart();
            var generated = await sp.GetRequiredService<ICvQuestionPipeline>().RunAsync(ctx, ct);
            _stats.Add("2. Soạn câu CV / vị trí", generated.Accepted.Count > 0, sw.ElapsedMilliseconds);

            // 3. Từng câu trả lời
            var answers = new List<InterviewAnswer>();
            var persona = Personas[interviewer];
            foreach (var q in picked)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.AnswerGapSeconds * (0.7 + 0.6 * rnd.NextDouble())), ct);   // thời gian nói
                var sample = SampleAnswers[rnd.Next(SampleAnswers.Length)];
                string text = await TranscribeAsync(sp, q.Question, sample, ct);

                sw.Restart();
                var turn = await sp.GetRequiredService<IInterviewerTurnService>().NextTurnAsync(new InterviewerTurnRequest
                {
                    InterviewerName = persona.Name, InterviewerRole = persona.Role, Position = role.Name,
                    Question = q.Question, Answer = text, AllowFollowUp = true, CvContext = ctx.CvSummary
                }, ct);
                _stats.Add("4. Người phỏng vấn nhận xét", turn.FromAi, sw.ElapsedMilliseconds);
                answers.Add(new InterviewAnswer { QuestionId = q.Id, Question = q.Question, Answer = text, DurationSeconds = 25 });
            }

            // 4. Chấm điểm cuối buổi
            if (!_options.Scoring) return;
            sw.Restart();
            bool scored;
            try
            {
                await sp.GetRequiredService<IAiScoringService>().ScoreContentAsync(new ScoreRequest
                {
                    Position = role.Name, Answers = answers, QuestionCount = answers.Count
                }, ct);
                scored = true;
            }
            catch (AiScoringException) { scored = false; }   // app sẽ hiện nút "Chấm lại" / "chấm sơ bộ"
            _stats.Add("5. Chấm điểm AI", scored, sw.ElapsedMilliseconds);
        }

        /// <summary>Có file âm thanh: Whisper + chuẩn hoá như app. Không có: chỉ chuẩn hoá bản thô mẫu (không tốn lượt Whisper).</summary>
        private async Task<string> TranscribeAsync(IServiceProvider sp, string question, (string Raw, string Browser) sample, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            if (_audio != null)
            {
                try
                {
                    var result = await sp.GetRequiredService<ISpeechToTextService>().TranscribeAnswerAsync(
                        new AudioClip(_audio, "answer.wav", "audio/wav"), question, sample.Browser, ct);
                    _stats.Add("3a. Whisper", true, sw.ElapsedMilliseconds);
                    _stats.Add("3b. Chuẩn hoá (sau Whisper)", result.Normalized, 0);
                    return result.Text.Length > 0 ? result.Text : sample.Browser;
                }
                catch (SpeechException)
                {
                    _stats.Add("3a. Whisper", false, sw.ElapsedMilliseconds);   // app dùng chữ trình duyệt
                    return sample.Browser;
                }
            }

            try
            {
                var text = await sp.GetRequiredService<ITranscriptNormalizer>().NormalizeAsync(sample.Raw, sample.Browser, question, ct);
                _stats.Add("3. Chuẩn hoá bản chép", true, sw.ElapsedMilliseconds);
                return text;
            }
            catch (SpeechException)
            {
                _stats.Add("3. Chuẩn hoá bản chép", false, sw.ElapsedMilliseconds);   // app dùng bản thô
                return sample.Raw;
            }
        }
    }
}
