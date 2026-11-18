# MockViewAI — Trang chủ chia theo đợt

Trang chủ (`index.html`) được dựng dần qua 6 đợt. Mỗi đợt thêm **một phần HTML** vào `index.html`, **một file CSS** và (nếu cần) **một file JS** trong `assets/home/`. Đầu file `index.html` có dòng ghi đang ở đợt mấy.

`index.html` hiện đã gồm đủ cả 6 đợt (dòng đầu file ghi "đủ 6 đợt").

| Đợt | Phần trên trang | CSS | JS |
|---|---|---|---|
| 1 | Nền, thanh trên, hero 3D (quả cầu hạt, khối giao diện nghiêng), chân trang | `1-nen-hero.css` | `1-hero.js` |
| 2 | Số liệu khảo sát (4 ô), hành trình 3 giai đoạn (tab + thanh đo) | `2-so-lieu-hanh-trinh.css` | `2-hanh-trinh.js` |
| 3 | Sản phẩm: 3 thẻ, hàng 3 giao diện, hộp thoại demo phòng phỏng vấn | `3-san-pham.css` | `3-san-pham.js` |
| 4 | Bài tập thở 4–6 | `4-bai-tho.css` | `4-bai-tho.js` |
| 5 | Checklist + đăng nhập (Supabase, dùng `app.js` có sẵn) | `5-checklist.css` | `app.js` |
| 6 | 5 điều hay gặp, bài viết, nghiên cứu, kêu gọi cuối, nút cố định đáy điện thoại | `6-cuoi-trang.css` | `6-cuoi-trang.js` |

## Cách kiểm tra mỗi đợt

Mở `index.html` bằng Chrome hoặc Edge. Bấm **F12 → Console**: không được có dòng chữ đỏ. Bấm **Ctrl + Shift + M** để xem khổ điện thoại (chọn iPhone 12 Pro / 390px).

### Đợt 1 — Nền + hero
- [ ] Quả cầu hạt xoay chậm phía sau khối giao diện; rê chuột thì quả cầu và khối giao diện nghiêng theo.
- [ ] Đồng hồ "Thời gian suy nghĩ" trong khối giao diện đếm ngược 0:15 → 0:01 rồi lặp lại.
- [ ] Thẻ "Điểm tham khảo 78/100": hai thanh 81 và 73 chạy lên.
- [ ] Sàn lưới phía dưới chạy về phía trước.
- [ ] Khổ điện thoại: không có thanh cuộn ngang; nút ba gạch mở/đóng được menu.
- [ ] Biết trước: nút "Bắt đầu chuẩn bị" và "Xem số liệu khảo sát" chưa đi tới đâu (đích của chúng có ở đợt 5 và đợt 2).

### Đợt 2 — Số liệu + hành trình
- [ ] Cuộn tới 4 ô số: số đếm từ 0 lên 53 / 64,2% / 62,3% / 55,8%.
- [ ] Bấm 3 tab Trước / Trong / Sau: nội dung, màu quả cầu và thanh % đổi theo. Dùng phím ← → cũng chuyển được tab.
- [ ] Menu có thêm "Hành trình", "Số liệu"; mục đang xem được gạch chân.

### Đợt 3 — Sản phẩm
- [ ] Hàng 3 giao diện giống ảnh: phòng phỏng vấn, xem lại hội thoại, kết quả mẫu.
- [ ] Bấm thẻ phòng phỏng vấn → hộp demo mở; đồng hồ đếm 15 giây; bấm "Trả lời ngay" → "Đang ghi âm", chữ hiện dần; "Gửi câu trả lời" → câu hỏi nối tiếp; gửi tiếp → "Đã xong buổi".
- [ ] Đóng hộp bằng nút ×, phím Esc, hoặc bấm ra ngoài.

### Đợt 4 — Bài thở
- [ ] "Bắt đầu thở": quả cầu phồng 4 giây, thu 6 giây, đếm "Nhịp 1 / 6"; "Dừng" dừng ngay.

### Đợt 5 — Checklist + đăng nhập (cần mạng)
- [ ] Chưa đăng nhập: checklist bị che, có nút "Đăng nhập / Tạo tài khoản".
- [ ] Nhập email sai định dạng → báo lỗi tiếng Việt.
- [ ] Đăng ký → nhận email xác nhận → đăng nhập → tích mục → thanh tiến độ chạy, dòng "Đã lưu."
- [ ] F5 vẫn còn tiến độ; đăng xuất thì checklist trống và bị che lại.
- [ ] Lưu ý: 5 mục checklist dùng mã `truoc-01` … `truoc-05`; nội dung mới, nhóm cần duyệt.

### Đợt 6 — Cuối trang
- [ ] 5 thẻ "điều hay gặp" có % và link "Bằng chứng" mở đúng đoạn trong bài viết 4.
- [ ] 4 thẻ bài viết mở đúng bài.
- [ ] Khổ điện thoại: nút "Bắt đầu chuẩn bị" cố định ở đáy; ẩn khi ở đầu trang, ở checklist và ở cuối trang.
