# MockViewAI — Progress

> **Cập nhật 09/11/2026 (chiều):** Đã có tương tác (B3), lưu Supabase (B4), đăng nhập + tiến độ riêng từng người (B5, đã test giả lập + test RLS ở database), kiểm tra DOI qua Edge Function (B6), 4 bài viết, trang nghiên cứu nhóm, trang chủ giao diện tối, (nút đăng nhập Google đã bỏ theo quyết định nhóm).
> **Chưa có:** tài khoản thật nào trong Supabase (0 tài khoản, 0 dòng tiến độ) → chưa test A/B thật. Link Vercel: https://mockviewai.vercel.app (project `mockviewai` đã tạo 09/11, đang chờ bản deploy đầu tiên). Danh sách việc: mục NEXT bên dưới và `kiem-thu-va-demo.md`.

## 2026-11-08: Thêm trang "Nghiên cứu của nhóm" (nghien-cuu/)

- **Đã làm:** thêm thư mục `nghien-cuu/` (trang nghiên cứu Unit 16 của nhóm Ngũ Hổ Tướng, dựng theo prompt 08/11, số liệu từ `nghien-cuu/data/*.json`). Trang chủ có thêm mục menu "Nghiên cứu nhóm", khối giới thiệu (53 phản hồi · 4 chuyên gia · 18 tài liệu) và link ở chân trang. Trang nghiên cứu có nút "← MockViewAI" về trang chủ.
- **Giữ nguyên:** checklist, đăng nhập, Supabase, kiểm tra DOI, 4 bài viết, trang Tài liệu.
- **Đã kiểm tra:** trang chủ + bài viết 86/86 · trang nghiên cứu 54/54 · checklist 10/10 · không tràn ngang máy tính/điện thoại.

## 2026-11-02: Step 5 v2 — Chuẩn hóa khung bài báo + làm gọn giao diện (XONG)

- **Khung bài báo (A), cả 4 bài:** dòng tác giả + đơn vị ("Nhóm biên soạn MockViewAI · Dự án học tập, khóa Vibecode – MindX", liên hệ qua GitHub LoveKurumi1000) · Tóm tắt viết liền, **không trích dẫn** · Từ khóa (5 từ) · Mở đầu có **mục tiêu bài viết** và **khoảng trống nghiên cứu** · Phương pháp (nơi tìm nguồn, ngày tra cứu, tiêu chí, đối chiếu Crossref) · Thân bài = kết quả và thảo luận · Giới hạn · **Kết luận** (3 ý) · Tài liệu APA.
- **Làm gọn (B):** danh mục 22 nguồn chuyển sang trang riêng `tai-lieu.html` (trang chủ ngắn còn ~4.000px thay vì ~9.300px) · bỏ số liệu lặp (9,07% chỉ còn 1 lần ở trang chủ) · thẻ bài viết còn 1 câu · ô số lớn trong bài để đọc lướt · đoạn văn dài tự tách (tối đa ~520 ký tự) · hồ sơ nguồn thu gọn trong "Chi tiết nguồn" · trích dẫn ở trang chủ trỏ sang trang Tài liệu.
- **Nút kiểm tra DOI (B6)** nằm ở trang Tài liệu và cuối mỗi bài viết (17 nguồn có DOI).
- **Đã kiểm tra:** 86/86 (trang chủ, Tài liệu, 4 bài × máy tính/điện thoại: đủ khung bài báo, tóm tắt không trích dẫn, mọi [n] trỏ đúng, không tràn, không lỗi JS) · B6 20/20 (trên trang Tài liệu) · B5 38/38 + 10/10.
- **Học viên có thể đổi:** dòng tác giả sang họ tên thật nếu muốn.

## 2026-11-02: Step 5 — Trang đọc nghiên cứu, 4 bài theo vấn đề (XONG, chờ học viên đọc lại)

