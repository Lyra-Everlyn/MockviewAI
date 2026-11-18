/* Trang nghiên cứu – nhóm Ngũ Hổ Tướng.
   Mọi con số đọc từ data/*.json (gom trong data/data.bundle.js bằng: node build-data.js). */
(function () {
  'use strict';
  const D = window.RESEARCH_DATA;
  const S = D.survey, R = D.research, REFS = D.references.references;
  const $ = (s, r = document) => r.querySelector(s);
  const $$ = (s, r = document) => [...r.querySelectorAll(s)];
  const esc = (t) => String(t ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const f1 = (n) => Number(n).toFixed(1).replace('.', ',');
  const f2 = (n) => Number(n).toFixed(2).replace('.', ',');
  const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const refById = Object.fromEntries(REFS.map((r) => [r.id, r]));
  const refByShort = Object.fromEntries(REFS.map((r) => [r.short, r]));
  const href = (id) => `tai-lieu/${id}.html`;
  const cite = (id, label) => `<a class="cite-btn" href="${href(id)}">${esc(label || refById[id].short)}</a>`;
  const ic = (id) => `<svg class="ic" aria-hidden="true"><use href="#i-${id}"/></svg>`;
  const set = (sel, html) => { const el = $(sel); if (el) el.innerHTML = html; };
  // Trạng thái lọc / tab nằm trên URL (?loai=review&tab=c) để chia sẻ link đúng chỗ
  const params = new URLSearchParams(location.search);
  const setParam = (k, v) => { const u = new URL(location.href); if (v) u.searchParams.set(k, v); else u.searchParams.delete(k); history.replaceState(null, '', u); };

  // Link cũ dạng #ref-<id> → mở trang chi tiết
  if (location.hash.startsWith('#ref-') && refById[location.hash.slice(5)]) location.replace(href(location.hash.slice(5)));

  /* ================= Mở đầu ================= */
  set('.js-title-vi', esc(R.meta.title_vi));
  set('.js-title-en', esc(R.meta.title_en));
  const m = R.origin[0].match(/Trước:\s*(.*?)\s*Trong:\s*(.*?)\s*Sau:\s*(.*?)\.\s/);
  const cap = (t) => t.charAt(0).toUpperCase() + t.slice(1).replace(/\.$/, '') + '.';
  set('.js-stages', m ? [['s-before', 'Trước', m[1], 'book'], ['s-during', 'Trong', m[2], 'mic'], ['s-after', 'Sau', m[3], 'rotate']]
    .map(([c, t, x, i]) => `<li class="${c}"><span class="st-ic">${ic(i)}</span><h2>${t} phỏng vấn</h2><p>${esc(cap(x))}</p></li>`).join('') : '');
  set('.js-meta', esc(['Unit 16 – Computing Research Project', R.meta.school, 'GV hướng dẫn ' + R.meta.tutor].join(' · ')));
  set('.js-n-survey', S.collected.responses);
  set('.js-n-experts', R.experts.used);
  set('.js-n-refs', REFS.length);

  /* ================= Vì sao ================= */
  const ctx = esc(R.origin[3]).replace('(Viet Nam Hoi Nhap, 2026)', cite('vnhn2026', '(Viet Nam Hội Nhập, 2026)'));
  set('.js-story', [
    ['', 'Quan sát', esc(R.origin[0]), 'eye'],
    ['', 'Thu hẹp đề tài', esc(R.origin[1]) + `<span class="aside">${esc(R.origin[2])}</span>`, 'target'],
    ['', 'Bối cảnh', ctx, 'trend'],
    ['gap', 'Khoảng trống nghiên cứu', esc(R.gap), 'search'],
  ].map(([c, t, x, i]) => `<li class="${c}"><span class="story-ic">${ic(i)}</span><h3>${t}</h3><p>${x}</p></li>`).join(''));
  set('.js-theme', `Chủ đề chung của môn ${cite('pearson2026', '(Pearson, 2026)')}: <em lang="en">${esc(R.meta.theme)}</em>. Nghiên cứu đặt <strong>con người – cảm xúc của ứng viên</strong> ở trung tâm, công nghệ đi sau.`);

  /* ================= Câu hỏi – Giả thuyết ================= */
  const badgeQ = (s) => `<span class="badge ${s === 'Đạt một phần' ? 'b-partial' : 'b-no'}">${esc(s)}</span>`;
  set('.js-questions', R.questions.map((q) => {
    const oid = q.objective.split(' ')[0];
    return `<details class="q"><summary><span class="q-id">${q.id}</span><span><p class="q-text">${esc(q.text)}</p><span class="q-obj">${esc(q.objective)}</span></span>${badgeQ(q.status)}</summary>
      <div class="q-body"><p>${esc(R.objective_status_detail[oid] || '')}</p></div></details>`;
  }).join(''));
  const basedOn = (s) => s.split(';').map((x) => x.trim()).map((x) => (refByShort[x] ? cite(refByShort[x].id, x) : esc(x))).join('; ');
  const hypClass = { partial: 'b-partial', undecided: 'b-undecided', untested: 'b-no' };
  set('.js-hyps-before', R.hypotheses.map((h) => `<article class="hyp"><span class="hyp-id">${h.id}</span><p>${esc(h.text)}</p><p class="based">Dựa trên: ${basedOn(h.based_on)}</p></article>`).join(''));
  set('.js-hyps-after', R.hypotheses.map((h) => `<article class="hyp after ${h.color}"><span class="hyp-id">${h.id}</span><p>${esc(h.text)}</p><span><span class="badge ${hypClass[h.color]}">${esc(h.result)}</span></span><p class="evidence">${esc(h.evidence)}</p></article>`).join(''));
  set('.js-wont', R.will_not_do.map((x) => `<li>${esc(x)}</li>`).join(''));

  /* ================= Thư viện tài liệu ================= */
  const ORDER = ['review', 'theory', 'method', 'context'];
  const lib = [...REFS].sort((a, b) => ORDER.indexOf(a._role.key) - ORDER.indexOf(b._role.key));
  // 4 bài nổi bật (3 nhóm chọn + 1 gợi ý thêm), còn lại trong "Tìm hiểu thêm"
  const featured = D.references.featured.map((id) => refById[id]);
  const rest = lib.filter((r) => !r._pick);
  const pickTag = (r) => `<span class="pick ${r._pick}">${r._pick === 'group' ? 'Nhóm chọn' : 'Gợi ý thêm'}</span>`;
  set('.js-lib', featured.map((r) => `<a class="lib-card" href="${href(r.id)}">
    <div class="cover cv-${r._role.key}"><div class="cv-top"><span>${esc(r._.venue || r._.kind)}</span><span class="cv-year">${esc(r._.year)}</span></div><p class="cv-title" lang="en">${esc(r._.title)}</p></div>
    <div class="lib-meta">${pickTag(r)}<span class="lib-name">${esc(r.short.replace(/\s\(\d{4}\)$/, ''))}</span><span class="lib-kind">${esc(r.field || r._role.chip)}${r.sample ? ' · ' + esc(r.sample) : ''}</span></div>
    <p class="lib-find">${esc(r.finding || r.what || '')}</p></a>`).join(''));
  set('.js-more-count', rest.length + ' tài liệu khác');
  set('.js-more-list', rest.map((r) => `<a class="more-row" href="${href(r.id)}" data-role="${r._role.key}">
    <span class="mr-head"><b>${esc(r.short)}</b><span class="mr-kind">${esc(r._role.chip)}${r.field ? ' · ' + esc(r.field) : ''}</span></span>
    <span class="mr-find">${esc(r.finding || r.what || '')}</span><span class="mr-go" aria-hidden="true">›</span></a>`).join(''));
  const counts = ORDER.map((k) => [k, rest.filter((r) => r._role.key === k).length, (lib.find((r) => r._role.key === k) || {})._role]).filter(([, n]) => n);
  set('.js-filter', `<button type="button" aria-pressed="true" data-f="all">Tất cả<em>${rest.length}</em></button>` +
    counts.map(([k, n, role]) => `<button type="button" aria-pressed="false" data-f="${k}">${esc(role.chip)}<em>${n}</em></button>`).join(''));
  function applyFilter(b, keepUrl) {
    $$('.js-filter button').forEach((x) => x.setAttribute('aria-pressed', String(x === b)));
    $$('.js-more-list .more-row').forEach((c) => { c.hidden = b.dataset.f !== 'all' && c.dataset.role !== b.dataset.f; });
    if (!keepUrl) setParam('loai', b.dataset.f === 'all' ? '' : b.dataset.f);
  }
  $('.js-filter').addEventListener('click', (e) => { const b = e.target.closest('button'); if (b) applyFilter(b); });
  const fb = params.get('loai') && $(`.js-filter button[data-f="${CSS.escape(params.get('loai'))}"]`);
  if (fb) { applyFilter(fb, true); $('.js-more').open = true; }
  const reviewed = lib.filter((r) => r._role.key === 'review');
  set('.js-compare', `<thead><tr><th scope="col">Nghiên cứu</th><th scope="col">Phương pháp</th><th scope="col">Mẫu</th><th scope="col">Phát hiện chính</th><th scope="col">Giới hạn</th></tr></thead><tbody>` +
    reviewed.map((r) => `<tr><td>${cite(r.id, r.short)}</td><td>${esc(r.what)}</td><td>${esc(r.sample || '')}</td><td>${esc(r.finding || '')}</td><td>${esc(r.limitation || '')}</td></tr>`).join('') + '</tbody>');

  /* ================= Phương pháp ================= */
  const ds = R.design_steps, survM = R.methods[1], intM = R.methods[2];
  set('.js-flow', `
    <div class="stp"><b>Bước 1</b><span>${esc(ds[0])}</span></div>
    <div class="lanes"><div class="head">Bước 2 · ${esc(ds[1].split(':')[0])}</div>
      <div class="lane">Khảo sát online<em>${esc(survM.actual)}</em></div><div class="lane">Phỏng vấn chuyên gia<em>${esc(intM.actual)}</em></div></div>
    <div class="lanes"><div class="head">Bước 3 · ${esc(ds[2])}</div>
      <div class="lane">Thống kê mô tả<em>đếm, %, trung bình, độ lệch chuẩn (Python)</em></div><div class="lane">Phân tích chủ đề<em>${cite('braun2006', 'Braun & Clarke (2006)')}</em></div></div>
    <div class="stp converge"><b>Bước 4</b><span>${esc(ds[3])}</span></div>
    <div class="stp"><b>Bước 5</b><span>${esc(ds[4])}</span></div>`);
  const stClass = (s) => (s === 'Xong' ? 'done' : s.startsWith('Xong') ? 'part' : 'next');
  set('.js-methods', `<thead><tr><th scope="col">Phương pháp</th><th scope="col">Loại dữ liệu</th><th scope="col">Trả lời</th><th scope="col">Kế hoạch → thực tế</th><th scope="col">Trạng thái</th></tr></thead><tbody>` +
    R.methods.map((x) => `<tr class="${stClass(x.status) === 'next' ? 'next' : ''}"><td><strong>${esc(x.name)}</strong></td><td>${esc(x.type)}</td><td>${esc(x.answers)}</td><td>${[x.plan ? 'Kế hoạch ' + esc(x.plan) : '', x.actual ? esc(x.actual) : ''].filter(Boolean).join(' → ')}</td><td><span class="st ${stClass(x.status)}">${esc(x.status)}</span></td></tr>`).join('') + '</tbody>');
  const samp = esc(R.sampling.survey).replace('(Etikan et al., 2016)', cite('etikan2016', '(Etikan et al., 2016)'));
  set('.js-trio', `
    <div><h3>${ic('chart')}Khảo sát</h3><p>${esc(S.structure)} Thang ${S.scale.points} mức ${cite('likert1932', '(Likert, 1932)')}. Thu từ 07/10 đến 12/10/2026. ${samp}</p></div>
    <div><h3>${ic('users')}Phỏng vấn</h3><p>${esc(R.interview_guide)} ${esc(R.sampling.experts)}</p></div>
    <div><h3>${ic('layers')}Phân tích</h3><p>${esc(S.analysis)} Ghi chú phỏng vấn được mã hoá theo phân tích chủ đề ${cite('braun2006', '(Braun & Clarke, 2006)')}.</p></div>`);
  set('.js-ethics', R.ethics.map((x) => `<li>${esc(x)}</li>`).join(''));

  /* ================= Kết quả: tab ================= */
  const tabs = $$('.tabs [role="tab"]');
  function showTab(t, focus, init) {
    tabs.forEach((x) => { const on = x === t; x.setAttribute('aria-selected', String(on)); x.tabIndex = on ? 0 : -1; $('#' + x.getAttribute('aria-controls')).hidden = !on; });
    if (focus) t.focus();
    if (!init) setParam('tab', t.id === 't-b' ? '' : t.id.slice(2));
    if (t.id === 't-b') animateLikert();
  }
  tabs.forEach((t, i) => {
    t.addEventListener('click', () => showTab(t));
    t.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') { e.preventDefault(); showTab(tabs[(i + (e.key === 'ArrowRight' ? 1 : tabs.length - 1)) % tabs.length], true); }
    });
  });
  tabs.forEach((t) => { t.tabIndex = t.getAttribute('aria-selected') === 'true' ? 0 : -1; });
  const tb0 = params.get('tab') && document.getElementById('t-' + params.get('tab'));

  /* a. Ai đã trả lời */
  const C = S.collected;
  set('.js-collected', `${C.responses} phản hồi / kế hoạch ${C.planned_min} · 07–12/10/2026 · ${esc(C.note)}`);
  const ramp = ['#8f86ff', '#7266d8', '#5a50b0', '#453d8a', '#3a4152'];
  const sampleBlock = (title, rows) => `<div class="sb"><h4>${title}</h4>
    <div class="stack100" role="img" aria-label="${esc(rows.map((r) => `${r[0]}: ${r[1]} người, ${f1(r[2])}%`).join('; '))}">${rows.map((r, i) => `<i style="width:${r[2]}%;background:${ramp[i]}"></i>`).join('')}</div>
    <ul>${rows.map((r, i) => `<li><i style="background:${ramp[i]}"></i><span>${esc(r[0])}</span><b>${r[1]} · ${f1(r[2])}%</b></li>`).join('')}</ul></div>`;
  set('.js-sample', sampleBlock('Nhóm đối tượng', S.sample.group) + sampleBlock('Ngành học', S.sample.field) + sampleBlock('Số lần đã đi phỏng vấn', S.sample.interviews_attended));

  /* b. 12 phát biểu */
  const SHORT = {
    S1: 'Lo kiến thức chưa đủ', S2: 'Học thuộc câu trả lời từng chữ', S3: 'Khó nói điểm mạnh mà không thấy như khoe',
    S4: 'Từng từ chối hoặc bỏ buổi phỏng vấn đã được mời', S5: 'Nói tiếp khi nhà tuyển dụng im lặng', S6: 'Tự tin hơn khi nói về dự án thật',
    S7: 'Online khó đọc phản ứng nhà tuyển dụng hơn', S8: 'Để ý nét mặt nhà tuyển dụng hơn câu hỏi', S9: 'Nhớ câu trả lời chưa tốt rõ hơn câu tốt',
    S10: 'Hiếm khi hỏi lý do không được chọn', S11: 'Thấy kết quả không phản ánh đúng năng lực', S12: 'Sẽ dùng ứng dụng luyện phỏng vấn với AI',
  };
  const isPos = (s) => s.note.includes('tích cực');
  const imgFor = (id) => (id === 'S10' || id === 'S11' ? 'assets/evidence/form_S10-S11.png' : `assets/evidence/form_${id}.png`);
  const STAGES = ['before', 'during', 'after', 'support'];
  const narrow = window.matchMedia('(max-width: 640px)');
  const maxSide = Math.max(...S.statements.map((s) => Math.max(s.disagree_pct + s.neutral_pct / 2, s.agree_pct + s.neutral_pct / 2)));
  const DOM = Math.ceil(maxSide / 10) * 10;
  let mode = 'agree';
  const seg = (cls, w, label, minW) => `<i class="${cls}" style="width:${w}%">${w >= minW ? `<span>${label}</span>` : ''}</i>`;
  function barHTML(s) {
    if (mode === 'mean') {
      const x = (v) => ((Math.min(5, Math.max(1, v)) - 1) / 4 * 100).toFixed(2);
      return `<div class="lk-track"><span class="lk-ref" style="left:${x(3)}%"></span><span class="lk-sd" style="left:${x(s.mean - s.sd)}%;width:${(x(s.mean + s.sd) - x(s.mean - s.sd)).toFixed(2)}%"></span><span class="lk-mean-dot" style="left:${x(s.mean)}%"></span></div>`;
    }
    if (mode === 'dist') {
      return `<div class="lk-track"><div class="lk-bar" style="left:0;width:100%;--origin:0%">${s.counts_1_to_5.map((c, i) => seg('c' + (i + 1) + (i === 2 ? ' neu' : ''), +(c / s.n * 100).toFixed(2), c, 6)).join('')}</div></div>`;
    }
    const tot = s.disagree_pct + s.neutral_pct + s.agree_pct;
    const segs = (minW) => `${seg('c-dis', s.disagree_pct / tot * 100, f1(s.disagree_pct) + '%', minW)}${seg('c-neu neu', s.neutral_pct / tot * 100, f1(s.neutral_pct) + '%', minW)}${seg('c-agr', s.agree_pct / tot * 100, f1(s.agree_pct) + '%', minW)}`;
    if (narrow.matches) return `<div class="lk-track"><div class="lk-bar" style="left:0;width:100%;--origin:0%">${segs(16)}</div></div>`;
    const left = 50 - (s.disagree_pct + s.neutral_pct / 2) / DOM * 50, width = tot / DOM * 50;
    const origin = ((s.disagree_pct + s.neutral_pct / 2) / tot * 100).toFixed(2);
    const outL = s.disagree_pct / tot * 100 < 15 ? `<em class="out">${f1(s.disagree_pct)}%</em>` : '';
    return `<div class="lk-track"><span class="lk-axis"></span><div class="lk-bar" style="left:${left.toFixed(2)}%;width:${width.toFixed(2)}%;--origin:${origin}%">${outL}${segs(15)}</div></div>`;
  }
  function scaleHTML() {
    let t;
    if (mode === 'mean') t = [1, 2, 3, 4, 5].map((v) => [(v - 1) / 4 * 100, String(v)]);
    else if (mode === 'dist' || narrow.matches) t = [0, 25, 50, 75, 100].map((v) => [v, v + '%']);
    else t = [-DOM, -DOM / 2, 0, DOM / 2, DOM].map((v) => [50 + v / DOM * 50, Math.abs(v) + '%']);
    const ends = mode === 'agree' && !narrow.matches ? '← không đồng ý | đồng ý →' : mode === 'mean' ? 'điểm 1–5' : '% người trả lời';
    return `<div class="lk-scale"><span class="ends">${ends}</span><span class="ticks">${t.map(([p, l]) => `<span style="left:${p}%">${l}</span>`).join('')}</span><span></span></div>`;
  }
  function renderLegend() {
    const L = mode === 'agree' ? [['c-dis', 'Không đồng ý (1–2)'], ['c-neu', 'Trung lập (3)'], ['c-agr', 'Đồng ý (4–5)']]
      : mode === 'dist' ? [['c1', '1 Rất không đồng ý'], ['c2', '2'], ['c3', '3'], ['c4', '4'], ['c5', '5 Rất đồng ý']]
        : [['', 'Chấm = trung bình, vạch = ± độ lệch chuẩn, đường đứt = 3,0']];
    set('.js-legend', L.map(([c, t]) => `<span>${c ? `<i class="${c}"></i>` : ''}${esc(t)}</span>`).join(''));
  }
  function renderLikert() {
    const box = $('.js-likert');
    box.dataset.mode = mode;
    box.innerHTML = STAGES.map((st) => `<div class="lk-group g-${st}"><div class="lk-gtitle">${esc(S.stage_labels[st])}</div>` +
      S.statements.filter((s) => s.stage === st).map((s) => `<button class="lk-row" type="button" data-s="${s.id}" aria-label="${esc(`${s.id}: ${s.text_vi}. n = ${s.n}, trung bình ${f2(s.mean)}, độ lệch chuẩn ${f2(s.sd)}; không đồng ý ${f1(s.disagree_pct)}%, trung lập ${f1(s.neutral_pct)}%, đồng ý ${f1(s.agree_pct)}%. Mở ảnh gốc.`)}">
          <span class="lk-label"><b>${s.id}</b>${esc(SHORT[s.id])}${isPos(s) ? '<span class="pos-mark" title="Phát biểu tích cực">+</span>' : ''}</span>
          ${barHTML(s)}<span class="lk-stat">n = ${s.n}<br><b>${f2(s.mean)}</b> ± ${f2(s.sd)}</span></button>`).join('') + '</div>').join('') + scaleHTML();
    renderLegend();
  }
  $$('.seg button').forEach((b) => b.addEventListener('click', () => {
    mode = b.dataset.mode;
    $$('.seg button').forEach((x) => x.setAttribute('aria-checked', String(x === b)));
    renderLikert();
  }));
  narrow.addEventListener('change', renderLikert);
  renderLikert();
  set('.js-likert-table', `<caption>12 phát biểu: số đếm 1–5, n, trung bình, độ lệch chuẩn và tỷ lệ</caption><thead><tr><th>Mã</th><th>Phát biểu</th><th>Số đếm 1–5</th><th>n</th><th>Trung bình</th><th>Độ lệch chuẩn</th><th>% không đồng ý</th><th>% trung lập</th><th>% đồng ý</th></tr></thead><tbody>` +
    S.statements.map((s) => `<tr><td>${s.id}</td><td>${esc(s.text_vi)}</td><td>${s.counts_1_to_5.join(', ')}</td><td>${s.n}</td><td>${f2(s.mean)}</td><td>${f2(s.sd)}</td><td>${f1(s.disagree_pct)}</td><td>${f1(s.neutral_pct)}</td><td>${f1(s.agree_pct)}</td></tr>`).join('') + '</tbody>');
  // biểu đồ "mọc" từ tâm một lần, khi lần đầu nhìn thấy
  let grown = false;
  function animateLikert() {
    if (grown || reduce) return;
    const lk = $('.js-likert'); if (lk.offsetParent === null) return;
    grown = true; lk.classList.add('animate'); setTimeout(() => lk.classList.remove('animate'), 900);
  }
  if ('IntersectionObserver' in window) {
    const io = new IntersectionObserver((es) => { if (es[0].isIntersecting) { animateLikert(); io.disconnect(); } }, { threshold: 0.2 });
    io.observe($('.js-likert'));
  }
  // tooltip + bấm hàng mở ảnh gốc
  const tip = $('.tip');
  const tipFor = (s) => `<b>${s.id}</b> · <span lang="en">${esc(s.text_en)}</span><br>Mức 1–5: ${s.counts_1_to_5.join(' · ')} (n = ${s.n})<br>Không đồng ý ${f1(s.disagree_pct)}% · Trung lập ${f1(s.neutral_pct)}% · Đồng ý ${f1(s.agree_pct)}%`;
  function placeTip(x, y) {
    const r = tip.getBoundingClientRect();
    let top = y + 16; if (top + r.height > window.innerHeight - 8) top = y - r.height - 12;
    tip.style.left = Math.max(8, Math.min(window.innerWidth - r.width - 8, x + 14)) + 'px'; tip.style.top = Math.max(8, top) + 'px';
  }
  const lkBox = $('.js-likert');
  lkBox.addEventListener('pointermove', (e) => {
    if (e.pointerType !== 'mouse') return;
    const row = e.target.closest('.lk-row'); if (!row) { tip.hidden = true; return; }
    tip.innerHTML = tipFor(S.statements.find((s) => s.id === row.dataset.s)); tip.hidden = false; placeTip(e.clientX, e.clientY);
  });
  lkBox.addEventListener('pointerleave', () => { tip.hidden = true; });
  lkBox.addEventListener('focusin', (e) => {
    const row = e.target.closest('.lk-row'); if (!row || !row.matches(':focus-visible')) return;
    const r = row.getBoundingClientRect();
    tip.innerHTML = tipFor(S.statements.find((s) => s.id === row.dataset.s)); tip.hidden = false; placeTip(r.left + 40, r.bottom - 8);
  });
  lkBox.addEventListener('focusout', () => { tip.hidden = true; });
  lkBox.addEventListener('click', (e) => {
    const row = e.target.closest('.lk-row'); if (!row) return;
    tip.hidden = true;
    const stItem = (s) => ({ src: imgFor(s.id), alt: `Biểu đồ Google Forms cho phát biểu ${s.id}`, label: s.id,
      cap: 'Ảnh gốc từ Google Forms; % trên trang tính theo số người trả lời từng câu.',
      info: `<h3>${s.id} · ${esc(s.text_vi)}</h3><p lang="en"><em>${esc(s.text_en)}</em></p>
       <p>n = ${s.n} · trung bình ${f2(s.mean)} ± ${f2(s.sd)} · không đồng ý ${f1(s.disagree_pct)}% · trung lập ${f1(s.neutral_pct)}% · đồng ý ${f1(s.agree_pct)}%</p>
       <div class="lb-dist">${s.counts_1_to_5.map((c, i) => `<span><i class="c${i + 1}"></i>Mức ${i + 1}: ${c}</span>`).join('')}</div>` });
    const idx = S.statements.findIndex((x) => x.id === row.dataset.s);
    openLightbox('', '', '', '', row, S.statements.map(stItem), idx);
  });

  /* c. Dumbbell */
  const x15 = (v) => ((v - 1) / 4 * 100).toFixed(2);
  set('.js-dumbbell', S.statements.map((s) => {
    const a = s.mean_attended_1plus_n45, b = s.mean_never_attended_n8, lo = Math.min(a, b), hi = Math.max(a, b);
    return `<div class="db-row" role="img" aria-label="${esc(`${s.id}: đã phỏng vấn ${f2(a)}, chưa từng ${f2(b)}`)}"><span><b>${s.id}</b> ${esc(SHORT[s.id])}</span>
      <span class="db-track"><span class="db-ref" style="left:${x15(3)}%"></span><span class="db-line" style="left:${x15(lo)}%;width:${(x15(hi) - x15(lo)).toFixed(2)}%"></span>
      <span class="db-dot dot-b" style="left:${x15(b)}%"></span><span class="db-dot dot-a" style="left:${x15(a)}%"></span></span>
      <span class="db-val"><b>${f2(a)}</b> · ${f2(b)}</span></div>`;
  }).join('') + `<div class="db-axis"><span></span><span class="ticks">${[1, 2, 3, 4, 5].map((v) => `<span style="left:${x15(v)}%">${v}</span>`).join('')}</span><span>đã · chưa</span></div>`);
  set('.js-robust', S.robustness.map((x) => `<li>${esc(x)}</li>`).join(''));

  /* d. Tính năng */
  const FW = S.features_wanted;
  set('.js-features', [...FW.items].sort((a, b) => b[1] - a[1]).map(([t, c, p]) =>
    `<div class="hb"><span>${esc(t)}</span><span class="tr" role="img" aria-label="${esc(`${t}: ${c} người, ${f1(p)}%`)}"><span class="fi" style="--w:${p}%"></span></span><b>${c} người · ${f1(p)}%</b></div>`).join('') +
    `<p class="small muted" style="margin:0">${esc(FW.note)}</p>`);

  /* e. Chuyên gia */
  const E = R.experts;
  const prof = E.profiles[0].replace(/^Chuyên gia 1–4:\s*/, '').replace(/\s*\(Không gắn.*\)\s*$/, '');
  set('.js-expert-note', `${E.used} chuyên gia được phân tích (phỏng vấn ${esc(E.dates)}): ${esc(prof)} ${esc(E.data_note)} ${esc(E.excluded)}`);
  set('.js-themes', E.themes.map((t) => `<li><details><summary><span class="tn">${esc(t.theme)}</span>
    <span class="sq" role="img" aria-label="${t.count} trên ${E.used} chuyên gia">${Array.from({ length: E.used }, (_, i) => `<i class="${i < t.count ? 'f' : ''}"></i>`).join('')}<em>${t.count}/${E.used}</em></span></summary>
    <ul>${t.points.map((p) => `<li>${esc(p)}</li>`).join('')}</ul></details></li>`).join(''));

  /* f. Đối chiếu */
  const vcls = { 'Khớp': 'v-yes', 'Khớp, kèm cảnh báo': 'v-warn', 'Khớp một phần': 'v-part', 'Liên quan, không trùng': 'v-rel', 'Không so được': 'v-na' };
  set('.js-tri', R.triangulation.map((t) => `<div class="tri-row"><div><small>Khảo sát</small>${esc(t.survey)}</div><div><small>Phỏng vấn</small>${esc(t.interview)}</div>
    <div><small>Có khớp không?</small><span class="verdict ${vcls[t.verdict] || 'v-part'}">${esc(t.verdict)}</span><span class="tnote">${esc(t.note)}</span></div></div>`).join(''));

  /* ================= Giới hạn · Công cụ · Bước tiếp · Tham khảo ================= */
  set('.js-limits', R.limitations.map((x) => `<li>${esc(x)}</li>`).join(''));
  set('.js-lessons', R.process_lessons.map((x) => `<li>${esc(x)}</li>`).join(''));
  set('.js-recs', R.recommendations.filter((r) => r.for === 'MockviewAI').map((r) => `<div class="rec"><div><small>Phát hiện / khuyến nghị</small>${esc(r.rec)}</div><div><small>Bằng chứng</small>${esc(r.why)}</div>
    <div class="feat ${/^Chưa làm/.test(r.feature) ? 'notyet' : ''}"><small>Tính năng đã làm</small>${esc(r.feature)}</div></div>`).join(''));
  const order = (r) => (/mô phỏng/.test(r.rec) ? 0 : /buổi 5/.test(r.rec) ? 1 : 2);
  set('.js-next', R.recommendations.filter((r) => r.for.startsWith('Nghiên cứu')).sort((a, b) => order(a) - order(b))
    .map((r) => `<li><span>${esc(r.rec)}<span class="why">${esc(r.why)}</span></span></li>`).join(''));
  set('.js-biblio', [...REFS].sort((a, b) => a.citation.localeCompare(b.citation, 'vi')).map((r) => {
    const i = r.citation.indexOf(') ');
    const head = i > 0 ? r.citation.slice(0, i + 1) : r.short, tail = i > 0 ? r.citation.slice(i + 1) : r.citation;
    const link = r._href ? ` <a class="ext" href="${esc(r._href)}" target="_blank" rel="noopener">${r.doi ? 'doi:' + esc(r.doi) : 'Mở nguồn'}</a>` : '';
    return `<li id="tk-${r.id}">${cite(r.id, head)}${esc(tail)}${link}</li>`;
  }).join(''));
  set('.js-ai', 'Tóm tắt: ' + esc(R.ai_use));

  if (tb0) showTab(tb0, false, true);

  /* ================= Khung xem ảnh (thư viện: chuyển ảnh, phóng to, kéo, vuốt) ================= */
  const lb = $('.lb'), lbImg = $('img', lb), lbCanvas = $('.lb-canvas', lb), lbStrip = $('.lb-strip', lb);
  const ZOOMS = [1, 1.5, 2, 3];
  let lbReturn = null, lbList = [], lbIdx = 0, zoom = 0;          // zoom 0 = vừa khung
  function setZoom(z) {
    zoom = Math.max(0, Math.min(ZOOMS.length, z));
    lb.classList.toggle('zoomed', zoom > 0);
    if (zoom > 0) {
      const fitW = Math.min(lbImg.naturalWidth || 1000, lbCanvas.clientWidth - 24);
      lbImg.style.width = Math.round(fitW * ZOOMS[zoom - 1] * (zoom === 1 ? 1.5 : 1)) + 'px';
    } else lbImg.style.width = '';
    $('.lb-zoom', lb).textContent = zoom ? `${Math.round((zoom === 1 ? 1.5 : ZOOMS[zoom - 1]) * 100)}%` : 'Vừa';
  }
  function show(i) {
    lbIdx = (i + lbList.length) % lbList.length;
    const it = lbList[lbIdx];
    lb.classList.add('loading'); setZoom(0);
    lbImg.onload = () => lb.classList.remove('loading');
    lbImg.src = it.src; lbImg.alt = it.alt || '';
    $('.lb-open', lb).href = it.src;
    $('.lb-cap', lb).textContent = it.cap || '';
    $('.lb-info', lb).innerHTML = it.info || '';
    const many = lbList.length > 1;
    lb.classList.toggle('single', !many);
    $('.lb-count', lb).textContent = many ? `Ảnh ${lbIdx + 1} / ${lbList.length}` : 'Ảnh gốc';
    $$('button', lbStrip).forEach((b, k) => { b.setAttribute('aria-selected', String(k === lbIdx)); if (k === lbIdx) b.scrollIntoView({ block: 'nearest', inline: 'center' }); });
    $('.lb-body', lb).scrollTop = 0;
  }
  function openLightbox(src, alt, capTxt, infoHTML, from, list, index) {
    lbReturn = from || document.activeElement;
    lbList = list && list.length ? list : [{ src, alt, cap: capTxt, info: infoHTML }];
    lbStrip.innerHTML = lbList.length > 1 ? lbList.map((it, k) => `<button type="button" role="tab" aria-label="${esc(it.label || 'Ảnh ' + (k + 1))}"><img src="${esc(it.src)}" alt="" loading="lazy"><span>${esc(it.label || String(k + 1))}</span></button>`).join('') : '';
    $$('button', lbStrip).forEach((b, k) => b.addEventListener('click', () => show(k)));
    lb.hidden = false; document.body.classList.add('lock');
    show(index || 0);
    $('.x', lb).focus();
  }
  function closeLightbox() {
    if (lb.hidden) return;
    lb.hidden = true; document.body.classList.remove('lock'); lbImg.removeAttribute('src');
    if (lbReturn && document.contains(lbReturn)) lbReturn.focus({ preventScroll: true });
  }
  // Ảnh thu nhỏ: các ảnh trong cùng một mục là một bộ, chuyển qua lại được
  $$('.js-lightbox').forEach((b) => b.addEventListener('click', () => {
    const group = $$('.js-lightbox', b.closest('section') || document).filter((x) => x.offsetParent !== null || x === b);
    const list = group.map((x) => ({ src: x.dataset.img, alt: x.dataset.alt, cap: x.dataset.cap, label: (x.dataset.cap || x.dataset.alt || '').split(':')[0].slice(0, 28) }));
    openLightbox('', '', '', '', b, list, group.indexOf(b));
  }));
  $('.x', lb).addEventListener('click', closeLightbox);
  $('.prev', lb).addEventListener('click', () => show(lbIdx - 1));
  $('.next', lb).addEventListener('click', () => show(lbIdx + 1));
  $$('[data-z]', lb).forEach((b) => b.addEventListener('click', () => setZoom(b.dataset.z === 'in' ? zoom + 1 : b.dataset.z === 'out' ? zoom - 1 : 0)));
  lbImg.addEventListener('dblclick', () => setZoom(zoom ? 0 : 2));
  lb.addEventListener('click', (e) => { if (e.target === lb || e.target.classList.contains('lb-body')) closeLightbox(); });
  // Kéo để xem khi đang phóng to; vuốt ngang để chuyển ảnh khi đang vừa khung
  let drag = null;
  lbCanvas.addEventListener('pointerdown', (e) => { drag = { x: e.clientX, y: e.clientY, sl: lbCanvas.scrollLeft, st: lbCanvas.scrollTop, moved: false }; if (zoom) lbCanvas.setPointerCapture(e.pointerId); });
  lbCanvas.addEventListener('pointermove', (e) => {
    if (!drag || !zoom) return;
    lbCanvas.scrollLeft = drag.sl - (e.clientX - drag.x); lbCanvas.scrollTop = drag.st - (e.clientY - drag.y); drag.moved = true;
  });
  lbCanvas.addEventListener('pointerup', (e) => {
    if (drag && !zoom && lbList.length > 1) { const dx = e.clientX - drag.x; if (Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(e.clientY - drag.y)) show(lbIdx + (dx < 0 ? 1 : -1)); }
    drag = null;
  });
  document.addEventListener('keydown', (e) => {
    if (lb.hidden) return;
    if (e.key === 'Escape') { e.preventDefault(); closeLightbox(); }
    else if (e.key === 'ArrowRight' && lbList.length > 1) { e.preventDefault(); show(lbIdx + 1); }
    else if (e.key === 'ArrowLeft' && lbList.length > 1) { e.preventDefault(); show(lbIdx - 1); }
    else if (e.key === '+' || e.key === '=') setZoom(zoom + 1);
    else if (e.key === '-') setZoom(zoom - 1);
    else if (e.key === 'Tab') {                                     // giữ Tab trong khung xem ảnh
      const f = $$('button:not([hidden]), a[href]', lb).filter((x) => x.offsetParent !== null);
      const i = f.indexOf(document.activeElement);
      if (e.shiftKey && i <= 0) { e.preventDefault(); f[f.length - 1].focus(); }
      else if (!e.shiftKey && i === f.length - 1) { e.preventDefault(); f[0].focus(); }
    }
  });

  /* ================= Menu + tiến độ đọc ================= */
  const menuBtn = $('.menu-btn'), toc = $('#toc');
  menuBtn.addEventListener('click', () => { const o = menuBtn.getAttribute('aria-expanded') !== 'true'; menuBtn.setAttribute('aria-expanded', String(o)); toc.classList.toggle('open', o); });
  toc.addEventListener('click', (e) => { if (e.target.closest('a')) { toc.classList.remove('open'); menuBtn.setAttribute('aria-expanded', 'false'); } });
  const secs = $$('main > section[id]'), bar = $('.read-progress span');
  let tick = false;
  function onScroll() {
    tick = false;
    const h = document.documentElement.scrollHeight - window.innerHeight;
    bar.style.setProperty('--p', h > 0 ? (window.scrollY / h).toFixed(4) : 0);
    const y = window.scrollY + 120; let cur = null;
    secs.forEach((s) => { if (s.offsetTop <= y) cur = s.id; });
    $$('.toc a').forEach((a) => a.classList.toggle('on', a.getAttribute('href') === '#' + cur));
  }
  window.addEventListener('scroll', () => { if (!tick) { tick = true; requestAnimationFrame(onScroll); } }, { passive: true });
  onScroll();
})();
