// Thử tải AI cho MockviewAI: giả lập N buổi phỏng vấn chạy chồng lên nhau, gọi Gemini / Groq THẬT qua đúng service của app,
// rồi báo bao nhiêu % bước chạy được bằng AI và mỗi model trả bao nhiêu lần 429. Xem README.md cùng thư mục.
using System.Text.Json;
using AiLoadTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.CvQuestions;
using MockviewAI.Services.InterviewQuestions.Implementations;
using MockviewAI.Services.InterviewQuestions.Interfaces;
using MockviewAI.Services.Interviews.Conversation;
using MockviewAI.Services.KeyManagement.ProviderExtensions;
using MockviewAI.Services.Speech;
using MockviewAI.Services.Speech.Implementations;
using MockviewAI.Services.Speech.Interfaces;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var options = LoadTestOptions.Parse(args);

// Thư mục gốc repo (có MockviewAI.slnx): đọc .env như app, bộ đề lấy từ MockviewAI/Data
var root = new DirectoryInfo(Environment.CurrentDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "MockviewAI.slnx"))) root = root.Parent;
if (root == null) { Console.WriteLine("Hãy chạy lệnh từ trong thư mục repo MockviewAI."); return 1; }
var envFile = Path.Combine(root.FullName, ".env");
if (File.Exists(envFile)) DotNetEnv.Env.Load(envFile);

// Thứ tự giống app: user-secrets (máy dev) rồi biến môi trường / .env đè lên
var config = new ConfigurationBuilder()
    .AddUserSecrets(typeof(GeminiOptions).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var cvs = JsonDocument.Parse(File.ReadAllText(options.CvFile)).RootElement.EnumerateArray()
    .Select(e => e.TryGetProperty("cv", out var cv) ? cv.GetRawText() : e.GetRawText()).ToList();
byte[]? audio = options.AudioFile != null ? File.ReadAllBytes(options.AudioFile) : null;

int perSession = 1 + 3 + options.Answers * 2 + (options.Scoring ? 1 : 0);
Console.WriteLine($"""
    Kế hoạch: {options.Sessions} buổi, bắt đầu rải trong {options.SpreadMinutes} phút, mỗi buổi {options.Answers} câu, nghỉ ~{options.AnswerGapSeconds} giây giữa các câu.
    Ước tính ~{perSession} lệnh Gemini + 1–2 lệnh Groq duyệt mỗi buổi{(audio != null ? $" + {options.Answers} lệnh Whisper" : " (không gọi Whisper)")} => ~{perSession * options.Sessions} lệnh Gemini.
    Khoá Gemini: {(config.GetSection("Gemini:ApiKeys").GetChildren().Any(c => !string.IsNullOrWhiteSpace(c.Value)) || !string.IsNullOrWhiteSpace(config["Gemini:ApiKey"]) ? "có" : "KHÔNG CÓ")}, khoá Groq: {(config.GetSection("Groq:ApiKeys").GetChildren().Any(c => !string.IsNullOrWhiteSpace(c.Value)) ? "có" : "KHÔNG CÓ")}.
    CẢNH BÁO: dùng lượt THẬT của các khoá trên (chung hạn mức với web nếu là khoá production). Không chạy vào ngày thi.
    """);
if (!options.Confirmed) { Console.WriteLine("Thêm --yes để chạy thật."); return 0; }

// DI: đăng ký đúng các service AI như Program.cs của app (không DB, không Redis)
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true)
    .SetMinimumLevel(options.Verbose ? LogLevel.Information : LogLevel.Warning)
    .AddFilter("System.Net.Http", LogLevel.Warning));
services.AddSingleton<HttpStatusTally>();
services.AddTransient<HttpStatusCounter>();
services.AddSingleton<GeminiKeyProvider>();
services.AddSingleton<GroqKeyProvider>();
services.AddTransient<GeminiKeyHandler>();
services.AddTransient<GroqKeyHandler>();
services.AddHttpClient("GeminiClient", c => { c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/"); c.Timeout = TimeSpan.FromSeconds(180); })
    .AddHttpMessageHandler<GeminiKeyHandler>().AddHttpMessageHandler<HttpStatusCounter>();
services.AddHttpClient("GroqClient", c => c.BaseAddress = new Uri("https://api.groq.com/openai/v1/"))
    .AddHttpMessageHandler<GroqKeyHandler>().AddHttpMessageHandler<HttpStatusCounter>();

services.Configure<GeminiOptions>(config.GetSection("Gemini"));
services.AddSingleton<IQuestionBankStore>(new JsonQuestionBankStore(Path.Combine(root.FullName, "MockviewAI", "Data", "InterviewQuestions")));
services.AddSingleton<IQuestionSelector>(sp => new RandomQuestionSelector(sp.GetRequiredService<IQuestionBankStore>()));
services.AddSingleton<IScoringPromptBuilder, ScoringPromptBuilder>();
services.AddSingleton<IAiResponseParser, GeminiResponseParser>();
services.AddScoped<IAiScoringService, GeminiAiScoringService>();
services.Configure<InterviewerOptions>(config.GetSection(InterviewerOptions.SectionName));
services.AddScoped<IInterviewerTurnService, GeminiInterviewerTurnService>();
services.Configure<SpeechOptions>(config.GetSection(SpeechOptions.SectionName));
services.AddSingleton<ISpeechPromptBuilder, SpeechPromptBuilder>();
services.AddSingleton<IHallucinationFilter, WhisperHallucinationFilter>();
services.AddScoped<ISpeechTranscriber, GroqWhisperTranscriber>();
services.AddScoped<ITranscriptNormalizer, GeminiTranscriptNormalizer>();
services.AddScoped<ISpeechToTextService, SpeechToTextService>();
services.AddCvQuestions(config);
await using var provider = services.BuildServiceProvider();

// Rải thời điểm bắt đầu các buổi đều trong SpreadMinutes
var stats = new StepStats();
var simulator = new SessionSimulator(provider, options, stats, audio);
var watch = System.Diagnostics.Stopwatch.StartNew();
int done = 0, crashed = 0;
var tasks = Enumerable.Range(0, options.Sessions).Select(async i =>
{
    await Task.Delay(TimeSpan.FromMinutes(options.SpreadMinutes * i / Math.Max(1, options.Sessions)));
    try { await simulator.RunAsync(i, cvs[i % cvs.Count], CancellationToken.None); }
    catch (Exception ex) { Interlocked.Increment(ref crashed); Console.WriteLine($"Buổi {i + 1} lỗi ngoài dự kiến: {ex.GetType().Name}: {ex.Message}"); }
    Console.WriteLine($"[{watch.Elapsed:mm\\:ss}] xong buổi {Interlocked.Increment(ref done)}/{options.Sessions}");
}).ToList();
await Task.WhenAll(tasks);

Console.WriteLine($"\n===== KẾT QUẢ ({options.Sessions} buổi trong {watch.Elapsed:mm\\:ss}, {crashed} buổi lỗi ngoài dự kiến) =====");
Console.WriteLine(stats.Report());
Console.WriteLine("Mã HTTP theo model (429 = hết lượt, 503 = Google/Groq quá tải, 0 = mất mạng / quá giờ):");
Console.WriteLine(provider.GetRequiredService<HttpStatusTally>().Report());
return crashed == 0 ? 0 : 1;
