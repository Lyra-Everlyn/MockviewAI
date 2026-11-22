using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;
using MockviewAI.Services.CvParser;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [USER CV] 
    /// /CV (danh sách) -> /CV/UploadCv (tải lên, kiểm tra) -> /CV/SaveCv (chốt, trình duyệt lưu).
    /// Server không lưu CV lâu dài.
    /// </summary>
    [Authorize]
    [Route("CV")]
    public class CvController : Controller
    {
        #region Fields
        private const string ViewFolder = "~/Views/CV/";
        private const long MaxRequestBytes = 11 * 1024 * 1024;

        private static readonly JsonSerializerOptions BrowserJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly ICvService _cv;
        private readonly ICvFileValidator _validator;
        private readonly CvOptions _options;
        private readonly ILogger<CvController> _logger;
        #endregion

        #region Constructor
        public CvController(ICvService cv, ICvFileValidator validator, IOptions<CvOptions> options, ILogger<CvController> logger)
        {
            _cv = cv;
            _validator = validator;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Thư viện CV
        /// <summary>GET /CV: danh sách CV, do CvLibrary.js dựng từ trình duyệt.</summary>
        [HttpGet("")]
        public IActionResult Index() => View(ViewFolder + "Index.cshtml", Fill(new CvPageViewModel()));
        #endregion

        #region Tải lên và phân tích
        [HttpGet("UploadCv")]
        public IActionResult UploadCv() => View(ViewFolder + "UploadCV.cshtml", Fill(new CvUploadViewModel()));

        /// <summary>POST /CV/UploadCv: kiểm tra file rồi AI trích xuất; hiện lại trang để người dùng sửa trước khi chốt.</summary>
        [HttpPost("UploadCv")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(CvServiceExtensions.RateLimitPolicy)]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> UploadCv(IFormFile? cvFile, CancellationToken ct)
        {
            var model = Fill(new CvUploadViewModel());

            var error = await _validator.ValidateAsync(cvFile, ct);
            if (error != null)
            {
                model.ErrorMessage = error;
                return View(ViewFolder + "UploadCV.cshtml", model);
            }

            try
            {
                model.ParsedData = await _cv.AnalyzeAsync(CurrentEmail, cvFile!, ct);
                model.IsParsed = true;
            }
            catch (CvException ex)
            {
                model.ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "CV analysis failed unexpectedly");
                model.ErrorMessage = "Đã xảy ra lỗi khi phân tích CV. Vui lòng thử lại.";
            }

            return View(ViewFolder + "UploadCV.cshtml", model);
        }
        #endregion

        #region Chốt CV
        /// <summary>POST /CV/SaveCv: chốt CV rồi mở trang Saved để trình duyệt lưu.</summary>
        [HttpPost("SaveCv")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCv([Bind(Prefix = "ParsedData")] CvParsedDataDto edited, string? hardSkillsString, CancellationToken ct)
        {
            try
            {
                var confirmed = await _cv.ConfirmAsync(CurrentEmail, edited, hardSkillsString, ct);
                if (confirmed == null)
                {
                    TempData["ErrorMessage"] = "Phiên kiểm tra CV đã hết hạn. Vui lòng tải CV lên lại.";
                    return RedirectToAction(nameof(UploadCv));
                }

                var model = Fill(new CvSavedViewModel { CvJson = JsonSerializer.Serialize(confirmed, BrowserJson) });
                return View(ViewFolder + "Saved.cshtml", model);
            }
            catch (CvException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Confirming CV failed unexpectedly");
                TempData["ErrorMessage"] = "Không lưu được CV. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(UploadCv));
        }
        #endregion

        #region Helpers
        /// <summary>Gắn khoá chủ sở hữu và giới hạn lưu trữ trên trình duyệt (đã kẹp về khoảng an toàn) cho view.</summary>
        private T Fill<T>(T model) where T : CvPageViewModel
        {
            model.OwnerKey = CvOwnerKey.For(CurrentEmail);
            model.MaxCvs = Math.Clamp(_options.MaxCvsPerUser, 1, 10);
            model.KeepDays = Math.Clamp(_options.BrowserKeepDays, 1, 365);
            return model;
        }
        #endregion
    }
}
