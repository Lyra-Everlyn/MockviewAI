# MockViewAI — Development Plan

> Bản đồ, không phải đường ray (slide 24). Thứ tự theo slide 25. Bước nào xong chỉ khi đã kiểm thực tế.
> Cập nhật 09/11/2026: Step 6 (đăng nhập Google) đã bỏ theo quyết định nhóm; Step 7 (online + Demo Day). Trạng thái từng bước xem `progress.md`.

| Bước | Việc làm | Phụ thuộc | Kiểm tra xong bằng cách nào? |
|---|---|---|---|
| **0. Chốt hồ sơ — Buổi 2** | Đọc, sửa, lưu 4 file `docs/`. Chưa code. | 5 dòng định hướng + bản Stitch | 4 file thống nhất, không chỗ nào AI tự bịa |
| **1. Thêm tương tác** | Nút "Bắt đầu chuẩn bị" mở checklist; tích mục → thanh tiến độ cập nhật; vòng thở chạy; menu điện thoại mở/đóng. Chưa lưu. | Bước 0; checklist dùng bản Minh họa nếu chưa duyệt | Bấm nút thấy checklist; tích/bỏ → % đúng; thử trên điện thoại; không hiện "Đã lưu" |
| **2. Lưu dữ liệu** | Lưu tiến độ checklist và điểm tự đánh giá | Bước 1; [CẦN XÁC NHẬN] nơi lưu, thang đo | Tải lại trang vẫn còn; lưu lỗi không báo thành công |
| **3. Đăng nhập** | Đăng ký/đăng nhập để mỗi người có tiến độ riêng | [ĐÃ XÁC NHẬN] cần tài khoản; Bước 2 | Đăng xuất → đăng nhập lại thấy đúng tiến độ; tài khoản khác không xem được |
| **4. Đưa lên internet** | GitHub (LoveKurumi1000) + Vercel, trước Demo Day | [ĐÃ XÁC NHẬN] nơi; Bước 3 | Mở từ thiết bị khác, đi hết luồng, test A/B trên link thật |
| **5. Dịch vụ ngoài** | Kiểm tra DOI qua Crossref (Edge Function `doi-check`) — Step 4 | Bước 4 để test online | Bấm nút trên link Vercel ra đúng tên bài, tạp chí, năm |

> Đăng nhập nằm trong MVP vì học viên đã chốt, nhưng làm **sau** tương tác và lưu để mỗi bước kiểm được riêng.

**Chưa cần:** phỏng vấn thử AI, quiz, storytelling, biểu đồ cảm xúc, thanh toán, admin, app riêng.

**Khi kế hoạch đổi (s28):** hỏi lại học viên → sửa PRD / Design Guideline → sửa plan → ghi vào progress.

---

## Step 1: Tích checklist chuẩn bị phỏng vấn — Buổi 3 (đã chốt 26/10)

- **Mục tiêu người dùng:** Đánh dấu việc chuẩn bị đã làm xong và biết mình còn thiếu gì.
- **Kiểu tương tác (s7):** Nút bật/tắt (ô tích).
- **Bộ lọc 4/4 (s8):** Có ích cho MVP ✓ · Vừa sức ✓ · Phản hồi nhìn thấy ✓ · Kiểm tra được ngay ✓
- **Trigger:** Bấm "Bắt đầu chuẩn bị" (header, Hero, khối cuối trang) → cuộn tới khối checklist, con trỏ (focus) đặt vào mục đầu tiên chưa tích.
- **Input:** Bấm vào ô tích của từng mục (5 mục, nội dung **MINH HỌA** — học viên sẽ duyệt nội dung thật sau).
- **Phản hồi:** Mục đã tích có gạch ngang, mờ đi; chữ "x / 5 hoàn thành", "% tiến độ" và thanh tiến độ cập nhật ngay.
- **Validation / trường hợp sai:**
  - Bấm nhanh nhiều lần → số và % luôn khớp số mục đang tích, không vượt 100%, không âm.
  - Bỏ tích → % giảm đúng.
  - Tải lại trang (F5) → về 0/5. Đây là đúng ở bước này vì **chưa lưu**; trên thẻ có dòng ghi rõ "Chưa lưu".
- **State Done:** Tích đủ 5/5 → hiện lời động viên "Bạn đã sẵn sàng hơn rồi!". Bỏ tích một mục → lời động viên ẩn đi.
- **Acceptance:**
  - Trang mở lên ở 0/5 (không còn số cứng 3/5).
  - Tích 2/5 hiện 40%; bỏ 1 mục còn 20%; 5/5 hiện 100% + lời động viên.
  - Không reload trang khi tích; dùng được bằng bàn phím (Tab + Space).
  - Chạy đúng ở máy tính (1440px) và điện thoại (390px), không tràn ngang.
  - Không có chữ "Đã lưu" ở bất kỳ đâu.
