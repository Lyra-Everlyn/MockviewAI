/* Tìm nhanh (Ctrl K / ⌘ K) — tìm trong mục, tài liệu, giả thuyết, phát biểu.
   Dùng chung cho trang chính và trang chi tiết: <script src="(..)/palette.js" data-base="../"></script>
   Cần window.RESEARCH_DATA (data/data.bundle.js) nạp trước. */
(function () {
  'use strict';
  const D = window.RESEARCH_DATA; if (!D) return;
  const me = document.currentScript;
  const base = (me && me.dataset.base) || '';
  const home = base + 'index.html';
  const esc = (t) => String(t ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const fold = (t) => String(t).toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd');
  const isMac = /Mac|iPhone|iPad/.test(navigator.platform || navigator.userAgent);
  const MOD = isMac ? '⌘' : 'Ctrl';

  const SECTIONS = [
    ['vi-sao', 'Vì sao có nghiên cứu này', 'quan sát, bối cảnh, khoảng trống'],
    ['cau-hoi', 'Câu hỏi, mục tiêu và giả thuyết', 'Q1–Q4, O1–O4'],
    ['tai-lieu', 'Thư viện tài liệu', '18 tài liệu, bảng so sánh'],
    ['phuong-phap', 'Phương pháp và đạo đức', 'hỗn hợp hội tụ, khảo sát, phỏng vấn'],
    ['ket-qua', 'Kết quả', 'biểu đồ 12 phát biểu, chuyên gia, đối chiếu'],
    ['gia-thuyet', 'Giả thuyết đã đi tới đâu', 'H1–H4'],
    ['gioi-han', 'Giới hạn và bài học', 'mẫu nhỏ, thang đo'],
    ['cong-cu', 'Từ kết quả đến công cụ', 'MockViewAI, bước tiếp theo'],
    ['tham-khao', 'Tài liệu tham khảo', 'Harvard, DOI'],
    ['su-dung-ai', 'Khai báo sử dụng AI', 'Claude, Consensus, Stitch; nhóm kiểm tra gì'],
  ];
  const R = D.research, S = D.survey, REFS = D.references.references;
  const items = [
    ...SECTIONS.map(([id, t, s]) => ({ g: 'Mục', t, s, href: home + '#' + id, k: '§' })),
    ...REFS.map((r) => ({ g: 'Tài liệu', t: r.short, s: (r._ && r._.title) || r.what || '', href: base + 'tai-lieu/' + r.id + '.html', k: 'Doc', x: [r.field, r.role, r.finding].join(' ') })),
    ...R.hypotheses.map((h) => ({ g: 'Giả thuyết', t: h.id + ' · ' + h.text, s: h.result, href: home + '#gia-thuyet', k: h.id })),
    ...S.statements.map((st) => ({ g: 'Phát biểu', t: st.id + ' · ' + st.text_vi, s: 'đồng ý ' + String(st.agree_pct).replace('.', ',') + '%', href: home + '?tab=b#ket-qua', k: st.id, x: st.text_en })),
  ];
  items.forEach((it, i) => { it.i = i; it.f = fold([it.t, it.s, it.x || ''].join(' ')); });

  function search(q) {
    const words = fold(q).trim().split(/\s+/).filter(Boolean);
    if (!words.length) return items.filter((it) => it.g === 'Mục' || it.g === 'Tài liệu').slice(0, 14);
    return items.map((it) => {
      if (!words.every((w) => it.f.includes(w))) return null;
      const t = fold(it.t); let score = 0;
      words.forEach((w) => { if (t.startsWith(w)) score += 3; else if (t.includes(w)) score += 2; else score += 1; });
      return [score, it];
    }).filter(Boolean).sort((a, b) => b[0] - a[0] || a[1].i - b[1].i).slice(0, 30).map((x) => x[1]);
  }

  let uid = 0;
  /* Một ô tìm kiếm: root chứa .pal-input và .pal-list */
  function makePalette(root, onDone) {
    const id = 'pal' + (++uid);
    const input = root.querySelector('.pal-input'), list = root.querySelector('.pal-list'), count = root.querySelector('.pal-count');
    list.id = id + '-list';
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-controls', list.id);
    input.setAttribute('aria-expanded', 'true');
    input.setAttribute('aria-autocomplete', 'list');
    let res = [], cur = 0;
    function render() {
      res = search(input.value); cur = 0; list.scrollTop = 0;
      let g = '';
      list.innerHTML = res.length ? res.map((it, n) => {
        const head = it.g !== g ? `<li class="pal-group" role="presentation">${esc((g = it.g))}</li>` : '';
        return head + `<li role="presentation"><a role="option" id="${id}-o${n}" class="pal-opt" href="${esc(it.href)}" tabindex="-1" aria-selected="${n === 0}">
          <span class="pal-k">${esc(it.k)}</span><span class="pal-t"><span>${esc(it.t)}</span><small>${esc(it.s)}</small></span><span class="pal-go" aria-hidden="true">↵</span></a></li>`;
      }).join('') : `<li class="pal-empty" role="presentation">Không có kết quả cho “${esc(input.value)}”. Thử từ ngắn hơn, ví dụ “lo lắng” hoặc “MASI”.</li>`;
      if (count) count.textContent = res.length + ' kết quả';
      sync();
    }
    function sync() {
      list.querySelectorAll('.pal-opt').forEach((o, n) => o.setAttribute('aria-selected', String(n === cur)));
      const o = list.querySelector(`#${id}-o${cur}`);
      if (!o) { input.removeAttribute('aria-activedescendant'); return; }
      input.setAttribute('aria-activedescendant', o.id);
      // cuộn trong danh sách, không cuộn cả trang
      const lr = list.getBoundingClientRect(), r = o.getBoundingClientRect();
      if (r.top < lr.top) list.scrollTop -= lr.top - r.top + 8;
      else if (r.bottom > lr.bottom) list.scrollTop += r.bottom - lr.bottom + 8;
    }
    input.addEventListener('input', render);
    input.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault(); if (!res.length) return;
        cur = (cur + (e.key === 'ArrowDown' ? 1 : res.length - 1)) % res.length; sync();
      } else if (e.key === 'Enter' && res[cur]) {
        e.preventDefault(); go(res[cur].href, e.ctrlKey || e.metaKey);
      }
    });
    list.addEventListener('mousemove', (e) => {
      const o = e.target.closest('.pal-opt'); if (!o) return;
      const n = +o.id.split('-o')[1]; if (n !== cur) { cur = n; list.querySelectorAll('.pal-opt').forEach((x, k) => x.setAttribute('aria-selected', String(k === cur))); }
    });
    list.addEventListener('click', (e) => { const o = e.target.closest('.pal-opt'); if (o && !e.ctrlKey && !e.metaKey && !e.shiftKey && e.button === 0) { e.preventDefault(); go(o.getAttribute('href'), false); } });
    function go(href, newTab) {
      if (newTab) { window.open(href, '_blank', 'noopener'); return; }
      if (onDone) onDone();
      const u = new URL(href, location.href);
      if (u.pathname === location.pathname && u.hash) {
        const tab = u.searchParams.get('tab'), tb = tab && document.getElementById('t-' + tab);
        if (tb) tb.click();
        else if (u.search !== location.search) { location.href = href; return; }
        history.pushState(null, '', u.hash);
        const t = document.querySelector(u.hash); if (t) t.scrollIntoView();
      } else location.href = href;
    }
    render();
    return { input, render };
  }

  /* Ô tìm trong hero (chỉ trang chính) */
  const heroRoot = document.querySelector('.js-palette-hero');
  if (heroRoot) makePalette(heroRoot);

  /* Hộp thoại Ctrl K */
  const dlg = document.createElement('div');
  dlg.className = 'pal-overlay'; dlg.hidden = true;
  dlg.innerHTML = `<div class="pal-win" role="dialog" aria-modal="true" aria-label="Tìm nhanh trong nghiên cứu">
    <div class="pal-bar"><svg class="pal-ico" viewBox="0 0 20 20" aria-hidden="true"><circle cx="9" cy="9" r="6"/><path d="m14 14 4 4"/></svg>
      <input class="pal-input" type="search" name="q" autocomplete="off" spellcheck="false" placeholder="Tìm tài liệu, giả thuyết, phát biểu…" aria-label="Tìm trong nghiên cứu">
      <kbd class="pal-esc">Esc</kbd></div>
    <ul class="pal-list" role="listbox" aria-label="Kết quả"></ul>
    <div class="pal-foot"><span class="pal-count" aria-live="polite"></span><span><kbd>↑</kbd><kbd>↓</kbd> chọn</span><span><kbd>↵</kbd> mở</span><span><kbd>${MOD}</kbd><kbd>↵</kbd> tab mới</span></div>
  </div>`;
  document.body.appendChild(dlg);
  let back = null;
  const pal = makePalette(dlg, close);
  function open() {
    if (!dlg.hidden) return;
    back = document.activeElement; dlg.hidden = false; document.body.classList.add('lock');
    pal.input.value = ''; pal.render(); pal.input.focus();
  }
  function close() {
    if (dlg.hidden) return;
    dlg.hidden = true; document.body.classList.remove('lock');
    if (back && document.contains(back)) back.focus({ preventScroll: true });
  }
  dlg.addEventListener('mousedown', (e) => { if (e.target === dlg) close(); });
  dlg.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') { e.preventDefault(); close(); }
    if (e.key === 'Tab') { e.preventDefault(); pal.input.focus(); }
  });
  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) { e.preventDefault(); dlg.hidden ? open() : close(); }
    else if (e.key === '/' && dlg.hidden && !/INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName) && !document.activeElement.isContentEditable) { e.preventDefault(); open(); }
  });
  document.querySelectorAll('.js-open-palette').forEach((b) => b.addEventListener('click', open));
  document.querySelectorAll('.js-mod').forEach((k) => { k.textContent = MOD; });
})();
