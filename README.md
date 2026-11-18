# MockViewAI — Chuẩn bị phỏng vấn dựa trên số liệu

Dự án học tập của **nhóm Ngũ Hổ Tướng** (Trần Minh Quang – Kurumi và nhóm), khóa **Vibecode (MindX)**, kết hợp nghiên cứu Unit 16 (BTEC FPT Hà Nội).

MockViewAI giúp sinh viên sắp ra trường, mới ra trường và người đi làm 1–2 năm **tự lập lộ trình chuẩn bị trước buổi phỏng vấn**: checklist lưu theo tài khoản, bài tập thở, số liệu khảo sát 53 người và các bài viết có nguồn kiểm chứng được.

> MockViewAI không thay thế tư vấn tâm lý và không cam kết trúng tuyển.

## Trang web gồm gì

| Trang | File | Nội dung |
|---|---|---|
| Trang chủ | `index.html` + `assets/home/` | Hero, số liệu khảo sát, hành trình 3 giai đoạn, sản phẩm, bài thở 4–6, checklist (cần đăng nhập), 5 điều hay gặp, bài viết |
| 4 bài viết | `bai-viet/*.html` | Áp lực tìm việc · Vì sao lo âu · Lo âu và kết quả · Điều gì giúp giảm lo âu (có trích dẫn, nút kiểm tra DOI) |
| Nghiên cứu nhóm | `nghien-cuu/index.html` | Câu hỏi, phương pháp, kết quả 53 phản hồi + 4 chuyên gia, giả thuyết, giới hạn, khai báo sử dụng AI |
| 18 trang tài liệu | `nghien-cuu/tai-lieu/*.html` | Tóm tắt từng tài liệu, sơ đồ, link bài gốc (DOI) |

## Chạy trên máy

1. Mở thư mục này bằng **VS Code**, cài extension **Live Server**.
2. Chuột phải `index.html` → **Open with Live Server** → trang mở ở `http://127.0.0.1:5500/index.html`.
3. Cần có mạng (font, thư viện Supabase, Edge Function).

Nhấp đúp `index.html` cũng xem được, nhưng nên dùng Live Server để đường dẫn và đăng nhập chạy giống khi đưa online.

## Kỹ thuật

- HTML, CSS, JavaScript thuần (không cần build). Riêng trang nghiên cứu: sửa `nghien-cuu/data/*.json` xong chạy `node nghien-cuu/build-data.js`.
- **Supabase** (project `mockviewai`, Singapore):
  - Đăng nhập: email + mật khẩu (Confirm Email bật). Không dùng đăng nhập Google (nhóm quyết định bỏ 09/11).
  - Bảng `checklist_progress`: 1 dòng = 1 mục checklist của 1 người. RLS: `auth.uid() = user_id`, không cho xóa. Lịch sử thay đổi: `supabase/migrations/`.
  - Edge Function `doi-check`: trình duyệt → function → Crossref API → trả tên bài, tạp chí, năm. Không cần khóa bí mật.
- Frontend chỉ chứa **publishable key** (khóa công khai dành cho trình duyệt). Không có khóa bí mật nào trong code; `.gitignore` chặn `.env`.

## Hồ sơ dự án (`docs/`)

| File | Dùng để |
|---|---|
| `project-overview-prd.md` | Xây gì, cho ai, chưa xây gì, tiêu chí đạt |
| `design-guidelines.md` | Màu, chữ, bố cục, điều tránh |
| `development-plan.md` | Thứ tự các bước và cách kiểm tra từng bước |
| `progress.md` | Đã làm / đang làm / bị chặn / quyết định / tiếp theo |
| `huong-dan-online.md` | Đưa lên GitHub + Vercel, khai báo địa chỉ trang trên Supabase |
| `kiem-thu-va-demo.md` | Bài test A/B, test online và kịch bản Demo Day |
| `cac-dot-giao-dien.md` | Trang chủ chia theo 6 đợt và cách kiểm từng đợt |

## Sử dụng AI

Dự án có dùng AI hỗ trợ (Claude, Consensus, Google Stitch). Phần nào AI hỗ trợ, nhóm tự làm và kiểm tra gì: xem **[Khai báo sử dụng AI](nghien-cuu/index.html#su-dung-ai)** (theo quy chế MindX mục IV.2).