- **Giữ nguyên (s18):** Bố cục, màu, nội dung các khối khác của bản Stitch; file Stitch gốc không sửa — làm trên bản sao `index.html`.
- **File:** `index.html` (bản sao từ Stitch desktop) + `app.js`.
- **Chưa làm ở bước này:** lưu dữ liệu (Buổi 4), đăng nhập (Buổi 5), tự đánh giá mức lo, đổi tên sang MockViewAI, nội dung checklist thật.

---

## Step 2: Lưu tiến độ checklist vào Supabase — Buổi 4 (học viên duyệt 26/10)

- **Hành động tạo dữ liệu:** Tích / bỏ tích một mục checklist (Step 1).
- **Nhóm nhu cầu (s5):** Theo dõi — tiến độ chuẩn bị.
- **Supabase project:** `mockviewai` · tổ chức KurumiTokisaki · gói Free · máy chủ Singapore (ap-southeast-1).
- **Table:** `checklist_progress`
- **Row (1 dòng = ?):** Trạng thái của **1 mục checklist**.
- **Columns:**
  - `item_key` — mã mục (vd. `truoc-01`), không trùng (khóa chính).
  - `is_done` — đã tích chưa (đúng/sai).
  - `updated_at` — thời điểm cập nhật gần nhất.
- **Đọc / ghi ở đâu:** `app.js` — mở trang → đọc toàn bộ bảng và tích lại đúng các mục; tích/bỏ tích → ghi (upsert) đúng 1 dòng của mục đó.
- **Security:** RLS **BẬT**, luật đơn giản cho trang web (vai trò `anon`): được **đọc, thêm, sửa**; **không được xóa**. Chỉ dùng khóa an toàn cho trình duyệt (publishable key).
- **Phản hồi khi lưu (s20):** "Đang lưu…" → chỉ hiện "Đã lưu" khi Supabase xác nhận. Lỗi → trả ô tích về trạng thái cũ + "Chưa lưu được, thử lại".
- **Acceptance Criteria:**
  - Tích trên web → Supabase có đúng dòng mới / dòng được cập nhật.
  - F5 tải lại trang → tiến độ vẫn còn nguyên.
  - Ghi 3 lần với 3 mục khác nhau → 3 dòng trong bảng.
  - Sửa 1 dòng trong Supabase → F5 → web hiển thị đúng dữ liệu mới.
  - Lưu lỗi (mất mạng) → không báo "Đã lưu", ô tích trở về trạng thái cũ.
- **Giới hạn đã biết:** Chưa có đăng nhập nên **mọi người dùng chung một tiến độ**. Buổi 5 sẽ thêm tài khoản để mỗi người chỉ thấy dữ liệu của mình.
- **Không thêm:** cột người dùng, cột ghi chú hay bảng khác (s8, s25).

---

## Step 3: Tài khoản & tiến độ riêng từng người — Buổi 5 (học viên chốt 02/11)

- **Target Users:** Sinh viên / người đi làm 0–2 năm muốn làm checklist chuẩn bị phỏng vấn.
- **Public Scope (không cần đăng nhập):** Toàn bộ nội dung trang chủ — giới thiệu, nỗi lo, lộ trình, bài thở, 5 lỗi, câu chuyện.
- **Private Scope:** Tiến độ checklist của từng người.
- **Auth Flow:** Email + mật khẩu (tối thiểu 6 ký tự). **Confirm Email = ON.** Đăng ký xong → "Kiểm tra email để xác nhận" → bấm link → đăng nhập.
- **Màn hình:**
  - Header: chưa đăng nhập → nút "Đăng nhập"; đã đăng nhập → email + nút "Đăng xuất".
  - Hộp đăng nhập / đăng ký (một hộp, chuyển qua lại), báo lỗi bằng tiếng Việt chỉ rõ cách sửa.
  - Checklist khi chưa đăng nhập: khóa ô tích, hiện "Đăng nhập để lưu tiến độ chuẩn bị của riêng bạn" + nút đăng nhập. "Bắt đầu chuẩn bị" khi chưa đăng nhập → mở hộp đăng nhập.