- **Đã làm:** Trang chủ đổi sang dạng tổng quan nghiên cứu: phần "Phát hiện chính", 4 thẻ bài viết (ảnh bìa, tóm tắt, số liệu nổi bật, nút "Đọc bài đầy đủ"), số liệu nhanh có nhãn cấp nguồn, "Cách chọn và kiểm chứng nguồn", checklist "Chuẩn bị theo bằng chứng" (5 mục mới, mỗi mục dẫn tới bài viết giải thích), danh mục 22 tài liệu theo APA. 4 trang bài viết trong `bai-viet/`: tóm tắt nhanh, mục lục, trích dẫn [n], sơ đồ / biểu đồ tự vẽ, "Giới hạn của bằng chứng", hồ sơ từng nguồn (thiết kế, mẫu, DOI, nút kiểm tra Crossref), chuyển bài trước/sau.
- **Nguồn (22):** 17 cấp A (tạp chí bình duyệt, DOI đối chiếu Crossref: tên bài, tạp chí, năm, giấy phép) · 2 cấp B (Cục Thống kê quý III/2026, ILO 2024) · 3 cấp C (JobsGO, BUV & Decision Lab, Hội Sinh viên).
- **Đã bỏ (không đạt chuẩn nguồn):** bài Orbith (tạp chí trường, nghi không thuộc chỉ mục quốc tế) · "5 lỗi hay mắc" (một bài báo 2022) · "Nỗi lo" và "Hành trình chuẩn bị" (không có nguồn) · "Miễn phí", "Khoảng 10 phút", email liên hệ tạm.
- **Ảnh:** 4 ảnh bìa Unsplash (giấy phép Unsplash, ghi tên tác giả), tải trực tiếp từ images.unsplash.com.
- **Code:** nút kiểm tra DOI tách ra `assets/doi-check.js` dùng chung (đã xóa khỏi `app.js`); giao diện bài viết `assets/bai-viet.css` (không phụ thuộc Tailwind).
- **Đã kiểm tra:** 84/84 (trang chủ + 4 bài × máy tính / điện thoại: không tràn ngang, không lỗi JS, mọi [n] trỏ đúng tài liệu, mọi tài liệu đều được trích dẫn, link nội bộ tồn tại, ảnh có alt, link ngoài mở tab mới, đủ nút DOI) · chạy lại B5 38/38 + 10/10, B6 22/22.
- **Lỗi đã sửa:** ảnh bìa bị cao quá trên máy tính; bảng 4 cột ở Bài 4 quá hẹp trên điện thoại → chuyển thành thẻ xếp dọc.
- **Còn lại:** học viên đọc lại 4 bài; điền dòng "Biên soạn" khi muốn; email liên hệ (đã tạm bỏ khỏi chân trang).

## 2026-11-02: Buổi 6 — Step 4 "Kiểm tra DOI qua Crossref" (XONG, chờ học viên chốt + test online)

- **Đã làm:** Edge Function `doi-check` (đã deploy, ACTIVE): nhận DOI → hỏi Crossref → trả tên bài, tạp chí, năm, tác giả. Không secret, không ghi database, không yêu cầu đăng nhập (`verify_jwt` tắt vì đây là chức năng công khai, chỉ đọc). Mỗi thẻ nghiên cứu có nút "Kiểm tra DOI trên Crossref".
- **Test riêng function (gọi thật, 10/10):** 5 DOI trong trang đều **có thật** và khớp tác giả / tạp chí / năm ghi trên thẻ · dán cả link `https://doi.org/...` vẫn nhận · DOI sai dạng → 400 + lỗi tiếng Việt · DOI không tồn tại → "không tìm thấy" · gọi GET → 405 · CORS cho phép trang web gọi.
- **Test giao diện (22/22, máy tính + điện thoại):** mở trang không tự gọi Crossref · bấm → "Đang kiểm tra…", khóa nút, bấm liên tục không gọi trùng · chỉ gửi DOI + publishable key, không gửi phiên đăng nhập · kết quả hiện dạng chữ (không chèn HTML lạ) · không tìm thấy / Crossref lỗi / mất mạng → báo lỗi tiếng Việt, bấm lại được · checklist vẫn chạy bình thường · không tràn ngang, không lỗi JS.
- **Nội dung đã sửa theo quyết định 16/10:** tên MockViewAI · bỏ 3 "câu chuyện" tự đặt tên → 5 nghiên cứu có DOI · thêm "Số liệu Việt Nam gần đây" (4 báo cáo, ghi rõ **không có DOI** + nguồn) · thêm "5 lỗi người mới hay mắc" (nguồn báo Tiền Phong) · bỏ "Không cần đăng ký" · bỏ email/Facebook giả ở chân trang. Test: 14/14 (không tràn, mọi link có đích, link ngoài mở tab mới); test B5 chạy lại vẫn 38/38 + 10/10.
- **An toàn trước khi online:** quét thư mục không có khóa bí mật · thêm `.gitignore` (chặn `.env`, tài liệu khóa học).

