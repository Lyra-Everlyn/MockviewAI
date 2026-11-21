/* MockViewAI — nút chọn theme (Đêm trước buổi phỏng vấn · Sáng ngày phỏng vấn · Phòng chờ · Phòng luyện với AI · Sau buổi phỏng vấn).
   Theme đã được đặt sẵn trong <head> (đoạn script ngắn) để trang không nháy màu khi tải;
   file này chỉ vẽ nút + menu, lưu lựa chọn và báo cho phần vẽ 3D (sự kiện "mv:themechange"). */
(() => {
  const KEY = 'mv-theme';
  // Xếp theo hành trình một buổi phỏng vấn: đêm trước → sáng hôm đó → phòng chờ → lúc luyện / phỏng vấn → sau đó
  const THEMES = [
    { id: 'night',    name: 'Đêm trước buổi phỏng vấn', hint: 'Tối, ánh đèn bàn ấm',                 dots: ['21 26 44', '242 179 94', '127 196 181'] },
    { id: 'morning',  name: 'Sáng ngày phỏng vấn',      hint: 'Sáng, dễ đọc ban ngày',               dots: ['238 243 241', '35 120 106', '23 38 46'] },
    { id: 'waiting',  name: 'Phòng chờ',                hint: 'Sáng dịu, xanh chàm, nhịp thở chậm',  dots: ['236 238 248', '72 82 190', '26 28 58'] },
    { id: 'original', name: 'Phòng luyện với AI',       hint: 'Tối xanh rêu, xanh ngọc và san hô',   dots: ['9 22 24', '92 214 172', '255 140 120'] },
    { id: 'dusk',     name: 'Sau buổi phỏng vấn',       hint: 'Hoàng hôn tím, nhìn lại nhẹ nhàng',   dots: ['31 23 42', '244 166 150', '176 158 234'] },
  ];
  const root = document.documentElement;
  const saved = () => { try { return localStorage.getItem(KEY); } catch { return null; } };
  const systemTheme = () => (matchMedia('(prefers-color-scheme: light)').matches ? 'morning' : 'night');
  const valid = (id) => THEMES.some((t) => t.id === id);
  const metaColor = document.querySelector('meta[name="theme-color"]');

  function apply(id, remember) {
    if (!valid(id)) id = 'night';
    root.dataset.theme = id;
    if (remember) { try { localStorage.setItem(KEY, id); } catch { /* trình duyệt chặn lưu */ } }
    if (metaColor) metaColor.content = 'rgb(' + getComputedStyle(root).getPropertyValue('--c-bg').trim().split(/\s+/).join(',') + ')';
    document.querySelectorAll('.mv-theme__item').forEach((b) => b.setAttribute('aria-checked', String(b.dataset.theme === id)));
    document.dispatchEvent(new CustomEvent('mv:themechange', { detail: { theme: id } }));
  }

  // Chưa từng chọn → đi theo cài đặt sáng/tối của máy, kể cả khi người dùng đổi cài đặt lúc đang mở trang
  matchMedia('(prefers-color-scheme: light)').addEventListener('change', () => { if (!valid(saved())) apply(systemTheme(), false); });

  // Chỗ đặt nút: [data-theme-slot] (trang Nghiên cứu) → .auth-area (trang chủ) → .topnav (bài viết)
  const slot = document.querySelector('[data-theme-slot]') || document.querySelector('.auth-area') || document.querySelector('.topnav');
  if (!slot) { apply(root.dataset.theme || systemTheme(), false); return; }

  const el = (tag, props = {}, ...kids) => { const n = Object.assign(document.createElement(tag), props); kids.forEach((k) => n.append(k)); return n; };
  const svg = (markup) => { const s = el('span', { className: 'mv-theme__icon' }); s.innerHTML = markup; return s; };

  const btn = el('button', { type: 'button', className: 'mv-theme__btn', title: 'Đổi giao diện' },
    svg('<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8.5" fill="none" stroke="currentColor" stroke-width="1.8"/><path d="M12 3.5a8.5 8.5 0 0 1 0 17z" fill="currentColor"/></svg>'),
    el('span', { className: 'mv-theme__label', textContent: 'Giao diện' }));
  btn.setAttribute('aria-haspopup', 'menu');
  btn.setAttribute('aria-expanded', 'false');
  btn.setAttribute('aria-controls', 'mv-theme-menu');

  const menu = el('div', { id: 'mv-theme-menu', className: 'mv-theme__menu', hidden: true });
  menu.setAttribute('role', 'menu');
  menu.setAttribute('aria-label', 'Chọn giao diện');
  THEMES.forEach((t) => {
    const dots = el('span', { className: 'mv-theme__dots' });
    t.dots.forEach((c) => dots.append(el('i', { style: `background:rgb(${c})` })));
    const item = el('button', { type: 'button', className: 'mv-theme__item' }, dots,
      el('span', {}, el('b', { textContent: t.name }), el('small', { textContent: t.hint })));
    item.dataset.theme = t.id;
    item.setAttribute('role', 'menuitemradio');
    item.addEventListener('click', () => { apply(t.id, true); open(false); btn.focus(); });
    menu.append(item);
  });

  const wrap = el('div', { className: 'mv-theme' }, btn, menu);
  slot.prepend(wrap);

  // Điện thoại / máy tính bảng: thanh trên chật → đặt lựa chọn giao diện trong menu ba gạch (#nav, trang chủ)
  // hoặc bảng Mục lục (#toc, trang Nghiên cứu)
  const nav = document.getElementById('nav') || document.getElementById('toc');
  if (nav) {
    const group = el('div', { className: 'mv-theme-inline' }, el('p', { textContent: 'Giao diện', id: 'mv-theme-inline-label' }));
    group.setAttribute('role', 'radiogroup');
    group.setAttribute('aria-labelledby', 'mv-theme-inline-label');
    THEMES.forEach((t) => {
      const dots = el('span', { className: 'mv-theme__dots' });
      t.dots.forEach((c) => dots.append(el('i', { style: `background:rgb(${c})` })));
      const item = el('button', { type: 'button', className: 'mv-theme__item' }, dots,
        el('span', {}, el('b', { textContent: t.name }), el('small', { textContent: t.hint })));
      item.dataset.theme = t.id;
      item.setAttribute('role', 'radio');
      item.addEventListener('click', () => apply(t.id, true));
      group.append(item);
    });
    nav.append(group);
  }

  const items = () => [...menu.querySelectorAll('[role="menuitemradio"]')];
  function open(on) {
    menu.hidden = !on;
    btn.setAttribute('aria-expanded', String(on));
    if (on) (items().find((b) => b.getAttribute('aria-checked') === 'true') || items()[0]).focus();
  }
  btn.addEventListener('click', () => open(menu.hidden));
  document.addEventListener('click', (e) => { if (!wrap.contains(e.target)) open(false); });
  wrap.addEventListener('focusout', (e) => { if (!wrap.contains(e.relatedTarget)) open(false); });
  wrap.addEventListener('keydown', (e) => {
    if (menu.hidden) return;
    const list = items(), i = list.indexOf(document.activeElement);
    if (e.key === 'Escape') { open(false); btn.focus(); }
    else if (e.key === 'ArrowDown') { e.preventDefault(); list[(i + 1) % list.length].focus(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); list[(i - 1 + list.length) % list.length].focus(); }
  });

  apply(root.dataset.theme || (valid(saved()) ? saved() : systemTheme()), false);
})();
