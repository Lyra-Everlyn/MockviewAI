# AiLoadTest — thử tải phần AI của MockviewAI

Giả lập N buổi phỏng vấn chạy chồng lên nhau. Công cụ gọi **Gemini / Groq thật** qua đúng các service của app, theo thứ tự:

1. Gợi ý vị trí.
2. Soạn câu theo CV / vị trí.
3. Với mỗi câu trả lời: Whisper (nếu có `--audio`), chuẩn hoá, rồi người phỏng vấn nhận xét.
4. Chấm điểm.

Kết quả gồm:
- Tỉ lệ bước chạy được bằng AI so với số lần app phải dùng đường dự phòng.
- Thời gian p50 / p95.
- Số lần mỗi model trả về 429 (hết lượt) hoặc 503 (quá tải).

Công cụ **không** đo sức chịu của web server hay DB, và không đụng DB / Redis. Nó chỉ đo hạn mức AI, vì đó là điểm nghẽn thật (xem `MockviewAI_Context/RUI_RO_DEMO_7_12.md`).

## Cảnh báo
- Công cụ dùng **lượt thật** của khoá trong `.env`. Nếu đó là khoá production thì nó dùng chung hạn mức với người dùng web.
- Hạn mức Gemini tính theo project và **từng model**: gói miễn phí `gemini-3.5-flash-lite` chỉ có 15 lệnh/phút (đo 27/11/2026).
- Hạn mức ngày đặt lại lúc 15:00 giờ Việt Nam.
- Chỉ chạy vào ngày khác ngày thi, lúc không có người dùng. Mỗi buổi tốn khoảng `4 + 2 × số câu` lệnh Gemini.

## Cách chạy (từ thư mục gốc repo)
Lệnh đọc `.env` ở thư mục gốc repo như app, và đọc thêm user-secrets nếu có trên máy dev. Chưa có `--yes` thì lệnh chỉ in kế hoạch, không gọi AI.

```powershell
dotnet run --project tools/AiLoadTest -- --sessions 5 --spread-minutes 2 --answers 5
```

Thêm `--yes` để chạy thật. Ví dụ giả lập 30 sinh viên vào trong 15 phút:

```powershell
dotnet run --project tools/AiLoadTest -- --sessions 30 --spread-minutes 15 --answers 5 --answer-gap 30 --yes
```

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| `--sessions` | 5 | Số buổi giả lập (1–200) |
| `--spread-minutes` | 2 | Rải thời điểm bắt đầu các buổi đều trong bấy nhiêu phút (0 = bấm cùng lúc) |
| `--answers` | 5 | Số câu mỗi buổi |
| `--answer-gap` | 20 | Số giây "nói" trung bình giữa các câu (±30%) |
| `--cv-file` | `sample-cvs.json` | Danh sách CV mẫu (CV giả, không có người thật), dùng xoay vòng |
| `--audio` | (không) | File ghi âm (wav/webm). Có thì gọi cả Whisper (mỗi câu một lượt) |
| `--no-scoring` | | Bỏ bước chấm điểm |
| `--verbose` | | In log thông tin của app (mặc định chỉ in cảnh báo) |
| `--yes` | | Bắt buộc để chạy thật |

Muốn thử cấu hình khác (ví dụ đổi model) thì đặt biến môi trường trước khi chạy, cùng tên như trên Dokploy, ví dụ `$env:Interviewer__Model = "gemini-3.1-flash-lite"`.

## Đọc kết quả
- **% AI** thấp ở bước "Người phỏng vấn": sinh viên nhận câu nhận xét soạn sẵn.
- **% AI** thấp ở bước "Soạn câu": buổi dùng câu bộ đề.
- **% AI** thấp ở bước "Chấm điểm": sinh viên phải bấm "Chấm lại" hoặc "chấm sơ bộ".
- Bảng mã HTTP cho biết model nào hết lượt (429) để tách tải sang model khác hoặc lên gói trả phí.