## 2026-11-02: Buổi 5 · Nhịp 2 — Dữ liệu riêng từng người (XONG PHẦN CODE + DATABASE)

- **Đã làm (database, migration `checklist_progress_per_user`):** xóa 4 dòng test Buổi 4 (học viên đã duyệt) · thêm cột `user_id` (bắt buộc, mặc định `auth.uid()`, nối `auth.users`, xóa tài khoản thì xóa theo) · khóa chính đổi thành (`user_id`, `item_key`) · bỏ quyền `anon` · 3 luật RLS cho `authenticated`: chỉ xem / thêm / sửa dòng có `user_id` = chính mình · vẫn không cho xóa.
- **Đã làm (app.js):** đọc `select(...).eq('user_id', id)`; lưu `upsert` kèm `user_id`, `onConflict: 'user_id,item_key'`; lưu lỗi → trả ô tích về như cũ + "Chưa lưu được, thử lại."; chỉ báo "Đã lưu." khi Supabase trả về thành công.
- **Đã kiểm tra ở database (giả lập 2 người A/B, chạy xong tự hoàn tác, bảng vẫn 0 dòng):** A thấy 2 dòng của A · B thấy 0 dòng của A · B sửa dòng của A → 0 dòng bị sửa · B ghi dòng mang `user_id` của A → bị RLS chặn · B xóa → bị chặn · B tự thêm dòng của mình → `user_id` tự điền đúng · khách chưa đăng nhập → bị chặn. Security / Performance Advisor: không cảnh báo.
- **Đã kiểm tra trên trang (Supabase giả lập có RLS, 10/10 PASS):** A tích 2 mục → "Đã lưu.", dòng ghi kèm `user_id` của A · A đăng xuất → ô tích xóa khỏi màn hình · B đăng nhập → 0/5 · B tích 1 mục · F5 vẫn là B · lỗi mạng → ô tích trả về · quay lại A → đúng 2 mục của A · không lỗi JS.
- **Lỗi đã sửa:** bộ giả lập Supabase dùng để test thiếu `.eq()` → bổ sung (lỗi của công cụ test, không phải của app).
- **Còn lại (học viên tự làm, mật khẩu không gửi vào chat AI):** bật Confirm Email trong Dashboard; tạo A/B (Auto Confirm); làm 6 bước test thật: A tích → đăng xuất → B không thấy → B tích → F5 → quay lại A vẫn đúng.

## 2026-11-02: Buổi 5 · Nhịp 1 — Danh tính (ĐANG CHỜ TEST THẬT)

