# MockViewAI — PRD

> Cập nhật 09/11/2026 (chuẩn bị Demo Day). Bản đầu soạn cuối Buổi 2, sáu mục theo slide 21.
> Nhãn: **[ĐÃ XÁC NHẬN]** học viên đã chốt · **[ĐỀ XUẤT]** Mentor gợi ý · **[CẦN XÁC NHẬN]** chưa quyết.

## 1. Vấn đề

Sinh viên sắp ra trường, mới ra trường và người đi làm 1–2 năm thường **lo lắng, suy nghĩ tiêu cực, hoảng hốt** khi đi phỏng vấn xin việc và **chưa biết chuẩn bị gì, theo thứ tự nào**. [ĐÃ XÁC NHẬN]

Khảo sát của nhóm (53 phản hồi, 07–12/10/2026) cho thấy ví dụ: 64,2% nhớ câu trả lời chưa tốt rõ hơn câu tốt (S9), 49,1% lo kiến thức chưa đủ (S1). Mẫu thuận tiện nên chỉ mô tả 53 người này, không đại diện cho mọi người mới tốt nghiệp.

MockViewAI không thay thế tư vấn tâm lý và không cam kết trúng tuyển.

## 2. Mục tiêu MVP

**Câu MVP** [ĐÃ XÁC NHẬN]:

> Tôi tạo ra **MockViewAI — website hướng dẫn chuẩn bị phỏng vấn dựa trên số liệu thực tế** dành cho **sinh viên sắp ra trường, mới ra trường và người đi làm 1–2 năm** để giúp họ **tự làm lộ trình chuẩn bị trước buổi phỏng vấn**, và biết rằng MVP hoạt động khi **người dùng hoàn thành checklist và tự đánh giá mình bớt lo hơn trước khi đi phỏng vấn**.

**Phễu MVP (s11):** đối tượng rõ · vấn đề cụ thể · hành động đủ nhỏ (checklist) · kết quả kiểm được (số mục đã tích; tự đánh giá **chưa xây**, xem mục 6).

**Phương án đã chọn:** checklist chuẩn bị + bài thở, có tài khoản lưu tiến độ. Phỏng vấn thử với AI: trên trang chỉ giới thiệu bản thử nghiệm (ghi rõ "Đang thử nghiệm", "dữ liệu mẫu"), không phải chức năng chạy thật của website này.

## 3. Người dùng

- Sinh viên sắp ra trường, mới ra trường, người đi làm 1–2 năm sắp đi phỏng vấn. [ĐÃ XÁC NHẬN]
- Dùng cả máy tính và điện thoại. [ĐÃ XÁC NHẬN]
- Nội dung đọc (trang chủ, bài viết, nghiên cứu) công khai; **checklist cần đăng nhập** để lưu tiến độ riêng. [ĐÃ XÁC NHẬN]
- Không có vai trò khác (admin, nhà tuyển dụng).

## 4. Chức năng MVP

| # | Chức năng | Trạng thái 09/11 |
|---|---|---|
| 1 | Trang chủ: hero, số liệu khảo sát, hành trình 3 giai đoạn, sản phẩm, bài thở, checklist, 5 điều hay gặp, bài viết, nghiên cứu, kêu gọi cuối trang | Đã xây |
| 2 | Menu đủ mục; điện thoại có nút ba gạch | Đã xây |
| 3 | "Bắt đầu chuẩn bị" → đăng nhập (nếu chưa) → checklist | Đã xây |
| 4 | Đăng ký / đăng nhập email + mật khẩu (Confirm Email bật) | Đã xây, **chưa test bằng tài khoản thật** |
| 5 | Checklist 5 mục (`truoc-01…05`), thanh tiến độ, lưu Supabase theo từng người | Đã xây; **nội dung 5 mục [CẦN XÁC NHẬN — nhóm duyệt]** |
| 6 | Bài tập thở 4–6 (hít 4 giây, thở 6 giây, 6 nhịp) | Đã xây |
| 7 | Tự đánh giá mức lo trước/sau | **Chưa xây** — [CẦN XÁC NHẬN] xem mục 6 |
| 8 | 5 điều người mới hay gặp (theo khảo sát) + gợi ý thử thay | Đã xây |
| 9 | Số liệu có nguồn | Đã xây: số liệu khảo sát của nhóm + nguồn Việt Nam/quốc tế trong bài viết |
| 10 | Nghiên cứu có DOI, nút kiểm tra DOI qua Crossref (Edge Function) | Đã xây |
| 11 | 4 bài viết theo vấn đề + trang nghiên cứu nhóm + 18 trang tài liệu | Đã xây |
| 12 | Responsive; nút "Bắt đầu chuẩn bị" cố định đáy điện thoại | Đã xây |
| 13 | Khai báo sử dụng AI (quy chế MindX IV.2) | Đã xây; **nội dung bảng [CẦN XÁC NHẬN — nhóm đọc lại]** |

**Luồng chính:** trang chủ → "Bắt đầu chuẩn bị" → đăng nhập → checklist + bài thở → đăng nhập lại vẫn thấy tiến độ.

## 5. Chưa làm

- Tự đánh giá mức lo trước/sau (xem mục 6).
- Đăng nhập bằng Google: đã thử làm rồi bỏ (09/11) — trang chủ yếu để đọc, đăng nhập email đủ cho checklist.
- Phỏng vấn thử với AI chạy thật trên website (chỉ có bản demo giao diện, dữ liệu mẫu).
- Quiz, storytelling, biểu đồ cảm xúc tương tác.
- Thanh toán, app mobile riêng, trang admin, cộng đồng, email nhắc lịch.

*Để sau ≠ bỏ vĩnh viễn (s13).*

## 6. Tiêu chí đạt

**Đã đạt (đã kiểm bằng test tự động hoặc trên database)**
- [x] Bấm "Bắt đầu chuẩn bị" đi đúng luồng.
- [x] Tích một mục thì thanh tiến độ cập nhật đúng.
- [x] Trên điện thoại đọc được, bấm được, không tràn ngang (390px, 1440px).
- [x] Số liệu trên trang khớp file khảo sát; nguồn có DOI kiểm được trên Crossref.
- [x] RLS: người này không đọc/sửa được tiến độ người khác (test giả lập trên database).

**Chưa đạt — cần làm trước Demo Day**
- [ ] Đăng nhập lại vẫn thấy tiến độ đã lưu — **test bằng 2 tài khoản thật A/B** (database hiện chưa có tài khoản nào).
- [ ] Chạy trên **link công khai (Vercel)**, test lại A/B và nút kiểm tra DOI trên link đó.

**[CẦN XÁC NHẬN] về câu MVP:** câu MVP đo bằng "tự đánh giá mình bớt lo hơn", nhưng chức năng tự đánh giá chưa xây. Nhóm chọn một trong hai:
1. Xây thêm tự đánh giá mức lo (1 câu hỏi thang 1–5 trước và sau checklist, lưu theo tài khoản), hoặc
2. Sửa câu MVP: "biết MVP hoạt động khi người dùng **hoàn thành checklist** và tiến độ được lưu riêng cho từng người".
