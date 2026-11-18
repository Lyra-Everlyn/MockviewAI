# Trang nghiên cứu – nhóm Ngũ Hổ Tướng (Unit 16)

Trang tĩnh một trang: HTML + CSS + JS thuần, không cần backend, không thư viện ngoài (chỉ font Be Vietnam Pro từ Google Fonts).

## Mở trang
- Nhấp đúp `index.html` là chạy (kể cả không có mạng, chỉ thiếu font).

## Sửa số liệu
Mọi con số, trích dẫn, tài liệu đều đọc từ `data/*.json` – **không có số nào viết cứng trong `index.html`**, trừ các đoạn "Đọc biểu đồ này thế nào" (lấy nguyên văn từ prompt §5).
1. Sửa `data/survey.json`, `data/research.json` hoặc `data/references.json`.
2. Chạy `node build-data.js` để cập nhật `data/data.bundle.js` (file trang thật sự đọc).
3. Nếu số liệu đổi, sửa luôn đoạn chữ tương ứng trong mục "Đọc biểu đồ này thế nào" ở `index.html`.

Trường nào là `null` trong JSON thì dòng đó tự ẩn.

## Cấu trúc
| File | Dùng để |
|---|---|
| `index.html` | khung 11 mục + lời giải thích biểu đồ |
| `app.js` | vẽ biểu đồ, panel tài liệu, lightbox ảnh gốc, menu |
| `styles.css` | màu sáng/tối (token ở đầu file), bố cục |
| `data/` | dữ liệu gốc (JSON) + bản gom `data.bundle.js` |
| `assets/evidence/` | ảnh gốc Google Forms và sơ đồ báo cáo |
| `assets/product/` | ảnh chụp MockviewAI (dữ liệu mẫu) |

## Đưa lên mạng
- **Vercel:** Import repo GitHub → Framework *Other* → Deploy (không cần lệnh build).
- **GitHub Pages:** Settings → Pages → Branch `main`, thư mục `/ (root)`.

## Liên kết thẳng tới một tài liệu
`…/index.html#ref-zielinska2021` mở sẵn panel của tài liệu đó (id lấy trong `references.json`).