- **Đã làm:** Header có nút "Đăng nhập" → sau khi đăng nhập hiện email + "Đăng xuất". Hộp đăng nhập / đăng ký (một hộp, chuyển qua lại), lỗi bằng tiếng Việt. Checklist khóa khi chưa đăng nhập ("Đăng nhập để lưu tiến độ chuẩn bị của riêng bạn"), có dòng "Đang lưu cho: email". "Bắt đầu chuẩn bị" khi chưa đăng nhập → mở hộp đăng nhập. Giữ phiên khi F5. **Chưa đổi database.**
- **Đã kiểm tra (Supabase giả lập, 38/38 PASS, máy tính + điện thoại):** để trống email · sai định dạng · mật khẩu < 6 · sai mật khẩu · email chưa xác nhận · đăng ký email đã có · đăng ký mới → "Đã gửi email xác nhận" · đăng nhập A thấy email A · đăng xuất → khóa lại · đăng nhập B thấy email B · F5 vẫn là B · header 1 dòng, không tràn ngang, không lỗi JS.
- **Lỗi đã sửa:** Header bị chật (menu xuống 2 dòng) sau khi thêm nút tài khoản → không cho xuống dòng, email chỉ hiện trên header ở màn rộng, thêm "Đang lưu cho" trong thẻ checklist.
- **Còn lại:** Học viên tạo tài khoản A/B (Auto Confirm) và tự đăng nhập thử trên trình duyệt thật (gộp chung với test Nhịp 2).

## 2026-10-26: Buổi 4 — Hoàn thành Step 2 "Lưu tiến độ checklist vào Supabase"

- **Đã làm:** Tạo Supabase project `mockviewai` (Singapore, gói Free). Migration `create_checklist_progress`: bảng `checklist_progress` (1 dòng = 1 mục checklist; cột `item_key`, `is_done`, `updated_at`), RLS bật, trang web chỉ được đọc/thêm/sửa — không được xóa. `app.js` đọc tiến độ khi mở trang và lưu ngay khi tích/bỏ tích; chỉ báo "Đã lưu." sau khi Supabase xác nhận. `index.html` thêm thư viện supabase-js và dòng trạng thái lưu.
- **Đã kiểm tra (trên trình duyệt thật, mạng thật):**
  - Ghi nhận: tích 3 mục → Supabase có đúng 3 dòng mới, mỗi lần hiện "Đang lưu…" rồi "Đã lưu." [OK]
  - Tra cứu: sửa trực tiếp 1 dòng (`truoc-05`) trong Supabase → tải lại → web hiện đúng [OK]
  - Theo dõi / F5: tải lại trang → tiến độ còn nguyên 4/5 [OK]
  - Lỗi: database từ chối dữ liệu sai luật → "Chưa lưu được, thử lại.", ô tích trả về trạng thái cũ, không có chữ "Đã lưu" [OK]
  - Bảo mật: vai trò trình duyệt (anon) không có quyền xóa; Supabase Security Advisor: 0 cảnh báo [OK]
  - Sau test đã đặt lại mọi mục về "chưa tích" (không xóa dòng).
- **Còn lại:** Mọi người mở trang đang **dùng chung một tiến độ** (chưa có đăng nhập) · nội dung checklist vẫn là minh họa · tên "Vững Vàng" và dòng "Không cần đăng ký" ở Hero chưa sửa · nên tự mở `index.html` trên máy tính và điện thoại thật một lần.
- **Tiếp theo:** Buổi 5 — đăng ký/đăng nhập, đổi luật RLS để mỗi người chỉ thấy tiến độ của mình.

## 2026-10-26: Buổi 3 — Hoàn thành Step 1 "Tích checklist"

