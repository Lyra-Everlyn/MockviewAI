using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Interviews;
using MockviewAI.Services.Speech;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [AI-INTERVIEW]
    /// Controller chịu trách nhiệm quản lý toàn bộ vòng đời của một buổi phỏng vấn AI.
    /// Bao gồm: Khởi tạo, Phòng phỏng vấn, Chuyển giọng nói thành chữ, Ghi nhận câu trả lời, Chấm điểm và Xem kết quả.
    /// </summary>
    [Authorize]
    public class InterviewController : Controller
    {
        private readonly IInterviewService _interviewService;
        private readonly ISpeechToTextService _speechToText;
        private readonly SpeechOptions _speechOptions;

        public InterviewController(
            IInterviewService interviewService,
            ISpeechToTextService speechToText,
            IOptions<SpeechOptions> speechOptions)
        {
            _interviewService = interviewService;
            _speechToText = speechToText;
            _speechOptions = speechOptions.Value;
        }

        /// <summary>
        /// Bộ lọc lỗi an toàn. Chỉ hiển thị cho người dùng những lỗi logic được định nghĩa trước
        /// (InterviewException, AiScoringException, SpeechException). Che giấu các lỗi hệ thống (Database, Network).
        /// </summary>
        private static string SafeMessage(Exception ex) =>
            ex is InterviewException or AiScoringException or SpeechException ? ex.Message : "Có lỗi xảy ra. Vui lòng thử lại.";

        /// <summary>
        /// Lấy Email của người dùng hiện tại từ Claims (Cookie đã được lưu lúc Login).
        /// Trả về rỗng nếu không tìm thấy.
        /// </summary>
        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Start
        /// <summary>
        /// GET: /Interview/Create
        /// Hiển thị form khởi tạo buổi phỏng vấn (chọn vị trí, người phỏng vấn, số lượng câu).
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: /Interview/Start
        /// Xử lý form khởi tạo buổi phỏng vấn mới.
        /// </summary>
        /// <param name="position">Vị trí ứng tuyển (vd: frontend, backend).</param>
        /// <param name="interviewer">Tính cách người phỏng vấn (vd: friendly, strict).</param>
        /// <param name="questionCount">Số lượng câu hỏi (vd: 5, 10).</param>
        /// <param name="emailResult">Người dùng đồng ý nhận kết quả qua email (tick ở form).</param>
        /// <returns>Chuyển hướng người dùng vào thẳng phòng phỏng vấn (Interview Room).</returns>
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

        #region Room
        /// <summary>
        /// GET: /Interview/Room/{id}
        /// Truy cập vào phòng phỏng vấn dựa trên ID buổi luyện.
        /// Tự động lấy câu hỏi hiện tại chưa được trả lời để hiển thị.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _interviewService.GetRoomAsync(CurrentEmail, id);

            // Nếu room = null nghĩa là không tìm thấy, hoặc là đã trả lời xong hết câu hỏi
            if (room == null)
            {
                try
                {
                    // Tự động gọi hàm chấm điểm và chốt sổ buổi phỏng vấn
                    await _interviewService.FinishAsync(CurrentEmail, new FinishSessionDto { SessionId = id });
                }
                catch (AiScoringException ex)
                {
                    // AI lỗi khi mở lại buổi đã trả lời xong: không để buổi kẹt mãi ở "đang làm".
                    // Chấm dự phòng (chỉ tính cách nói) rồi mở trang Kết quả, kèm lý do để người dùng biết.
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

                // Nếu chấm thành công, chuyển hướng thẳng đến trang Kết quả
                return RedirectToAction("Result", new { id });
            }

            // Trả về View phòng phỏng vấn cho câu hỏi hiện tại
            return View("~/Views/Home/InterviewRoom.cshtml", room);
        }

        /// <summary>
        /// POST: /Interview/Transcribe
        /// [SPEECH] Nhận file ghi âm câu trả lời (multipart/form-data: sessionId + audio),
        /// chuyển thành chữ bằng Whisper rồi chuẩn hoá bằng Gemini. KHÔNG lưu gì vào database.
        /// -- Câu hỏi dùng làm ngữ cảnh được lấy từ server (không tin dữ liệu trình duyệt gửi lên).
        /// -- Chỉ chủ buổi phỏng vấn, khi buổi đang diễn ra, mới gọi được.
        /// </summary>
        /// <param name="sessionId">Mã buổi phỏng vấn đang diễn ra.</param>
        /// <param name="audio">File ghi âm (webm / ogg / mp4 / m4a / wav...).</param>
        /// <returns>JSON { rawText, text, normalized, empty } hoặc 400 { message, retry, configError }.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("SpeechRateLimit")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
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
                // Kiểm tra quyền + buổi đang diễn ra, đồng thời lấy câu hỏi hiện tại làm ngữ cảnh
                string question = await _interviewService.GetCurrentQuestionAsync(CurrentEmail, sessionId);

                byte[] data;
                using (var buffer = new MemoryStream((int)audio.Length))
                {
                    await audio.CopyToAsync(buffer, ct);
                    data = buffer.ToArray();
                }

                var clip = new AudioClip(data, "answer" + extension, mediaType);
                var result = await _speechToText.TranscribeAnswerAsync(clip, question, Truncate(browserText, 4000), ct);


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
                // configError = true: lỗi cấu hình phía server, trình duyệt dùng chữ tự nhận dạng thay vì thử lại
                return BadRequest(new { message = ex.Message, configError = ex.IsConfigurationError, retry = ex.IsTransient });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }

        /// <summary>
        /// POST: /Interview/Answer
        /// API nhận diện giọng nói / văn bản trả lời cho một câu hỏi.
        /// </summary>
        /// <param name="dto">Dữ liệu từ Frontend bao gồm nội dung câu trả lời hoặc lệnh Bỏ qua.</param>
        /// <returns>Dữ liệu JSON chứa câu hỏi tiếp theo, hoặc cờ Done = true nếu đã hết.</returns>
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
                // retry = true: trình duyệt xoá câu trả lời cũ để người dùng nói lại
                return BadRequest(new { message = ex.Message, retry = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }

        /// <summary>
        /// POST: /Interview/Finish
        /// API kích hoạt thủ công việc chấm điểm và kết thúc buổi phỏng vấn (thường dùng khi người dùng bấm Nộp bài sớm).
        /// </summary>
        /// <param name="dto">Mã buổi học và chỉ số lo lắng sau phỏng vấn.</param>
        /// <returns>URL để Frontend tự động redirect sang trang Kết quả.</returns>
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
                // configError = true: lỗi cấu hình (sai model/key), bấm "Chấm lại" vô ích -> giao diện mở ngay nút dự phòng
                return BadRequest(new { message = ex.Message, configError = ex.IsConfigurationError });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = SafeMessage(ex) });
            }
        }
        #endregion

        #region Result
        /// <summary>
        /// GET: /Interview/Result/{id}
        /// Hiển thị trang báo cáo kết quả đánh giá chi tiết từ AI.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var result = await _interviewService.GetResultAsync(CurrentEmail, id);

            // Nếu buổi phỏng vấn không tồn tại hoặc vẫn đang trong trạng thái InProgress (chưa nộp bài)
            if (result == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kết quả của buổi luyện này.";
                return RedirectToAction("History", "Home");
            }

            return View(result);
        }
        #endregion

        private static string? Truncate(string? text, int max) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Length <= max ? text : text[..max];

    }
}
