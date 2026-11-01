using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;
using MockviewAI.Services.CvParser;
using System.Text.Json;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [USER CV]
    /// -- Nhận CV người dùng và gọi dich vụ xử lý dữ liệu
    /// </summary>
    public class CvController : Controller
    {
        private readonly ICvExtractionService _cvExtractionService;
        private readonly IHttpClientFactory _httpClientFactory;

        public CvController(ICvExtractionService cvExtractionService, IHttpClientFactory httpClientFactory)
        {
            _cvExtractionService = cvExtractionService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult UploadCv()
        {
            return View(new CvUploadViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> UploadCv(CvUploadViewModel model)
        {
            if (model.CvFile == null || model.CvFile.Length == 0)
            {
                model.ErrorMessage = "Vui lòng chọn một file CV.";
                return View(model);
            }

            // 1. Kiểm tra định dạng file
            var extension = Path.GetExtension(model.CvFile.FileName).ToLower();
            if (extension != ".pdf" && extension != ".jpg" && extension != ".png" && extension != ".jpeg")
            {
                model.ErrorMessage = "Định dạng file không được hỗ trợ. Vui lòng tải lên PDF hoặc ảnh (JPG/PNG).";
                return View(model);
            }

            try
            {
                // 2. Trích xuất văn bản thô và nén bằng PdfPig/Tesseract
                string rawText = await _cvExtractionService.ExtractAndCleanTextAsync(model.CvFile);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    model.ErrorMessage = "Không thể trích xuất văn bản từ file. Vui lòng thử file khác rõ nét hơn.";
                    return View(model);
                }

                // 3. (Mô phỏng) Gọi AI Service với System Prompt bạn đã lưu ở bước trước
                // string jsonResultFromAi = await _aiService.ParseCvText(rawText);

                // MÔ PHỎNG DỮ LIỆU JSON TRẢ VỀ TỪ AI ĐỂ TEST UI:
                string dummyJson = @"{
                  ""FullName"": ""Nguyễn Văn A"",
                  ""TargetJobTitle"": ""Software Engineer"",
                  ""CareerObjective"": ""Trở thành chuyên gia phát triển phần mềm."",
                  ""TotalYearsOfExperience"": 2.5,
                  ""WorkExperiences"": [], ""HardSkills"": [""C#"", "".NET""], ""SoftSkills"": [""Teamwork""], ""Educations"": [], ""CertificationsAndAwards"": []
                }";

                // 4. Deserialize JSON thành DTO
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsedData = JsonSerializer.Deserialize<CvParsedDataDto>(dummyJson, options);

                // 5. Trả kết quả về View để người dùng review
                model.ParsedData = parsedData;
                model.IsParsed = true;

                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Đã xảy ra lỗi trong quá trình xử lý: {ex.Message}";
                return View(model);
            }
        }
    }
}