- **Đã làm:** Tạo `index.html` (bản sao Stitch desktop, file Stitch gốc giữ nguyên) + `app.js`. Checklist 5 mục tích được thật; số "x / 5", % và thanh tiến độ cập nhật ngay; nút "Bắt đầu chuẩn bị" cuộn tới checklist và đặt focus vào mục đầu tiên chưa tích; 5/5 hiện "Bạn đã sẵn sàng hơn rồi!"; có dòng "Nội dung minh họa · Chưa lưu".
- **Đã kiểm tra (24/24 PASS, máy tính 1440px + điện thoại 390px):** mở trang 0/5 · tích 2 → 40% · bỏ tích → 20% · bấm nhanh 11 lần không lệch số · 5/5 hiện lời động viên · bỏ 1 mục → 80% và ẩn lời động viên · tích bằng bàn phím (Space) · F5 về 0/5 · không tràn ngang · không lỗi JavaScript · không có chữ "Đã lưu".
- **Lỗi đã sửa:** Lời động viên hiện sai ở 2/5 trên điện thoại (class `flex` của Tailwind đè thuộc tính `hidden`) → đổi sang bật/tắt bằng class; test lại đúng lỗi đó: đạt.
- **Còn lại:** Chưa lưu dữ liệu (F5 mất tiến độ) · nội dung checklist vẫn là minh họa · tên trên trang vẫn là "Vững Vàng" · dòng "Không cần đăng ký" ở Hero mâu thuẫn quyết định "cần tài khoản" · nên tự mở `index.html` trên điện thoại thật một lần (môi trường test chặn font icon).
- **Tiếp theo:** Buổi 4 — chốt bảng dữ liệu, lưu tiến độ vào Supabase, F5 vẫn còn.

## DONE
- Buổi 1: 5 dòng định hướng; bản Stitch máy tính + điện thoại.
- Buổi 2: 4 file `docs/` (cập nhật lại 09/11 cho khớp sản phẩm hiện tại).
- Buổi 3: tích checklist (24/24). Buổi 4: lưu Supabase (test thật 26/10). Buổi 5: đăng nhập + RLS theo người dùng (code + database; test giả lập 38/38 + 10/10).
- Buổi 6: Edge Function `doi-check` (ACTIVE) + nút kiểm tra DOI ở bài viết và trang tài liệu.
- Nội dung: 4 bài viết có trích dẫn; trang nghiên cứu nhóm (53 phản hồi, 4 chuyên gia, 18 tài liệu, khai báo sử dụng AI).
- 09/11: trang chủ giao diện tối 6 đợt; README, hướng dẫn online, checklist kiểm thử + kịch bản demo; lưu bản sao 2 migration vào `supabase/migrations/`; ghi cố định phiên bản supabase-js 2.45.4; sửa menu gạch chân sai mục khi cuộn.
- 09/11: kiểm tra toàn bộ 24 trang: không link hỏng, không lỗi JS, không tràn ngang (1440px, 390px); số liệu trang chủ khớp `survey.json`.

- 09/11 (16h): kiểm tra đăng nhập Google — máy chủ Supabase báo `google: false` (provider chưa bật). `app.js` thêm bước hỏi máy chủ trước khi chuyển sang Google: chưa bật thì báo lỗi tiếng Việt thay vì trang lỗi JSON.

- 09/11 (16h05): **bỏ đăng nhập Google** theo quyết định học viên (trang chủ yếu để đọc). Giữ đăng nhập email + checklist Supabase (tiêu chí Buổi 4–5). Bản trước khi bỏ: ~/mvwork/truoc-bo-google (máy ảo).

## DOING
- Chuẩn bị Demo Day (Buổi 7): đưa online, test A/B thật.

## BLOCKED (cần học viên tự làm — có mật khẩu / mã bí mật)
- Tạo tài khoản test A/B (Auto Confirm) và chạy test thật.
- Đưa lên GitHub + Vercel bằng tài khoản của học viên.

## DECISIONS
- 12/10: Đối tượng SV sắp/mới ra trường + đi làm 1–2 năm; vấn đề lo âu phỏng vấn; hành động "Bắt đầu chuẩn bị"; phong cách nghiêm túc; có máy tính + điện thoại.
- 16/10: Chốt câu MVP. Đổi tên → MockViewAI; câu chuyện → nghiên cứu có DOI; số liệu → ưu tiên Việt Nam gần nhất. Cần tài khoản.
- 26/10: Buổi 3 chọn tương tác "Tích checklist". Buổi 4 duyệt project `mockviewai` + bảng `checklist_progress`. Claude đảm nhận cả vai Mentor và vai xây.
- 02/11: Buổi 5 chốt đăng nhập email + mật khẩu, Confirm Email ON, tiến độ riêng từng người. Buổi 6 [ĐỀ XUẤT] tích hợp Crossref.
- 08/11: Thêm trang nghiên cứu nhóm vào thư mục `nghien-cuu/` (học viên chọn "trang riêng").
- 09/11: Thư viện tài liệu chỉ hiện 4 bài (Zielinska, Gomez, Wilkie + Santos); thêm sơ đồ trong trang tài liệu; thêm khai báo sử dụng AI theo quy chế MindX; giao diện tối cho trang chủ. Chiều 09/11: bỏ đăng nhập Google, chỉ giữ email.

