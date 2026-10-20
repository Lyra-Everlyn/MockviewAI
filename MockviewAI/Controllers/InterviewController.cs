using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Interviews;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [AI-INTERVIEW]
    /// Controller chịu trách nhiệm quản lý toàn bộ vòng đời của một buổi phỏng vấn AI.
    /// Bao gồm: Khởi tạo, Phòng phỏng vấn, Ghi nhận câu trả lời, Chấm điểm và Xem kết quả.
    /// </summary>
    [Authorize]
    public class InterviewController : Controller
    {
        private readonly IInterviewService _interviewService;

        public InterviewController(IInterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        /// <summary>
        /// Bộ lọc lỗi an toàn. Chỉ hiển thị cho người dùng những lỗi logic được định nghĩa trước 
        /// (InterviewException, AiScoringException). Che giấu các lỗi hệ thống (Database, Network).
        /// </summary>
        private static string SafeMessage(Exception ex) =>
            ex is InterviewException or AiScoringException ? ex.Message : "Có lỗi xảy ra. Vui lòng thử lại.";

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
        /// <param name="anxietyBefore">Mức độ lo lắng trước phỏng vấn (1-10).</param>
        /// <returns>Chuyển hướng người dùng vào thẳng phòng phỏng vấn (Interview Room).</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string position, string interviewer, int questionCount, int? anxietyBefore)
        {
            try
            {
                int sessionId = await _interviewService.StartAsync(CurrentEmail, position, interviewer, questionCount, anxietyBefore);
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
            if (result == null) return NotFound();

            return View(result);
        }
        #endregion
    }
}