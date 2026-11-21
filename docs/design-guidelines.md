# MockViewAI — Design Guideline

> Cập nhật 21/11/2026 (rút còn 2 giao diện, bỏ các chi tiết trang trí kiểu AI). Năm mục theo slide 22.
> Bản Buổi 1 (Stitch, tông navy sáng, font Lora) **đã được thay** bằng giao diện tối ngày 09/11 theo yêu cầu học viên; bản Stitch giữ trong `stitch_v_ng_v_ng_landing_page/` để đối chiếu.
> Nhãn: **[ĐÃ XÁC NHẬN]** · **[ĐỀ XUẤT]** · **[CẦN XÁC NHẬN]**.

## 1. Phong cách

- Nghiêm túc, chuyên nghiệp, **điềm tĩnh như đêm trước buổi phỏng vấn**: nền tối, một nguồn sáng ấm (đèn bàn), hình cánh cửa hé mở ở hero sáng dần theo tiến độ checklist. [ĐỀ XUẤT 21/11]
- Hai giao diện: **Tối** (mặc định, theo máy) và **Sáng**. Ba theme Phòng chờ / Phòng luyện với AI / Sau buổi phỏng vấn đã ẩn khỏi menu; ai đã lưu theme cũ được đưa về Tối hoặc Sáng. Code CSS của ba theme vẫn còn trong `assets/themes.css`, xóa khi nhóm thống nhất. [CẦN XÁC NHẬN]
- Trang nghiên cứu đọc như bài báo học thuật: số liệu có nguồn, nêu giới hạn, không quiz. [ĐÃ XÁC NHẬN]
- Xưng "bạn", đồng cảm, không phán xét. "Bắt đầu chuẩn bị" là nút nổi bật nhất.

| | So với bản Buổi 1 (Stitch) |
|---|---|
| **Giữ** | Menu, hành trình 3 giai đoạn, bài thở, checklist, nút cố định đáy điện thoại, font Be Vietnam Pro |
| **Đổi** | Nền sáng navy → **nền tối**; Lora → **Space Grotesk** cho tiêu đề; "Vững Vàng" → **MockViewAI**; câu chuyện → nghiên cứu có DOI |
| **Bỏ** | Mạng xã hội giả, tên người và lời chứng tự bịa, dòng "Không cần đăng ký" |

## 2. Bộ màu

Biến màu đặt trong `assets/home/1-nen-hero.css` (trang chủ) và `nghien-cuu/styles.css` (trang nghiên cứu, cùng hệ màu).

| Vai trò | Mã |
|---|---|
| Nền / nền phụ | `#070b16` / `#0b1122` (nghiên cứu: `#0a0f1c`) |
| Thẻ / thẻ nổi | `#0f172a` / `#151f38` |
| Chữ chính / chữ phụ / chữ mờ | `#f1f5f9` / `#cbd5e1` / `#94a3b8` |
| Nhấn chính (liên kết, nút) | `#6d8bff` (nhạt: `#a5b8ff`) |
| Cyan (điểm sáng, viền chọn) | `#22d3ee` |
| Tím | `#8b5cf6` |
| 3 giai đoạn Trước / Trong / Sau | `#6aa3e8` / `#a08cf0` / `#d98bbb` |
| Đồng ý / cảnh báo | `#4fc1b4` / `#f0b84a` |

Màu trong biểu đồ luôn đi kèm chữ hoặc nhãn, không dùng màu làm cách duy nhất để phân biệt.

## 3. Kiểu chữ

- Tiêu đề: **Space Grotesk** (500–700). Nội dung: **Be Vietnam Pro** (dấu tiếng Việt rõ).
- Chữ nội dung ≥ 15px trên điện thoại; giãn dòng 1,7–1,8.
- Số liệu so sánh dùng chữ số đều cột (`tabular-nums`).

## 4. Bố cục