## NEXT
1. [Học viên] Vercel: project `mockviewai` → https://mockviewai.vercel.app. Kiểm tra trang đã deploy, rồi cập nhật Site URL = https://mockviewai.vercel.app và Redirect URLs = https://mockviewai.vercel.app/** trên Supabase.
2. [Học viên] Tạo A/B, chạy `kiem-thu-va-demo.md` trên link Vercel, chụp màn hình bằng chứng.
3. [Nhóm] Xác nhận: nội dung 5 mục checklist · bảng khai báo AI · câu MVP (xây thêm tự đánh giá hay sửa câu, xem PRD mục 6) · Step 4 Crossref là bài Buổi 6.
4. [Nhóm] Hỏi giảng viên về giới hạn 20% nội dung AI (quy chế IV.2.5) với môn Vibecode.
5. [Nhóm] Tập kịch bản demo 5 phút.

## 08/11/2026 – Trang nghiên cứu v2 (giao diện kiểu motionsites)
- Viết lại nghien-cuu/: nền tối, hero chữ lớn, một mép trái thống nhất (khung 1160px).
- Thư viện 18 tài liệu dạng thẻ bìa + nút lọc (Tổng quan / Lý thuyết / Phương pháp / Bối cảnh).
- Mỗi tài liệu có trang riêng nghien-cuu/tai-lieu/<id>.html: tóm tắt, mẫu, phát hiện, giới hạn, nhóm dùng thế nào, giả thuyết liên quan, trích dẫn Harvard, nút "Đọc bài gốc" (DOI, tab mới), kiểm tra DOI Crossref, sao chép trích dẫn.
- Kết quả gom thành 6 tab; đoạn dài để trong "Đọc thêm".
- Sửa JSON xong chạy: node nghien-cuu/build-data.js
- Bản cũ sao lưu ở ~/mvwork/truoc-rs2 (máy ảo).

## 09/11/2026 – Trang nghiên cứu v3 (tham khảo Raycast + Vortex)
- Thêm ô Tìm nhanh kiểu Raycast: hiện ở hero, mở mọi lúc bằng Ctrl K (⌘ K trên Mac) hoặc phím /. Tìm trong mục, 18 tài liệu, H1–H4, S1–S12; gõ không dấu cũng tìm được.
- Màu theo Vortex (slate, xanh – tím – cyan), tiêu đề Space Grotesk, nút bo góc, thẻ kính có viền sáng.
- Tab kết quả và bộ lọc tài liệu lưu trên link (?tab=e&loai=method) để gửi link đúng chỗ.
- Sửa theo web-design-guidelines: color-scheme dark, text-wrap balance, touch-action, translate="no" cho tên MockViewAI, dấu ngoặc kép cong, link bỏ qua ở trang chi tiết.
- File mới: nghien-cuu/palette.js. Bản v2 sao lưu ở ~/mvwork/truoc-rs3 (máy ảo).

