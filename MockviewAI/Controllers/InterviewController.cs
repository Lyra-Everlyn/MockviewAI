// [AI-INTERVIEW] Các trang và API của buổi luyện phỏng vấn.
//   GET  /Interview/Create        form chọn vị trí, người phỏng vấn (tạm, giao diện chính thức sẽ thay)
//   POST /Interview/Start         tạo buổi luyện rồi chuyển vào phòng phỏng vấn
//   GET  /Interview/Room/{id}     phòng phỏng vấn (Views/Home/InterviewRoom.cshtml)
//   POST /Interview/Answer        lưu câu trả lời hoặc bỏ qua (JSON)
//   POST /Interview/Finish        chấm điểm và kết thúc (JSON)
//   GET  /Interview/Result/{id}   trang kết quả

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Controllers
{
    [Authorize]
    public class InterviewController : Controller
    {
        private readonly IInterviewService _interviewService;

        public InterviewController(IInterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        // Chỉ hiện thông báo của lỗi mình chủ động ném. Lỗi bất ngờ (database, mạng) thì hiện câu chung.
        private static string SafeMessage(Exception ex) =>
            ex is InterviewException or AiScoringException ? ex.Message : "Có lỗi xảy ra. Vui lòng thử lại.";

        // Email của người đang đăng nhập (đã được lưu vào cookie lúc đăng nhập)
        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Start
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Tên tham số khớp với các ô trong form CreateSession: position, interviewer, questionCount
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
        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _interviewService.GetRoomAsync(CurrentEmail, id);
            if (room == null)
            {
                // Hết câu hỏi mà chưa chấm điểm (hoặc buổi đã kết thúc): chấm luôn rồi xem kết quả
                try
                {
                    await _interviewService.FinishAsync(CurrentEmail, new FinishSessionDto { SessionId = id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = SafeMessage(ex);
                    return RedirectToAction("Create");
                }
                return RedirectToAction("Result", new { id });
            }

            // Dùng giao diện phòng phỏng vấn của nhóm UI
            return View("~/Views/Home/InterviewRoom.cshtml", room);
        }

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
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var result = await _interviewService.GetResultAsync(CurrentEmail, id);
            if (result == null) return NotFound();

            return View(result);
        }
        #endregion
    }
}
