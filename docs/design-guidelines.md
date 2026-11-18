# MockViewAI — Design Guideline

> Cập nhật 09/11/2026. Năm mục theo slide 22.
> Bản Buổi 1 (Stitch, tông navy sáng, font Lora) **đã được thay** bằng giao diện tối ngày 09/11 theo yêu cầu học viên; bản Stitch giữ trong `stitch_v_ng_v_ng_landing_page/` để đối chiếu.
> Nhãn: **[ĐÃ XÁC NHẬN]** · **[ĐỀ XUẤT]** · **[CẦN XÁC NHẬN]**.

## 1. Phong cách

- Nghiêm túc, chuyên nghiệp, **công nghệ nhưng điềm tĩnh**: nền tối, thẻ kính, ánh sáng xanh – tím nhẹ. [ĐÃ XÁC NHẬN 09/11]
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

Thứ tự khối trang chủ:
1. Thanh trên + menu (điện thoại: ba gạch)
2. Hero: lời giới thiệu, "Bắt đầu chuẩn bị", giao diện mẫu 3D (ghi "dữ liệu mẫu")
3. Số liệu khảo sát (4 ô)
4. Hành trình 3 giai đoạn (tab Trước / Trong / Sau)
5. Sản phẩm (3 thẻ, giao diện mẫu, demo)
6. Bài thở 4–6
7. Checklist (khóa khi chưa đăng nhập)
8. 5 điều người mới hay gặp
9. Bài viết (băng chuyền 4 bài) + khối nghiên cứu nhóm
10. Kêu gọi cuối trang, chân trang (có link "Khai báo sử dụng AI")

- Khung nội dung tối đa ~1180px, một mép trái thống nhất.
- Điện thoại: khối xếp dọc, vùng bấm ≥ 44px, nút "Bắt đầu chuẩn bị" cố định đáy (ẩn ở đầu trang, ở checklist, ở cuối trang).

## 5. Không dùng

- Đỏ rực, neon chói; chữ nhấp nháy, rung.
- Emoji làm icon.
- Lời chứng, tên người, số liệu, link **tự bịa**; số liệu không nguồn.
- Câu khẳng định MockViewAI "giảm lo âu", "đã chứng minh hiệu quả" (chưa kiểm định). Giao diện mẫu phải ghi "dữ liệu mẫu / giả lập".
- Chuyển động không tắt được: mọi hiệu ứng phải tôn trọng cài đặt "giảm chuyển động" (`prefers-reduced-motion`).