## 09/11/2026 – Thư viện gọn lại + sơ đồ trong bài
- Thư viện chỉ hiện 4 bài: 3 bài nhóm chọn (Zielinska 2021, Gomez 2025, Wilkie 2024) + 1 bài gợi ý thêm (Santos 2021). 14 tài liệu còn lại nằm trong "Tìm hiểu thêm" (dạng danh sách, có bộ lọc). Danh sách bài nổi bật đặt ở đầu build-data.js (PICK, FEATURED).
- Mỗi trang chi tiết có sơ đồ tự vẽ từ số liệu trong references.json: sơ đồ nghiên cứu (ai → làm gì → phát hiện); thêm phễu số người + ô p-value (Zielinska), 5 phần MASI (Santos, McCarthy), khen/vấn đề (Gomez), chuẩn bị → luyện → cảm nhận (Wilkie), 3 mặt lo lắng (Constantin), thang 5 mức (Likert).
- Giãn dòng 1,8; các danh sách có đường kẻ ngăn từng dòng.
- Bản trước sao lưu ở ~/mvwork/truoc-rs4 (máy ảo).

## 09/11/2026 – Khai báo AI theo quy chế MindX + thêm hình
- Đối chiếu quy chế [MINDX_K18] Quy chế làm bài kiểm tra – đánh giá: thiếu khai báo AI (IV.2.1–2.2).
- Thêm mục "Khai báo sử dụng AI" (nghien-cuu/index.html#su-dung-ai): bảng phần việc – công cụ – AI hỗ trợ – nhóm tự làm/kiểm tra, kèm hạn chế của AI. Link ở chân trang nghiên cứu, 18 trang tài liệu, 4 bài viết.
- Câu ghi chú trang tài liệu sửa thành: nhóm tóm tắt, sơ đồ do AI vẽ từ số liệu nhóm, nhóm đã kiểm lại.
- CẦN NHÓM XÁC NHẬN: nội dung bảng AI (đặc biệt "Thu dữ liệu" và "Phân tích, kết luận" ghi là không dùng AI). Hỏi giảng viên về giới hạn 20% AI (IV.2.5) với môn Vibe Code.
- Thêm hình: biểu tượng cho 3 giai đoạn, 4 thẻ "Vì sao", các thẻ phương pháp/giới hạn; ảnh thu nhỏ bấm phóng to cho sơ đồ ASM1 (hinh1, hinh2), ảnh gốc Google Forms; thêm ảnh câu hỏi nối tiếp của MockViewAI.
- (Đã giải quyết chiều 09/11: học viên chép lại bản đầy đủ có index.html; tai-lieu.html được thay bằng nghien-cuu/index.html#tham-khao.)

## 09/11/2026 – Trang chủ dark tech 3D + Google + bài viết tối + ảnh nghiên cứu
- Trang chủ `index.html` dựng lại (theme tối, 3D), CSS/JS tách theo 6 đợt trong `assets/home/` (xem `docs/cac-dot-giao-dien.md`). (Ghi chú 09/11 chiều: không có bản một file riêng; `index.html` đã đủ 6 đợt.)
- Đăng nhập Google: nút "Tiếp tục với Google" trong hộp đăng nhập, `app.js` gọi `signInWithOAuth`. CẦN LÀM: bật Google trong Supabase (Authentication → Providers) + tạo OAuth Client trên Google Cloud; thêm địa chỉ trang (Vercel / Live Server) vào Redirect URLs. Mở bằng nhấp đúp file thì Google không chạy được (trang sẽ báo).
- Bài viết: trang chủ có băng chuyền lướt ngang (mũi tên, đếm, kéo chuột, vuốt). 4 trang bài viết đổi sang giao diện tối (khối cuối `assets/bai-viet.css`, xoá khối đó là về bản sáng); link "Tài liệu" hỏng đã trỏ sang `nghien-cuu/index.html#tham-khao`.
- Nghiên cứu: ảnh thu nhỏ hiện đủ ảnh; khung xem ảnh mới (chuyển ảnh ← →, dải ảnh nhỏ, phóng to/kéo, mở ảnh gốc, vuốt trên điện thoại); 12 biểu đồ S1–S12 xem liên tiếp; ảnh sản phẩm ở mục Công cụ bấm để xem lớn.
- Checklist mới dùng mã `truoc-01` … `truoc-05` — nhóm cần duyệt nội dung 5 mục.
