/* MockViewAI — ảnh đại diện trên landing khi người dùng đã đăng nhập ứng dụng.
   Landing và ứng dụng ở hai tên miền khác nhau nên landing không đọc được cookie đăng nhập.
   Ứng dụng báo trạng thái qua phần # của URL mỗi lần chuyển hướng về đây (xem LandingAuthHandoff.cs):
     #mv-auth=<base64url JSON {n: tên, a: ảnh https|null, k: mã tài khoản, e: hết hạn (Unix giây)}>[&to=<neo>]
     #mv-auth=out[&to=<neo>]   ← vừa đăng xuất
   Landing lưu vào localStorage CHỈ ĐỂ HIỂN THỊ (không phải token, không mở được tài khoản), rồi xoá # khỏi thanh địa chỉ.
   Menu ảnh đại diện: Luyện phỏng vấn · Hồ sơ · Đăng xuất (đều mở trang của ứng dụng). */
(() => {
  const KEY = 'mv-auth';
  const PROD_APP = 'https://mockviewai.com';
  const LOCAL_APP = 'http://localhost:9090';          // Visual Studio, profile "http"
  const isLocal = location.hostname === '127.0.0.1' || location.hostname === 'localhost';
  const APP = isLocal ? LOCAL_APP : PROD_APP;

  const store = {
    get() { try { return JSON.parse(localStorage.getItem(KEY) || 'null'); } catch { return null; } },
    set(v) { try { localStorage.setItem(KEY, JSON.stringify(v)); } catch { /* trình duyệt chặn lưu trữ */ } },
    clear() { try { localStorage.removeItem(KEY); } catch { /* như trên */ } },
  };

  const decode = (b64url) => {
    const b64 = b64url.replace(/-/g, '+').replace(/_/g, '/');
    const bin = atob(b64 + '='.repeat((4 - (b64.length % 4)) % 4));
    return JSON.parse(new TextDecoder().decode(Uint8Array.from(bin, (c) => c.charCodeAt(0))));
  };

  // ---------- 1. Nhận tín hiệu từ ứng dụng ----------
  const m = location.hash.match(/^#mv-auth=([^&]*)(?:&to=(.*))?$/);
  if (m) {
    if (m[1] === 'out') {
      store.clear();
    } else {
      try {
        const d = decode(m[1]);
        if (d && typeof d.e === 'number') {
          store.set({
            name: typeof d.n === 'string' ? d.n.slice(0, 80) : '',
            avatar: typeof d.a === 'string' && d.a.startsWith('https://') ? d.a : null,
            key: typeof d.k === 'string' && /^[a-f0-9]{16,64}$/.test(d.k) ? d.k : null,
            exp: d.e,
          });
        }
      } catch { /* chuỗi hỏng → bỏ qua, giữ trạng thái cũ */ }
    }
    let anchor = '';
    try { anchor = m[2] ? decodeURIComponent(m[2]) : ''; } catch { anchor = ''; }
    history.replaceState(null, '', location.pathname + location.search + (anchor ? '#' + anchor : ''));
    if (anchor) {
      const target = document.getElementById(anchor);
      if (target) requestAnimationFrame(() => target.scrollIntoView());
    }
  }

  // ---------- 2. Hiện ảnh đại diện nếu đang đăng nhập ----------
  const user = store.get();
  // Lớp "mv-auth-in" trên <html>: CSS ẩn chữ dành cho khách ([data-auth="guest"]) và hiện chữ dành cho người đã đăng nhập ([data-auth="user"])
  if (!user || !(user.exp * 1000 > Date.now())) { if (user) store.clear(); document.documentElement.classList.remove('mv-auth-in'); return; }
  document.documentElement.classList.add('mv-auth-in');

  // Cho app.js biết để mở khoá checklist (chạy trước DOMContentLoaded, nên app.js đọc được ngay).
  window.mvAppUser = Object.freeze({ name: user.name || '', key: user.key || null });

  const slot = document.querySelector('.auth-area') || document.querySelector('.topnav');
  if (!slot) return;

  const el = (tag, props = {}, ...children) => {
    const node = Object.assign(document.createElement(tag), props);
    children.forEach((c) => node.append(c));
    return node;
  };

  const name = user.name || 'Tài khoản của bạn';
  const initial = (user.name || '?').trim().charAt(0).toUpperCase() || '?';

  const btn = el('button', { type: 'button', className: 'mv-user__btn', title: name });
  btn.setAttribute('aria-haspopup', 'menu');
  btn.setAttribute('aria-expanded', 'false');
  btn.setAttribute('aria-controls', 'mv-user-menu');
  btn.setAttribute('aria-label', 'Tài khoản: ' + name);
  const showInitial = () => { btn.replaceChildren(el('span', { textContent: initial })); };
  if (user.avatar) {
    const img = el('img', { src: user.avatar, alt: '', referrerPolicy: 'no-referrer' });
    img.addEventListener('error', showInitial, { once: true });
    btn.append(img);
  } else {
    showInitial();
  }

  const here = location.href.split('#')[0];
  const link = (text, path, extra = '') => {
    const a = el('a', { href: APP + path, textContent: text, className: extra });
    a.setAttribute('role', 'menuitem');
    return a;
  };
  const menu = el('div', { id: 'mv-user-menu', className: 'mv-user__menu', hidden: true },
    el('p', { className: 'mv-user__who' }, 'Đang đăng nhập', el('strong', { textContent: name })),
    link('Luyện phỏng vấn', '/Home/Index'),
    link('Hồ sơ', '/Profile'),
    link('Đăng xuất', '/Auth/Logout?returnUrl=' + encodeURIComponent(here), 'mv-user__out'),
  );
  menu.setAttribute('role', 'menu');
  menu.setAttribute('aria-label', 'Tài khoản');

  const wrap = el('div', { className: 'mv-user' }, btn, menu);
  slot.classList.add('mv-signed-in');   // CSS ẩn nút "Đăng nhập" (#auth-open)
  slot.append(wrap);

  const open = (on) => {
    menu.hidden = !on;
    btn.setAttribute('aria-expanded', String(on));
    if (on) menu.querySelector('a').focus();
  };
  btn.addEventListener('click', () => open(menu.hidden));
  document.addEventListener('click', (e) => { if (!wrap.contains(e.target)) open(false); });
  wrap.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !menu.hidden) { open(false); btn.focus(); }
  });
  wrap.addEventListener('focusout', (e) => { if (!wrap.contains(e.relatedTarget)) open(false); });
})();