- **Ownership Column:** `checklist_progress.user_id` (liên kết `auth.users`). Khóa chính đổi thành (`user_id`, `item_key`) → mỗi người có tối đa 5 dòng riêng.
- **RLS Policy:** `auth.uid() = user_id` cho đọc / thêm / sửa; **không xóa**; bỏ toàn bộ quyền của khách chưa đăng nhập (`anon`).
- **Dữ liệu cũ:** Xóa 4 dòng test của Buổi 4 trước khi đổi cấu trúc (dữ liệu thử, không phải dữ liệu thật).
- **Làm theo 2 nhịp (s17):**
  - **Nhịp 1 · Danh tính:** form đăng ký/đăng nhập, nút đăng xuất, giữ phiên, hiện email. **Chưa đổi database.**
  - **Nhịp 2 · Quyền dữ liệu:** thêm `user_id`, đổi RLS, app ghi kèm `user_id`.
- **Test Criteria:**
  - Nhịp 1: đăng nhập A thấy email A · đăng xuất → đăng nhập B thấy email B · F5 vẫn còn phiên.
  - Nhịp 2: A tích 2 mục → đăng xuất → B thấy 0/5 → B tích 1 mục → quay lại A vẫn đúng 2 mục của A · tài khoản chưa đăng nhập không đọc được gì.
  - Online: chạy lại toàn bộ test A/B trên URL công khai (cửa sổ ẩn danh / điện thoại).
- **Tài khoản test A/B:** Học viên tự tạo trên Supabase Dashboard (Auto Confirm). Mật khẩu không gửi vào chat AI.
- **Chuẩn bị trước khi online:** đổi tên hiển thị "Vững Vàng" → **MockViewAI**; bỏ dòng "Không cần đăng ký" ở Hero (mâu thuẫn quyết định cần tài khoản).

---

## Step 4: Kiểm tra DOI qua Crossref — Buổi 6 (đã xây 02/11; [CẦN XÁC NHẬN] học viên chốt dùng làm bài Buổi 6)

- **Vì sao chọn dịch vụ này:** Chủ đề là đọc nghiên cứu có nguồn rõ ràng. Crossref là nơi đăng ký DOI chính thức → bấm 1 nút là biết DOI có thật không, đúng tên bài, tạp chí, năm. Không chọn Telegram / AI vì không giúp người đọc kiểm chứng nguồn (slide 28: không thêm API cho có).
- **Luồng:** Trình duyệt → Edge Function `doi-check` → Crossref API (`api.crossref.org/works/{DOI}`) → trả kết quả gọn về trang.
- **Trigger:** Nút "Kiểm tra DOI" trên từng thẻ nghiên cứu (mục "Nghiên cứu khoa học nói gì?"). Ai cũng bấm được, không cần đăng nhập (nội dung công khai).
- **Dữ liệu gửi đi:** Chỉ chuỗi DOI. Không gửi email, tên hay tiến độ của người dùng.
- **Dữ liệu trả về:** tìm thấy hay không · tên bài · tạp chí · năm · 3 tác giả đầu · nhà xuất bản.
- **Secret:** Không cần (Crossref miễn phí, không cần khóa). Không có khóa bí mật nào trong frontend hay GitHub. Tùy chọn: biến `CROSSREF_MAILTO` (email liên hệ, không bí mật) đặt trong Edge Function Secrets.
- **Bảo vệ function:** Chỉ nhận POST · DOI phải đúng dạng `10.xxxx/...`, dài ≤ 200 ký tự · chờ tối đa 8 giây · không ghi gì vào database.
- **Báo lỗi trên trang (không làm hỏng trang):**
  - DOI sai dạng → "DOI không đúng định dạng."
  - Crossref không có → "Không tìm thấy DOI này trên Crossref."
  - Mất mạng / Crossref chậm → "Chưa kiểm tra được, thử lại sau." + nút vẫn bấm lại được.
- **Test Criteria:**
  1. Test riêng function (chưa nối giao diện): 5 DOI trong trang → tìm thấy, đúng tên tạp chí · DOI sai dạng → lỗi 400 · DOI không tồn tại → "không tìm thấy".
  2. Nối giao diện: bấm nút → "Đang kiểm tra…" → hiện kết quả · bấm lại không bị nhân đôi · lỗi mạng → báo lỗi, trang vẫn dùng bình thường.
  3. Online: bấm thử trên link Vercel.
- **Không thêm:** lưu kết quả vào database, kiểm tra hàng loạt, tự động kiểm tra khi mở trang (tránh gọi Crossref thừa).

---

## Step 5: Trang đọc nghiên cứu — 4 bài theo vấn đề (học viên chốt 02/11)

