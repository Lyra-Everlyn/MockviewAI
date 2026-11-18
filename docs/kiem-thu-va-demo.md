# Kiểm thử trước Demo Day + kịch bản demo

> Làm trên **link Vercel** (cửa sổ ẩn danh hoặc điện thoại). Mỗi bước đạt thì tích ô và **chụp màn hình** lưu vào một thư mục `bang-chung/` (không đưa ảnh có mật khẩu).
> Tài khoản test A/B: tạo trong Supabase → Authentication → Users → **Add user**, chọn **Auto Confirm User** (slide Buổi 5). Mật khẩu tự đặt, không gửi vào chat AI.

## 1. Tài khoản và đăng nhập (Buổi 5 · Nhịp 1)
- [ ] Chưa đăng nhập: checklist bị khóa, có nút "Đăng nhập / Tạo tài khoản".
- [ ] Nhập email sai định dạng → báo lỗi tiếng Việt.
- [ ] Đăng nhập A → thanh trên hiện email A.
- [ ] F5 → vẫn là A.
- [ ] Đăng xuất → checklist khóa lại, ô tích trống.
- [ ] Đăng nhập B → hiện email B.

## 2. Dữ liệu riêng từng người (Buổi 4 + Buổi 5 · Nhịp 2)
- [ ] A tích 2 mục → hiện "Đã lưu.".
- [ ] F5 → A vẫn 2 mục.
- [ ] Đăng xuất A, đăng nhập B → B thấy **0/5** (không thấy mục của A).
- [ ] B tích 1 mục → "Đã lưu.".
- [ ] Đăng xuất B, đăng nhập lại A → A vẫn đúng 2 mục của A.
- [ ] Supabase → Table Editor → `checklist_progress`: có 3 dòng, 2 dòng mang `user_id` của A, 1 dòng của B.
- [ ] Tắt mạng rồi tích 1 mục → **không** hiện "Đã lưu.", ô tích trả về như cũ.

## 3. Kết nối dịch vụ ngoài (Buổi 6)
- [ ] Mở một bài viết hoặc trang tài liệu → bấm "Kiểm tra DOI" → hiện "Đang kiểm tra…" rồi tên bài, tạp chí, năm khớp với trích dẫn.
- [ ] Bấm liên tục nhiều lần không bị gửi trùng.
- [ ] F12 → Network: yêu cầu chỉ gửi DOI, không có khóa bí mật.

## 4. Giao diện
- [ ] Điện thoại: không có thanh cuộn ngang; menu ba gạch mở/đóng được; nút "Bắt đầu chuẩn bị" cố định đáy.
- [ ] F12 → Console: không có dòng chữ đỏ ở trang chủ, 1 bài viết, trang nghiên cứu.
- [ ] Trang nghiên cứu: Ctrl K mở ô tìm nhanh; bấm 1 trong 4 bài nổi bật → mở trang tài liệu → "Đọc bài gốc" mở đúng bài báo ở tab mới.

## 5. Kịch bản demo (khoảng 5 phút)

| Thời gian | Nói gì | Bấm gì |
|---|---|---|
| 0:00–0:40 | Vấn đề: người mới tốt nghiệp lo khi phỏng vấn, không biết chuẩn bị gì, theo thứ tự nào | Trang chủ, hero |
| 0:40–1:20 | Bằng chứng: khảo sát 53 người (ví dụ 64,2% nhớ câu trả lời chưa tốt rõ hơn câu tốt); mẫu nhỏ, chỉ mô tả nhóm này | Khối số liệu, hành trình 3 giai đoạn |
| 1:20–2:30 | Hành động chính: đăng nhập → tích checklist → F5 vẫn còn → đổi tài khoản B không thấy dữ liệu A (RLS) | Checklist, đăng nhập A/B |
| 2:30–3:10 | Bài thở 4–6 trước khi vào phòng | Bài thở |
| 3:10–4:10 | Nguồn đáng tin: mở bài viết → "Kiểm tra DOI" (trình duyệt → Edge Function → Crossref) | Bài 4, nút DOI |
| 4:10–4:50 | Nghiên cứu nhóm: câu hỏi, kết quả, giới hạn, khai báo sử dụng AI | Trang nghiên cứu |
| 4:50–5:00 | Bước tiếp theo: thực nghiệm đo lo âu trước–sau, mẫu lớn hơn | — |

**Phương án dự phòng:** mở sẵn tab trang chủ đã đăng nhập; có ảnh chụp màn hình từng bước trong `bang-chung/` phòng khi mạng lỗi.

## 6. Câu hỏi có thể gặp
- *RLS là gì, sao B không thấy dữ liệu A?* — Database tự kiểm tra `auth.uid() = user_id` trước khi trả dữ liệu; ẩn trên giao diện không phải là bảo mật.
- *Khóa trong code có lộ không?* — Chỉ có publishable key (dành cho trình duyệt); quyền thật do RLS quyết định. Crossref không cần khóa nên Edge Function không chứa bí mật.
- *Vì sao chọn Crossref mà không phải Telegram/AI?* — Chủ đề là đọc nghiên cứu có nguồn; Crossref giúp người đọc tự kiểm tra DOI có thật.
- *AI làm phần nào?* — Xem bảng "Khai báo sử dụng AI" trên trang nghiên cứu; mỗi thành viên phải giải thích được phần mình phụ trách (quy chế IV.2.2).
- *MockViewAI có giúp giảm lo âu không?* — Chưa kiểm định; đây là bước tiếp theo (thực nghiệm trước–sau).