Thứ tự khối trang chủ (cập nhật 21/11, theo nguyên tắc "cho dùng trước, xin tài khoản sau"):
1. Thanh trên + menu (điện thoại: ba gạch)
2. Hero: lời giới thiệu, câu hỏi "Buổi phỏng vấn của bạn khi nào?" (dẫn tới đúng công cụ), giao diện mẫu đặt thẳng (ghi "dữ liệu mẫu"), cánh cửa 3D phía sau
3. Bài thở 4–6: vòng tiến độ trên sàn (40% hít / 60% thở) + đèn bàn sáng theo nhịp. Không cần tài khoản
4. Checklist 5 việc: chưa đăng nhập vẫn tích được (lưu tạm trên trình duyệt); đăng nhập thì việc đã tích được chuyển sang tài khoản
5. Số liệu khảo sát: một phát hiện chính vẽ thành 53 chấm (mỗi chấm một người), ba phát hiện viết thành câu, mục đóng sẵn "Cách tính và giới hạn"
6. Hành trình: đường thời gian Trước / Trong / Sau (là tab); mỗi giai đoạn có danh sách phát biểu với thanh 3 đoạn (đồng ý / phân vân / không đồng ý) và gợi ý "Thử", bên cạnh là việc nên làm. Đã gộp khối "5 điều người mới hay gặp" vào đây
7. Sản phẩm (2 công cụ đang chạy, 1 đang thử, giao diện mẫu, demo)
8. Bài viết (băng chuyền 4 bài) + khối nghiên cứu nhóm
9. Kêu gọi cuối trang (dẫn tới checklist), chân trang (có link "Khai báo sử dụng AI")

Số liệu, cách viết 3 lớp: (1) câu nói thường, (2) số người cụ thể "34 trên 53", dùng đúng mẫu số của từng câu (S2, S3, S4, S12 tính trên 52), (3) mã câu, thang đo, giới hạn để trong "Cách tính". Hiện cả phần phân vân và không đồng ý, không chỉ phần đồng ý. Số lấy từ `nghien-cuu/data/survey.json`. Mã hoá: đồng ý = đặc màu nhấn, phân vân = xám, không đồng ý = viền rỗng (không chỉ dựa vào màu).

Bài thở: chữ pha và số giây đặt dưới hình trên nền đặc; đọc pha cho trình đọc màn hình; rung nhẹ khi đổi pha (điện thoại hỗ trợ); bật "giảm chuyển động" thì không xoay góc nhìn nhưng vòng vẫn chạy.

- Khung nội dung tối đa ~1180px, một mép trái thống nhất.
- Điện thoại: khối xếp dọc, vùng bấm ≥ 44px, nút "Bắt đầu chuẩn bị" cố định đáy (ẩn ở đầu trang, ở checklist, ở cuối trang).

## 5. Không dùng

- Đỏ rực, neon chói; chữ nhấp nháy, rung.
- Emoji làm icon.
- Lời chứng, tên người, số liệu, link **tự bịa**; số liệu không nguồn.
- Câu khẳng định MockViewAI "giảm lo âu", "đã chứng minh hiệu quả" (chưa kiểm định). Giao diện mẫu phải ghi "dữ liệu mẫu / giả lập".
- Ô số to + nhãn nhỏ, mỗi số một màu, số tự đếm tăng khi cuộn tới; số thập phân ở chỗ to (để trong phần cách tính).
- Chữ gradient trong tiêu đề; tô màu riêng một cụm từ trong tiêu đề.
- Nhãn viết HOA có gạch ngang phía trên mỗi tiêu đề (eyebrow); mũi tên "→" gắn sau chữ của link và nút.
- Thẻ nghiêng 3D theo chuột, lưới sàn chạy, vòng tròn xoay, hiệu ứng trượt lên ở từng khối khi cuộn.
- Ảnh giao diện sản phẩm đặt nghiêng tới mức không đọc được chữ.
- Nút chính dùng gradient và quầng sáng: nút chính là một màu đặc (màu nhấn của theme).
- Chuyển động không tắt được: mọi hiệu ứng phải tôn trọng cài đặt "giảm chuyển động" (`prefers-reduced-motion`).