- **Mục tiêu:** Người đọc hiểu vấn đề hiện nay của sinh viên khi tìm việc / phỏng vấn qua số liệu và nghiên cứu có nguồn uy tín. Trình bày như trang nghiên cứu, **không có quiz / bài test**.
- **Trang chủ (mặt ngoài):** ngắn gọn. Mỗi vấn đề = 1 thẻ: ảnh bìa, tiêu đề, 2–3 câu tóm tắt, 1 con số nổi bật + nguồn, nút "Đọc bài đầy đủ". Giữ: số liệu nhanh, checklist (B3–B5), danh mục tài liệu tham khảo.
- **4 bài viết (mỗi bài 1 trang riêng `bai-viet/…html`):**
  1. Sinh viên Việt Nam đang gặp áp lực gì khi tìm việc?
  2. Vì sao phỏng vấn khiến người mới lo âu?
  3. Lo âu có thật sự làm phỏng vấn kém đi?
  4. Điều gì giúp giảm lo âu — theo bằng chứng?
- **Khung mỗi bài:** ảnh bìa · tiêu đề · ngày cập nhật · mục lục · nội dung có trích dẫn [n] · sơ đồ thực tế · "Giới hạn của bằng chứng" · hồ sơ nguồn (DOI, nút kiểm tra Crossref, link gốc). Dòng "Biên soạn" để sau.
- **Cấp nguồn (gắn nhãn trên trang):** A = tạp chí bình duyệt, có DOI · B = tổ chức quốc tế / cơ quan thống kê nhà nước · C = báo cáo doanh nghiệp, báo chí Việt Nam.
- **Quy tắc nội dung:** mọi nhận định có trích dẫn; phân biệt "liên quan" và "nguyên nhân"; ghi cỡ mẫu + nước; nói rõ khoảng trống nghiên cứu ở Việt Nam; viết bằng lời mình, không chép abstract; không đăng lại PDF — chỉ link bài gốc; có dòng "không thay thế tư vấn tâm lý".
- **Ảnh:** 1 ảnh bìa / bài (giấy phép miễn phí, ghi nguồn) + sơ đồ tự vẽ mô tả tình huống thật / biểu đồ từ số liệu nguồn. Không dùng ảnh AI.
- **Không làm (lúc chốt 02/11):** đăng nhập Google (sau đó làm ở Step 6), quiz, tự đánh giá.
- **Test Criteria:** mọi [n] trỏ đúng nguồn trong danh mục · mọi DOI xác minh được trên Crossref · không tràn ngang máy tính / điện thoại · không lỗi JS · checklist + kiểm tra DOI vẫn chạy.

---

## Step 6: Đăng nhập bằng Google — ĐÃ BỎ (09/11 chiều)

> Học viên quyết định bỏ: trang chủ yếu để đọc; đăng nhập email + checklist đủ cho tiêu chí Buổi 4–5. Đã xóa nút và code khỏi `index.html`, `app.js`, `assets/home/6-cuoi-trang.css`. Phần dưới giữ lại để tham khảo nếu muốn làm lại.

- **Mục tiêu:** đăng nhập nhanh, không cần chờ email xác nhận.
- **Cách làm:** Supabase OAuth, provider Google. Nút "Tiếp tục với Google" trong hộp đăng nhập; `app.js` gọi `signInWithOAuth({ provider: 'google', options: { redirectTo: trang hiện tại } })`. Đăng nhập email + mật khẩu giữ nguyên, Confirm Email vẫn bật.
- **Dữ liệu:** dùng chung bảng `checklist_progress` và luật RLS `auth.uid() = user_id`; không thêm bảng, không đổi database.
- **Secret:** Client Secret chỉ nằm trong Supabase (Authentication → Providers → Google). Không có trong code, không gửi vào chat AI.
- **Cấu hình (học viên tự làm):** xem `huong-dan-online.md` Phần A.
- **Báo lỗi:** mở bằng nhấp đúp file → "Đăng nhập Google cần mở trang qua địa chỉ web…"; provider chưa bật → "Đăng nhập Google chưa được bật trên máy chủ. Bạn dùng email trước nhé."
- **Test Criteria:** đăng nhập Google → quay về đúng trang, hiện email · tích 1 mục, F5 còn · đăng xuất rồi đăng nhập email A → không thấy mục của tài khoản Google · làm lại trên link Vercel.

## Step 7: Đưa online + chuẩn bị Demo Day (Buổi 7)

- **Việc làm:** GitHub → Vercel (xem `huong-dan-online.md`) · cập nhật Site URL / Redirect URLs trên Supabase bằng link Vercel · chạy toàn bộ `kiem-thu-va-demo.md` trên link thật, chụp màn hình làm bằng chứng · tập kịch bản demo 5 phút.
- **Không thêm tính năng mới** trước Demo Day; chỉ sửa lỗi.
- **Test Criteria:** mọi ô trong `kiem-thu-va-demo.md` mục 1–4 được tích trên link Vercel; Security Advisor 0 cảnh báo; không có khóa bí mật trong repo.
