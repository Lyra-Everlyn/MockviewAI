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
| `Models/Ai/InterviewModels.cs` | Lớp dữ liệu vào/ra |
| `Services/Ai/InterviewScoringService.cs` | Điểm vào chính, gộp 60/40 |
| `Services/Ai/IAiScoringService.cs` | Giao diện bộ chấm nội dung |
| `Services/Ai/MockAiScoringService.cs` | Bản giả, không cần key |
| `Services/Ai/GeminiAiScoringService.cs` | Bản gọi Gemini thật |
| `Services/Ai/DeliveryAnalyzer.cs` | Chấm tốc độ, từ đệm, độ dài, khoảng lặng |
| `Services/Ai/AiOptions.cs` | Cấu hình ApiKey/Model |

## Cấu hình

| Khóa cấu hình | Biến môi trường (Docker) | Ý nghĩa |
|---|---|---|
| `Ai:Provider` | `Ai__Provider` | `Mock` (mặc định, không tốn tiền) hoặc `Gemini` |
| `Gemini:ApiKey` | `Gemini__ApiKey` | Key lấy ở Google AI Studio hoặc Google Cloud. **Không commit** |
| `Gemini:Model` | `Gemini__Model` | Mặc định `gemini-2.5-flash`. Tên model có thể đổi theo Google: kiểm tra danh sách model hiện hành và chỉ sửa khóa này, không sửa code |

Chạy từ Visual Studio thì đặt bằng `dotnet user-secrets` (xem lệnh mẫu cuối file `.env.example`). Chưa có key thì để `Ai:Provider=Mock`: giao diện vẫn chạy với điểm mẫu có nhãn `[MOCK]`.

## Bảo mật đã làm

- Câu trả lời được bọc trong thẻ `<interview>`, bỏ ký tự `<` `>`, tối đa 10 câu, mỗi câu 2000 ký tự (chống prompt injection, giới hạn chi phí).
- Điểm trả về luôn bị ép vào khoảng 0..25.
- API key không được log; lỗi chỉ log mã HTTP, không log nội dung người dùng.
- Dữ liệu câu trả lời được gửi tới Google (Gemini) khi bật `Gemini`: đúng với Proposal/Ethics đã ký, nhưng vẫn cần ghi trong chính sách bảo mật.
- Nên đặt budget alert trong Google Cloud ngay từ đầu để tránh phát sinh chi phí ngoài ý muốn.
