// Dựng dữ liệu + trang chi tiết tài liệu từ data/*.json.
// Chạy lại sau mỗi lần sửa JSON:   node build-data.js
//  1) data/data.bundle.js   – trang chính đọc file này (chạy được cả khi mở thẳng file)
//  2) tai-lieu/<id>.html    – 18 trang chi tiết, mỗi tài liệu một trang
const fs = require('fs'), path = require('path');
const dir = __dirname;
const read = (f) => JSON.parse(fs.readFileSync(path.join(dir, 'data', f), 'utf8'));
const survey = read('survey.json'), research = read('research.json'), refsFile = read('references.json');
const REFS = refsFile.references;

const ROLE = {
  'Nghiên cứu được tổng quan': { key: 'review', chip: 'Được tổng quan' },
  'Lý thuyết nền': { key: 'theory', chip: 'Lý thuyết nền' },
  'Phương pháp': { key: 'method', chip: 'Phương pháp' },
  'Bối cảnh': { key: 'context', chip: 'Bối cảnh' },
};

// Tách tác giả – năm – tên bài – nơi đăng từ trích dẫn Harvard
function meta(r) {
  const m = r.citation.match(/^(.*?)\s\((\d{4})\)\s(.*)$/);
  const authors = m ? m[1] : r.short, year = m ? m[2] : '', rest = m ? m[3] : r.citation;
  let title, venue, kind;
  const q = rest.match(/^'(.+?)',\s*(?:in\s+)?(.*)$/);
  if (q) {
    title = q[1];
    venue = q[2].split(/,\s*(?=\d|pp\.|article)|\.\s/)[0].replace(/\.$/, '');
    kind = 'Bài báo khoa học';
  } else if (/edn\./.test(rest)) {
    title = rest.split('. ')[0];
    venue = (rest.match(/:\s*([^:.]+)\.?$/) || [])[1] || '';
    kind = 'Sách';
  } else if (r.id === 'pearson2026') {
    title = rest.replace(/\.$/, ''); venue = 'Pearson'; kind = 'Tài liệu môn học';
  } else {
    title = rest.split(/\.\s*\(/)[0].replace(/\.$/, ''); venue = authors; kind = 'Tin tức';
  }
  return { title, venue, authors, year, kind };
}
// 3 bài nhóm chọn + 1 bài gợi ý thêm: hiện ở đầu thư viện; còn lại nằm trong "Tìm hiểu thêm"
const PICK = { zielinska2021: 'group', gomez2025: 'group', wilkie2024: 'group', santos2021: 'extra' };
const FEATURED = ['zielinska2021', 'gomez2025', 'wilkie2024', 'santos2021'];
REFS.forEach((r) => {
  Object.assign(r, { _: meta(r), _role: ROLE[r.role] || ROLE['Bối cảnh'], _href: r.link || r.url || null, _pick: PICK[r.id] || null });
});
refsFile.featured = FEATURED;
// Giả thuyết dựa trên tài liệu nào
const byShort = Object.fromEntries(REFS.map((r) => [r.short, r]));
research.hypotheses.forEach((h) => h.based_on.split(';').map((s) => s.trim()).forEach((s) => {
  if (byShort[s]) (byShort[s]._hyps = byShort[s]._hyps || []).push(h.id);
}));

/* ---------- 1) bundle ---------- */
fs.writeFileSync(path.join(dir, 'data', 'data.bundle.js'),
  `/* TỰ SINH từ data/*.json bằng build-data.js — đừng sửa tay */\nwindow.RESEARCH_DATA = ${JSON.stringify({ survey, research, references: refsFile })};\n`);

/* ---------- 2) trang chi tiết ---------- */
const esc = (t) => String(t ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const ORDER = ['review', 'theory', 'method', 'context'];
const lib = [...REFS].sort((a, b) => ORDER.indexOf(a._role.key) - ORDER.indexOf(b._role.key));

function card(r, prefix) {
  const find = r.finding || r.what || '';
  return `<a class="lib-card" href="${prefix}${r.id}.html">
  <div class="cover cv-${r._role.key}"><div class="cv-top"><span>${esc(r._.venue || r._.kind)}</span><span class="cv-year">${esc(r._.year)}</span></div><p class="cv-title">${esc(r._.title)}</p></div>
  <div class="lib-meta">${r._pick ? `<span class="pick ${r._pick}">${r._pick === 'group' ? 'Nhóm chọn' : 'Gợi ý thêm'}</span>` : ''}<span class="lib-name">${esc(r.short.replace(/\s\(\d{4}\)$/, ''))}</span><span class="lib-kind">${esc(r.field || r._role.chip)}</span></div>
  <p class="lib-find">${esc(find)}</p></a>`;
}

/* ---------- Sơ đồ tự vẽ (chỉ dùng số liệu có trong references.json) ---------- */
// Sơ đồ cách làm: [ai tham gia, làm gì, ra kết quả gì] hoặc 2 bước [ý chính, nhóm dùng vào đâu]
const FLOW = {
  santos2021: ['436 người ở Bồ Đào Nha', 'Trả lời bảng hỏi MASI, phân tích nhân tố', 'Thang đo khớp 5 phần; người trẻ, ít phỏng vấn lo hơn'],
  luo2024: ['19 sinh viên đại học', 'Phỏng vấn trong thực tế ảo, đo điện da và bảng hỏi', 'Câu hỏi chuyên môn gây lo hơn câu hỏi cá nhân'],
  zielinska2021: ['31 người dự khoá (sinh viên y)', 'Khoá học 1 ngày, phỏng vấn thử online, đo MASI trước và sau', 'Điểm lo lắng giảm ở cả 5 phần'],
  wilkie2024: ['Sinh viên đại học (không nêu cỡ mẫu)', 'Phỏng vấn thử trực tuyến thời Covid-19, hỏi cảm nhận', 'Thấy làm tốt hơn khi phỏng vấn thật'],
  daryanto2025: ['19 người trưởng thành', 'Luyện với Conversate: AI hỏi nối tiếp, phản hồi', 'Ít căng thẳng hơn, được sai và thử lại'],
  gomez2025: ['20 người tham gia', 'Dùng công cụ phỏng vấn kỹ thuật thử bằng AI', 'Thấy chân thực, hữu ích, tự tin hơn'],
  nofal2025: ['Không có người tham gia (kiểm thử hệ thống)', 'Đổi quốc gia, tôn giáo, giới tính với cùng câu trả lời', 'Có dấu hiệu thiên lệch → dùng thận trọng'],
  mccarthy2004: ['Xây dựng thang đo MASI', 'Định nghĩa lo lắng khi phỏng vấn; đề xuất dùng MASI lần sau'],
  constantin2021: ['Lo lắng nhìn qua 3 mặt', 'Dùng cho phần định nghĩa'],
  schneider2019: ['Lo lắng có làm sai lệch dự đoán của phỏng vấn?', 'Lo lắng là vấn đề công bằng cho ứng viên'],
  braun2006: ['Ghi chú phỏng vấn 4 chuyên gia', 'Mã hoá theo phân tích chủ đề', '7 chủ đề'],
  creswell2018: ['Khảo sát (định lượng) và phỏng vấn (định tính) song song', 'Phân tích riêng rồi đặt cạnh nhau'],
  etikan2016: ['Thuận tiện → người khảo sát', 'Có mục đích → chuyên gia'],
  saunders2019: ['Giáo trình phương pháp nghiên cứu', 'Đánh giá SWOT các phương pháp'],
  likert1932: ['Thang đo thái độ 5 mức', 'Dùng cho 12 phát biểu S1–S12'],
  russell2021: ['Nghe câu trả lời', 'Quyết định hỏi gì tiếp', 'Phản hồi'],
  vnhn2026: ['Thất nghiệp 15–24 tuổi quý IV/2025: 9,04%', 'Chỉ để cho thấy áp lực chung lên người trẻ'],
  pearson2026: ['Chủ đề môn: Human-Centred Computing', 'Đặt đề tài vào chủ đề Pearson'],
};
const MASI = ['Giao tiếp', 'Ngoại hình', 'Xã hội', 'Kết quả', 'Hành vi'];
function flowFig(r) {
  const f = FLOW[r.id]; if (!f) return '';
  const heads = f.length === 3
    ? (r._role.key === 'review' ? ['Ai tham gia', 'Nghiên cứu làm gì', 'Phát hiện'] : ['Bước 1', 'Bước 2', 'Bước 3'])
    : ['Ý chính', 'Nhóm dùng vào'];
  return `<figure class="fig"><figcaption>${r._role.key === 'review' ? 'Sơ đồ nghiên cứu' : 'Tóm tắt bằng sơ đồ'}</figcaption>
    <ol class="fig-flow n${f.length}">${f.map((t, k) => `<li><small>${heads[k]}</small><span>${esc(t)}</span></li>`).join('')}</ol></figure>`;
}
function extraFig(r) {
  switch (r.id) {
    case 'zielinska2021': {
      const steps = [[31, 'người dự khoá'], [28, 'tham gia phỏng vấn thử'], [22, 'hoàn thành bảng hỏi sau khoá']];
      const p = [['Lo lắng xã hội', 'p = 0,004'], ['Lo lắng về kết quả', 'p < 0,001'], ['Lo lắng hành vi', 'p = 0,003'], ['Tự tin với phỏng vấn online', 'p = 0,009']];
      return `<figure class="fig"><figcaption>Số người qua từng bước</figcaption>
        <div class="funnel">${steps.map(([n, t]) => `<div class="fn-row"><span class="fn-bar" style="--w:${(n / 31 * 100).toFixed(1)}%"><b>${n}</b></span><span>${t}</span></div>`).join('')}</div>
        <p class="fig-note">Chỉ 22 người có điểm sau khoá, nên kết quả trước – sau dựa trên nhóm nhỏ này.</p></figure>
        <figure class="fig"><figcaption>Thay đổi có ý nghĩa thống kê sau khoá học</figcaption>
        <div class="ptiles">${p.map(([t, v]) => `<div><span>${t}</span><b>${v}</b></div>`).join('')}</div>
        <p class="fig-note">Ba dòng đầu là điểm lo lắng giảm, dòng cuối là tự tin tăng.</p></figure>`;
    }
    case 'santos2021':
      return `<figure class="fig"><figcaption>Năm phần của thang MASI được xác nhận trên 436 người</figcaption>
        <div class="masi">${MASI.map((t) => `<span>${t}</span>`).join('')}</div>
        <div class="who"><small>Lo lắng cao hơn ở</small><span>Người trẻ hơn</span><span>Người đi phỏng vấn ít hơn</span><span>Người tự thấy buổi gần nhất làm chưa tốt</span></div></figure>`;
    case 'mccarthy2004':
      return `<figure class="fig"><figcaption>Năm phần của lo lắng khi phỏng vấn (MASI)</figcaption><div class="masi">${MASI.map((t) => `<span>${t}</span>`).join('')}</div></figure>`;
    case 'constantin2021':
      return `<figure class="fig"><figcaption>Ba mặt của lo lắng khi phỏng vấn</figcaption><div class="masi m3"><span>Nhận thức</span><span>Hành vi</span><span>Sinh lý</span></div></figure>`;
    case 'likert1932':
      return `<figure class="fig"><figcaption>Thang 5 mức nhóm dùng</figcaption><div class="likert5">${['Rất không đồng ý', 'Không đồng ý', 'Trung lập', 'Đồng ý', 'Rất đồng ý'].map((t, k) => `<span class="c${k + 1}"><b>${k + 1}</b>${t}</span>`).join('')}</div></figure>`;
    case 'gomez2025':
      return `<figure class="fig"><figcaption>Người tham gia nói gì (20 người)</figcaption>
        <div class="procon"><div class="pro"><h4>Điểm được khen</h4><ul><li>Chân thực, hữu ích</li><li>Tự tin hơn</li><li>Giải thích cách giải quyết vấn đề tốt hơn</li></ul></div>
        <div class="con"><h4>Vấn đề gặp phải</h4><ul><li>Mạch hội thoại chưa mượt</li><li>Thời gian</li></ul></div></div></figure>`;
    case 'wilkie2024':
      return `<figure class="fig"><figcaption>Theo cảm nhận của sinh viên</figcaption>
        <ol class="fig-flow n3 hl1"><li><small>Quan trọng nhất</small><span>Chuẩn bị trước buổi mô phỏng</span></li><li><small>Luyện</small><span>Phỏng vấn thử trực tuyến</span></li><li><small>Cảm nhận</small><span>Làm tốt hơn khi phỏng vấn thật</span></li></ol>
        <p class="fig-note">Đây là cảm nhận của sinh viên, bài không đo thay đổi lo lắng.</p></figure>`;
  }
  return '';
}

function page(r, i) {
  const prev = lib[(i - 1 + lib.length) % lib.length], next = lib[(i + 1) % lib.length];
  const related = lib.filter((x) => x !== r && x._role.key === r._role.key).concat(lib.filter((x) => x !== r && x._role.key !== r._role.key)).slice(0, 3);
  const part = (t, v, cls = '') => (v ? `<div class="card ${cls}"><h3>${t}</h3><p>${esc(v)}</p></div>` : '');
  const hyps = (r._hyps || []).map((id) => research.hypotheses.find((h) => h.id === id));
  const tags = [r._.kind, r.role, r.field].filter(Boolean).map((t) => `<span>${esc(t)}</span>`).join('');
  const cta = r._href
    ? `<a class="btn btn-light" href="${esc(r._href)}" target="_blank" rel="noopener">Đọc bài gốc${r.doi ? ' trên trang nhà xuất bản' : ''} ↗</a>`
    : `<span class="btn btn-dark" aria-disabled="true">Không có bản trực tuyến${r._.kind === 'Sách' ? ' (sách in)' : ''}</span>`;
  return `<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(r.short)} — Thư viện tài liệu | Nghiên cứu nhóm Ngũ Hổ Tướng</title>
<meta name="description" content="${esc((r.finding || r.what || r._.title).slice(0, 180))}">
<meta name="theme-color" content="#0a0f1c">
<meta name="color-scheme" content="dark">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&family=Space+Grotesk:wght@500;600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="../styles.css">
</head>
<body>
<a class="skip" href="#main">Bỏ qua, tới nội dung chính</a>
<header class="topbar"><div class="wrap topbar-in">
  <a class="brand" href="../index.html#tai-lieu"><span class="brand-back" aria-hidden="true">←</span> Thư viện tài liệu</a>
  <button class="search-btn js-open-palette" type="button" aria-label="Tìm nhanh trong nghiên cứu" aria-keyshortcuts="Control+K Meta+K"><svg viewBox="0 0 20 20" aria-hidden="true"><circle cx="9" cy="9" r="6"/><path d="m14 14 4 4"/></svg><span class="search-lbl">Tìm nhanh</span><span class="keys" aria-hidden="true"><kbd class="js-mod">Ctrl</kbd><kbd>K</kbd></span></button>
</div></header>
<main class="detail" id="main">
  <div class="wrap"><div class="d-shell">
    <article class="d-card" aria-labelledby="d-title">
      <div class="d-cover cv-${r._role.key}">
        <a class="d-back" href="../index.html#tai-lieu"><span aria-hidden="true">←</span>Quay lại thư viện</a>
        <p class="d-venue"><span>${esc(r._.venue || r._.kind)}</span>${r._.year ? `<span class="yr">${esc(r._.year)}</span>` : ''}</p>
        <h1 class="d-title" id="d-title" lang="en">${esc(r._.title)}</h1>
      </div>
      <div class="d-body">
        <p class="d-name">${esc(r.short)}</p>
        <p class="d-authors">${esc(r._.authors)}</p>
        <div class="d-tags">${tags}</div>
        <div class="d-actions">${cta}<button class="btn btn-dark copy-btn" type="button" data-copy="${esc(r.citation)}">Sao chép trích dẫn</button></div>
        ${r.doi ? `<div class="d-check doi-check" data-doi="${esc(r.doi)}"></div>` : ''}
        <p class="d-note">Nhóm tóm tắt từ bài gốc; sơ đồ do AI vẽ từ số liệu nhóm cung cấp và đã được nhóm kiểm lại (<a href="../index.html#su-dung-ai">khai báo sử dụng AI</a>). Toàn văn thuộc bản quyền của tác giả và nhà xuất bản${r._href ? ', bấm “Đọc bài gốc” để mở' : ''}.</p>
      </div>
    </article>

    <div class="figs">${flowFig(r)}${extraFig(r)}</div>

    <div class="d-grid">
      ${part('Nghiên cứu đã làm gì', r.what, r.sample ? '' : 'wide')}
      ${part('Mẫu', r.sample)}
      ${part('Phát hiện chính', r.finding, r.limitation ? '' : 'wide')}
      ${part('Giới hạn', r.limitation)}
      ${part('Nhóm dùng tài liệu này thế nào', r.used_for, 'wide d-used')}
      ${hyps.length ? `<div class="card wide"><h3>Là căn cứ cho giả thuyết</h3><div class="d-hyps">${hyps.map((h) => `<a href="../index.html#gia-thuyet"><b>${h.id}</b>${esc(h.text)} <span class="muted">· ${esc(h.result)}</span></a>`).join('')}</div></div>` : ''}
      <div class="card wide d-cite"><h3>Trích dẫn (Harvard)</h3><p>${esc(r.citation)}${r.doi ? ` doi:${esc(r.doi)}` : ''}</p></div>
    </div>

    <section class="d-more" aria-labelledby="more-t">
      <h2 id="more-t">Tài liệu khác trong thư viện</h2>
      <div class="lib">${related.map((x) => card(x, '')).join('')}</div>
      <nav class="d-pager" aria-label="Chuyển tài liệu">
        <a class="btn btn-ghost" href="${prev.id}.html">← ${esc(prev.short)}</a>
        <a class="btn btn-ghost" href="${next.id}.html">${esc(next.short)} →</a>
      </nav>
    </section>
  </div></div>
</main>
<footer class="foot"><div class="wrap foot-in"><p>Nhóm Ngũ Hổ Tướng · BTEC FPT Hà Nội · 2026 · <a href="../index.html#su-dung-ai">Khai báo sử dụng AI</a></p><a href="../index.html">← Về trang nghiên cứu</a></div></footer>
<script src="../../assets/doi-check.js"></script>
<script src="../data/data.bundle.js"></script>
<script src="../palette.js" data-base="../"></script>
<script>
document.querySelectorAll('.copy-btn').forEach(function (b) {
  b.addEventListener('click', function () {
    var t = b.getAttribute('data-copy'), done = function () { b.textContent = 'Đã sao chép'; setTimeout(function () { b.textContent = 'Sao chép trích dẫn'; }, 1800); };
    if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(t).then(done, function () { fallback(); });
    else fallback();
    function fallback() { var a = document.createElement('textarea'); a.value = t; document.body.appendChild(a); a.select(); try { document.execCommand('copy'); done(); } catch (e) {} a.remove(); }
  });
});
</script>
</body>
</html>
`;
}
const out = path.join(dir, 'tai-lieu');
fs.mkdirSync(out, { recursive: true });
lib.forEach((r, i) => fs.writeFileSync(path.join(out, r.id + '.html'), page(r, i)));
console.log(`Đã tạo data/data.bundle.js và ${lib.length} trang trong tai-lieu/`);
