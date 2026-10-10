# Luồng phỏng vấn AI (phòng phỏng vấn nối với chấm điểm)

Tìm nhanh: `Ctrl+Shift+F` và gõ `[AI-INTERVIEW]`.

## Luồng

```
Create (form)  ->  POST Start  ->  Room/{id}  ->  POST Answer (lặp từng câu)  ->  POST Finish  ->  Result/{id}
                   tạo buổi        phòng chat      lưu câu trả lời                 gọi AI chấm         xem điểm
```

## Các file

| File | Việc |
|---|---|
| `Models/Entities/InterviewSession.cs` | Bảng buổi luyện (người dùng, vị trí, điểm, nhận xét) |
| `Models/Entities/SessionAnswer.cs` | Bảng câu hỏi và câu trả lời của từng buổi |
| `Data/ApplicationDbContext.cs` | Thêm 2 `DbSet` và quan hệ giữa các bảng |
| `Models/DTOs/InterviewDtos.cs` | Dữ liệu giữa giao diện và service |
| `Services/Ai/QuestionBank.cs` | Câu hỏi mẫu (3 người phỏng vấn x 10 câu), sửa trực tiếp trong file |
| `Services/Interfaces/IInterviewService.cs` | Danh sách việc của một buổi luyện |
| `Services/Implementations/InterviewService.cs` | Làm các việc đó và gọi `InterviewScoringService` |
| `Controllers/InterviewController.cs` | Các trang và API (cần đăng nhập) |
| `Views/Home/InterviewRoom.cshtml` | Phòng phỏng vấn của nhóm UI, đã nối dữ liệu thật |
| `wwwroot/js/InterviewRoom.js` | Nói hoặc gõ, gửi câu trả lời, sang câu tiếp, kết thúc |
| `Views/Interview/Create.cshtml`, `Result.cshtml` | Trang TẠM để thử, thay bằng giao diện của nhóm UI |

## Cập nhật database (bắt buộc)

Chạy trong Package Manager Console (chọn project `MockviewAI`):

```
Add-Migration AddInterviewTables
Update-Database
```

## Cho người làm giao diện

- **Form tạo buổi** (`CreateSession.cshtml`): đổi thành `asp-controller="Interview" asp-action="Start"`. Các ô phải có `name`: `position` (developer, tester, ba), `interviewer` (linh, nam, an), `questionCount` (5 hoặc 10). Có thể thêm ô `anxietyBefore` (số 1-10).
- **Trang kết quả** (`Result.cshtml`): đặt ở `Views/Interview/Result.cshtml`, dòng đầu là `@model MockviewAI.Models.DTOs.SessionResultViewModel`. Dùng các thuộc tính: `TotalScore`, `ContentScore`, `DeliveryScore`, `AnxietyBefore`, `AnxietyAfter`, `AiSummary`, `Strengths`, `Improvements`, `DeliveryTips`, `HasScore`.
- Phòng phỏng vấn cần giữ các `id`: `btn-speak`, `btn-type`, `btn-skip`, `btn-send`, `btn-finish`, `question-text`, `transcript-text`, `answer-input`.

## Ghi chú

- Nhận giọng nói dùng trình duyệt (Chrome, Edge). Trình duyệt khác thì dùng nút "Gõ câu trả lời".
- Điểm chỉ chấm các câu đã trả lời; câu "bỏ qua" không tính. Chưa trả lời câu nào thì buổi có trạng thái `Stopped` và không có điểm.
- Mức lo lắng sau buổi (`AnxietyAfter`) đã có trong API `Finish`, giao diện chưa có ô nhập.
