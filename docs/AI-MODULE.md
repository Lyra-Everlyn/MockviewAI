# Module AI chấm phỏng vấn (MockviewAI)

Tìm nhanh trong VS Code: nhấn `Ctrl+Shift+F` và gõ `[AI-MODULE]` để thấy tất cả file của module.

## Dùng trong code (cho bạn làm phòng phỏng vấn)

```csharp
// 1. Inject vào controller
private readonly InterviewScoringService _scoring;

// 2. Gọi chấm điểm
var result = await _scoring.ScoreAsync(new ScoreRequest
{
    Position = "Lập trình viên",
    Answers = new()
    {
        new InterviewAnswer {
            Question = "Hãy giới thiệu về bản thân",
            Answer = "Em là ...",                 // chữ từ speech-to-text hoặc gõ tay
            DurationSeconds = 45,                  // null nếu gõ tay
            PausesSeconds = new() { 1.2, 3.0 }     // tuỳ chọn
        }
    }
});

// 3. Dùng kết quả
// result.Total               -> điểm tổng 0-100 (hiển thị là "Điểm tham khảo")
// result.Content.Summary / Strengths / Improvements -> nhận xét của AI
// result.Delivery.Tips       -> lời khuyên về cách nói
```

`ScoreAsync` có thể ném `AiScoringException` (thông báo tiếng Việt, hiển thị được cho người dùng). Hãy `try/catch` và show message.

## Luồng chấm điểm

```
ScoreRequest
   ├─> IAiScoringService (Mock hoặc Gemini) -> điểm NỘI DUNG, 4 tiêu chí x 25  (trọng số 60%)
   └─> DeliveryAnalyzer (code thuần)        -> điểm CÁCH NÓI, 0-100            (trọng số 40%)
                    └─> InterviewScoringService gộp -> InterviewScoreResult.Total
```

## Các file

| File | Việc |
|---|---|
| `Models/Ai/*.cs` | Lớp dữ liệu vào/ra, mỗi file một lớp (ScoreRequest, InterviewAnswer, ContentScoreResult, DeliveryScoreResult, InterviewScoreResult) |
| `Services/Ai/Interfaces/IInterviewScoringService.cs` | Giao diện điểm vào chính |
| `Services/Ai/Implementations/InterviewScoringService.cs` | Điểm vào chính, gộp 60/40 |
| `Services/Ai/Interfaces/IAiScoringService.cs` | Giao diện bộ chấm nội dung |
| `Services/Ai/Implementations/MockAiScoringService.cs` | Bản giả, không cần key |
| `Services/Ai/Implementations/GeminiAiScoringService.cs` | Bản gọi Gemini thật (chỉ gửi request) |
| `Services/Ai/Implementations/ScoringPromptBuilder.cs` | Soạn prompt, chống prompt injection |
| `Services/Ai/Implementations/GeminiResponseParser.cs` | Đọc phản hồi Gemini, ép điểm về 0..25 |
| `Services/Ai/Interfaces/IDeliveryAnalyzer.cs`, `Services/Ai/Implementations/DeliveryAnalyzer.cs` | Chấm tốc độ, từ đệm, độ dài, khoảng lặng |
| `Services/Ai/GeminiOptions.cs` | Cấu hình ApiKey/Model |
| `Services/Exceptions/AiScoringException.cs` | Lỗi của bộ chấm AI |

## Cấu hình

| Khóa cấu hình | Biến môi trường (Docker) | Ý nghĩa |
|---|---|---|
| `Ai:Provider` | `Ai__Provider` | `Mock` (mặc định, không tốn tiền) hoặc `Gemini` |
| `Gemini:ApiKey` | `Gemini__ApiKey` | Key lấy ở Google AI Studio hoặc Google Cloud. **Không commit** |
| `Gemini:Model` | `Gemini__Model` | Mặc định `gemini-3.8-flash`. Phải là model **văn bản** (không dùng `-tts`, `-live`, `-image`, `embedding`: hệ thống sẽ báo lỗi cấu hình). Tên model có thể đổi theo Google: kiểm tra danh sách model hiện hành và chỉ sửa khóa này, không sửa code. Lỗi 404 = sai tên model; log ghi rõ lý do Google trả về |

Chạy từ Visual Studio thì đặt bằng `dotnet user-secrets` (xem lệnh mẫu cuối file `.env.example`). Chưa có key thì để `Ai:Provider=Mock`: giao diện vẫn chạy với điểm mẫu có nhãn `[MOCK]`.

## Bảo mật đã làm

- Câu trả lời được bọc trong thẻ `<interview>`, bỏ ký tự `<` `>`, tối đa 10 câu, mỗi câu 2000 ký tự (chống prompt injection, giới hạn chi phí).
- Điểm trả về luôn bị ép vào khoảng 0..25.
- API key không được log; lỗi chỉ log mã HTTP, không log nội dung người dùng.
- Dữ liệu câu trả lời được gửi tới Google (Gemini) khi bật `Gemini`: đúng với Proposal/Ethics đã ký, nhưng vẫn cần ghi trong chính sách bảo mật.
- Nên đặt budget alert trong Google Cloud ngay từ đầu để tránh phát sinh chi phí ngoài ý muốn.
