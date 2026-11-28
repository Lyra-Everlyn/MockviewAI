using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Speech;
using MockviewAI.Services.CvQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Interviews;
using MockviewAI.Services.Speech;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Vòng đời một buổi phỏng vấn: tạo buổi, phòng phỏng vấn, chuyển giọng nói thành chữ,
    /// ghi nhận câu trả lời, chấm điểm và xem kết quả.
    /// </summary>
    [Authorize]
    public class InterviewController : Controller
    {
        #region Fields
        private const long MaxAudioRequestBytes = 12 * 1024 * 1024;
        private const int MaxBrowserTextChars = 4000;

        private readonly IInterviewService _interviewService;
        private readonly ISpeechToTextService _speechToText;
        private readonly SpeechOptions _speechOptions;
        private readonly IRoleSuggester _roleSuggester;
        #endregion

        #region Constructor
        public InterviewController(
            IInterviewService interviewService,
            ISpeechToTextService speechToText,
            IOptions<SpeechOptions> speechOptions,
            IRoleSuggester roleSuggester)
        {
            _interviewService = interviewService;
            _speechToText = speechToText;
            _speechOptions = speechOptions.Value;
            _roleSuggester = roleSuggester;
        }
        #endregion

        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Tạo buổi
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// [IT-ROLES] POST /Interview/SuggestRole (form: cvJson): AI đọc CV, gợi ý vị trí ngành IT phù hợp; người dùng vẫn đổi được.
        /// Không lưu gì. AI lỗi / chậm thì đoán bằng từ khoá.
        /// </summary>
        /// <returns>{ key, name, reason, source } hoặc 400 khi CV rỗng / hỏng.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(CvQuestionExtensions.RoleSuggestRateLimitPolicy)]
        public async Task<IActionResult> SuggestRole([FromForm] string? cvJson, CancellationToken ct)
        {
            var suggestion = await _roleSuggester.SuggestAsync(cvJson, ct);
            return suggestion == null
                ? BadRequest(new { message = "Không đọc được CV." })
                : Json(new { key = suggestion.Key, name = suggestion.Name, reason = suggestion.Reason, source = suggestion.Source });
        }

        /// <summary>POST /Interview/Start: tạo buổi từ form rồi vào thẳng phòng phỏng vấn.</summary>
        /// <param name="position">Mã vị trí ứng tuyển chọn trên form.</param>
        /// <param name="interviewer">Mã người phỏng vấn chọn trên form.</param>
        /// <param name="questionCount">Số câu hỏi của buổi.</param>
        /// <param name="emailResult">Gửi kết quả qua email sau buổi.</param>
        /// <param name="mode">Chế độ luyện chọn trên form.</param>
        /// <param name="cvJson">CV mặc định trên trình duyệt (JSON); rỗng = không hỏi theo CV.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string position, string interviewer, int questionCount, bool emailResult, string? mode, string? cvJson)
        {
            try
            {
                int sessionId = await _interviewService.StartAsync(CurrentEmail, position, interviewer, questionCount, emailResult, mode, cvJson);
                return RedirectToAction("Room", new { id = sessionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = SafeMessage(ex);
                return RedirectToAction("Create");
            }
        }
        #endregion

        #region Phòng phỏng vấn
        /// <summary>
        /// GET /Interview/Room/{id}: hiện câu hỏi chưa trả lời. Hết câu (hoặc buổi không còn mở) thì chấm điểm và chuyển sang Kết quả.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _interviewService.GetRoomAsync(CurrentEmail, id);
            if (room != null)
                return View("~/Views/Home/InterviewRoom.cshtml", room);

            try
            {
                await _interviewService.FinishAsync(CurrentEmail, new FinishSessionDto { SessionId = id });
            }
            catch (AiScoringException ex) when (ex.IsTransient && !ex.IsConfigurationError)
            {
                // AI hết lượt / quá tải (thường khi tải lại trang lúc đang chờ chấm): không chấm sơ bộ ngay
                // vì chấm từ khoá dễ chấm oan; mở trang chờ tự chấm lại bằng AI (buổi vẫn InProgress, người dùng tự chọn chấm sơ bộ được)
                ViewData["ScoreMessage"] = ex.Message;
                return View("Scoring", id);
            }
            catch (AiScoringException ex)
            {
                // AI lỗi khi mở lại buổi đã trả lời xong: chấm sơ bộ để buổi không kẹt ở trạng thái "đang làm"
                try
                {
                    await _interviewService.FinishAsync(CurrentEmail, new FinishSessionDto { SessionId = id, Fallback = true });
                    TempData["InfoMessage"] = ex.Message + " Kết quả dưới đây được chấm sơ bộ, không dùng AI.";
                    return RedirectToAction("Result", new { id });
                }
                catch (Exception inner)
                {
                    TempData["ErrorMessage"] = SafeMessage(inner);
                    return RedirectToAction("History", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = SafeMessage(ex);
                return RedirectToAction("Create");
            }

            return RedirectToAction("Result", new { id });
        }

        /// <summary>
        /// POST /Interview/Transcribe (multipart: sessionId, audio, browserText): Whisper + Gemini chuẩn hoá, không lưu gì.
        /// Câu hỏi làm ngữ cảnh lấy từ server; chỉ chủ buổi đang diễn ra mới gọi được.
        /// </summary>
        /// <param name="browserText">Chữ trình duyệt tự nhận dạng, dùng để đối chiếu với Whisper.</param>
        /// <returns>{ rawText, text, normalized, empty } hoặc 400 { message, retry, configError }.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("SpeechRateLimit")]
        [RequestSizeLimit(MaxAudioRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxAudioRequestBytes)]
        public async Task<IActionResult> Transcribe([FromForm] int sessionId, IFormFile? audio, [FromForm] string? browserText, CancellationToken ct)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(new { message = "Không nhận được file ghi âm. Vui lòng thử lại.", retry = true });
            if (audio.Length > _speechOptions.MaxAudioBytes)
                return BadRequest(new { message = "File ghi âm quá lớn. Hãy trả lời ngắn gọn hơn rồi thử lại." });
            if (!AudioFormats.TryGetExtension(audio.ContentType, out var mediaType, out var extension))
                return BadRequest(new { message = "Định dạng ghi âm không được hỗ trợ. Hãy dùng Chrome, Edge, Firefox hoặc Safari bản mới." });

            try
            {
                // Đồng thời kiểm tra quyền và buổi còn đang diễn ra
                string question = await _interviewService.GetCurrentQuestionAsync(CurrentEmail, sessionId);

                byte[] data;
                using (var buffer = new MemoryStream((int)audio.Length))
                {
                    await audio.CopyToAsync(buffer, ct);
                    data = buffer.ToArray();
                }

                var clip = new AudioClip(data, "answer" + extension, mediaType);
                var result = await _speechToText.TranscribeAnswerAsync(clip, question, Truncate(browserText, MaxBrowserTextChars), ct);

                return Json(new
                {
                    rawText = result.RawText,
                    text = result.Text,
                    normalized = result.Normalized,
                    empty = result.IsEmpty
                });
            }
            catch (SpeechException ex)
            {
                // configError: lỗi phía server (key, mạng bị chặn) -> trình duyệt dùng chữ tự nhận dạng thay vì thử lại
                return BadRequest(new { message = ex.Message, configError = ex.IsConfigurationError, retry = ex.IsTransient });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }

        /// <summary>POST /Interview/Answer: lưu câu trả lời hoặc bỏ qua; trả câu tiếp theo kèm phản hồi của người phỏng vấn.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer([FromBody] SubmitAnswerDto dto)
        {
            try
            {
                var result = await _interviewService.SubmitAnswerAsync(CurrentEmail, dto);
                return Json(result);
            }
            catch (AnswerRejectedException ex)
            {
                // retry: trình duyệt xoá câu trả lời cũ để người dùng nói lại
                return BadRequest(new { message = ex.Message, retry = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }

        /// <summary>POST /Interview/Finish: chấm điểm và kết thúc buổi (hết câu hoặc bấm Dừng). Fallback = chấm sơ bộ, không dùng AI.</summary>
        /// <returns>{ redirectUrl } tới trang Kết quả, hoặc 400 { message, configError }.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish([FromBody] FinishSessionDto dto)
        {
            try
            {
                await _interviewService.FinishAsync(CurrentEmail, dto);
                return Json(new { redirectUrl = Url.Action("Result", new { id = dto.SessionId }) });
            }
            catch (AiScoringException ex)
            {
                // configError: sai model / key, bấm "Chấm lại" vô ích -> giao diện mở ngay nút chấm sơ bộ
                // retryable: hết lượt / quá tải / quá giờ (hạn mức phút của Gemini hồi trong ~1 phút) -> giao diện tự chấm lại sau ít giây
                return BadRequest(new { message = ex.Message, configError = ex.IsConfigurationError, retryable = ex.IsTransient && !ex.IsConfigurationError });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }
        #endregion

        #region Kết quả
        /// <summary>GET /Interview/Result/{id}: báo cáo kết quả; buổi chưa kết thúc hoặc không tồn tại thì về Lịch sử.</summary>
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var result = await _interviewService.GetResultAsync(CurrentEmail, id);
            if (result == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kết quả của buổi luyện này.";
                return RedirectToAction("History", "Home");
            }

            return View(result);
        }
        #endregion

        #region Helpers
        /// <summary>Chỉ hiện nguyên văn lỗi nghiệp vụ; lỗi hệ thống (database, mạng) thay bằng thông báo chung.</summary>
        private static string SafeMessage(Exception ex) =>
            ex is InterviewException or AiScoringException or SpeechException ? ex.Message : "Có lỗi xảy ra. Vui lòng thử lại.";

        private static string? Truncate(string? text, int max) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Length <= max ? text : text[..max];
        #endregion
    }
}
