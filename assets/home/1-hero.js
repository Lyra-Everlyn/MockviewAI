/* MockViewAI — Đợt 1: quả cầu 3D, khối giao diện nghiêng, thẻ nghiêng, hiện dần khi cuộn, menu, đồng hồ hero */
(() => {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const fine = matchMedia('(pointer: fine)').matches;

  /* Màu lấy từ theme (assets/themes.css): đổi theme thì quả cầu đổi màu theo, không cần sửa code vẽ */
  const tokenCache = new Map();
  const themeColor = (role, alpha) => {
    let rgb = tokenCache.get(role);
    if (rgb === undefined) {
      rgb = getComputedStyle(document.documentElement).getPropertyValue('--c-' + role).trim() || '255 255 255';
      tokenCache.set(role, rgb);
    }
    return `rgb(${rgb} / ${alpha})`;
  };
  document.addEventListener('mv:themechange', () => tokenCache.clear());

  /* ---------- 1. Quả cầu hạt 3D ở hero (canvas 2D + phép chiếu phối cảnh) ---------- */
  const cv = document.getElementById('orb-canvas');
  const ctx = cv.getContext('2d');
  const N = 520, pts = [], links = [];
  for (let i = 0; i < N; i++) {                       // phân bố đều trên mặt cầu (Fibonacci)
    const y = 1 - (i / (N - 1)) * 2, r = Math.sqrt(1 - y * y), th = i * 2.399963;
    pts.push([Math.cos(th) * r, y, Math.sin(th) * r]);
  }
  for (let i = 0; i < N; i++) for (let j = i + 1; j < N; j++) {   // nối các điểm gần nhau
    const a = pts[i], b = pts[j], d = (a[0]-b[0])**2 + (a[1]-b[1])**2 + (a[2]-b[2])**2;
    if (d < 0.013) links.push([i, j]);
  }
  const ring = Array.from({ length: 140 }, (_, i) => i / 140 * Math.PI * 2);
  let W = 0, H = 0, dpr = 1, rotY = 0, mx = 0, my = 0, tx = 0, ty = 0, visible = true;

  /* Mỗi theme một hình 3D: night = cánh cửa, morning = sợi chỉ, waiting = hàng ghế chờ, dusk = mặt hồ (assets/home/1-hero-models.js);
     theme còn lại ("Phòng luyện với AI") giữ quả cầu hạt bên dưới. */
  const models = window.MVHeroModels || {};
  let theme = document.documentElement.dataset.theme;
  // Khi giảm chuyển động: không chạy vòng lặp, chỉ vẽ lại một lần lúc có thay đổi
  let drawPending = false;
  const requestDraw = () => { if (reduce && !drawPending) { drawPending = true; requestAnimationFrame((n) => { drawPending = false; frame(n); }); } };
  document.addEventListener('mv:themechange', () => { theme = document.documentElement.dataset.theme; requestDraw(); });

  // Tiến độ checklist (app.js ghi vào aria-valuenow của thanh tiến độ): cửa mở rộng hơn, sợi chỉ thẳng hơn
  let prog = 0, progTarget = 0, done = 0;
  const clBar = document.getElementById('checklist-bar');
  const readProgress = () => {
    progTarget = Math.max(0, Math.min(1, (Number(clBar.getAttribute('aria-valuenow')) || 0) / 100));
    done = document.querySelectorAll('#checklist-items .checklist-item.is-done').length;
    if (reduce) prog = progTarget;
    requestDraw();
  };
  if (clBar) { new MutationObserver(readProgress).observe(clBar, { attributes: true, attributeFilter: ['aria-valuenow'] }); readProgress(); }
  // Khối giao diện mẫu trong hero: trên điện thoại, quả cầu nằm sau khối này để không đè lên chữ
  const slot = document.getElementById('stage3d');
  let slotY = 0, slotH = 0;
  function size() {
    dpr = Math.min(devicePixelRatio || 1, 2);
    W = cv.clientWidth; H = cv.clientHeight;
    cv.width = W * dpr; cv.height = H * dpr; ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    if (slot) {
      const c = cv.getBoundingClientRect(), r = slot.getBoundingClientRect();
      slotY = r.top - c.top + r.height / 2; slotH = r.height;
    }
    requestDraw();   // đổi kích thước canvas sẽ xoá hình: vẽ lại (cần khi giảm chuyển động)
  }
  function project(x, y, z, R, cx, cy) {
    const f = 3.2, s = f / (f + z);                   // chiếu phối cảnh
    return [cx + x * R * s, cy + y * R * s, s];
  }
  function frame(now) {
    if (!visible) { if (!reduce) requestAnimationFrame(frame); return; }
    ctx.clearRect(0, 0, W, H);
    const wide = W > 900;
    const narrowSlot = !wide && slotH > 0;
    const R = wide ? Math.min(W * .2, 260) : Math.min(W * .42, 200, narrowSlot ? slotH * .5 : 200);
    const cx = wide ? W * .73 : W * .5, cy = wide ? H * .47 : (narrowSlot ? slotY : H * .78);
    tx += (mx - tx) * .05; ty += (my - ty) * .05;
    prog += (progTarget - prog) * .05;
    const model = models[theme];
    if (model) {
      model.draw({ ctx, W, H, R, cx, cy, t: (now || 0) / 1000, tx, ty, color: themeColor, progress: prog, done, reduce });
      if (!reduce) requestAnimationFrame(frame);
      return;
    }
    if (!reduce) rotY += .0022;
    const ay = rotY + tx * .5, ax = -.35 + ty * .3;
    const cY = Math.cos(ay), sY = Math.sin(ay), cX = Math.cos(ax), sX = Math.sin(ax);
    const P = pts.map(([x, y, z]) => {
      const x1 = x * cY - z * sY, z1 = x * sY + z * cY;
      const y2 = y * cX - z1 * sX, z2 = y * sX + z1 * cX;
      return project(x1, y2, z2, R, cx, cy).concat(z2);
    });
    ctx.lineWidth = .6;
    for (const [i, j] of links) {
      const a = P[i], b = P[j], depth = (2 - a[3] - b[3]) / 4;
      ctx.strokeStyle = themeColor('accent', .04 + depth * .22);
      ctx.beginPath(); ctx.moveTo(a[0], a[1]); ctx.lineTo(b[0], b[1]); ctx.stroke();
    }
    for (const p of P) {
      const front = (1 - p[3]) / 2;                   // 0 = sau, 1 = trước
      ctx.fillStyle = front > .5 ? themeColor('primary-2', .35 + front * .6) : themeColor('violet-2', .15 + front * .5);
      ctx.beginPath(); ctx.arc(p[0], p[1], .6 + front * 1.7 * p[2], 0, 6.283); ctx.fill();
    }
    // vành quỹ đạo nghiêng
    ctx.beginPath();
    ring.forEach((t, k) => {
      const x = Math.cos(t + rotY * 2) * 1.45, z = Math.sin(t + rotY * 2) * 1.45, y = 0;
      const y2 = y * Math.cos(1.2) - z * Math.sin(1.2), z2 = y * Math.sin(1.2) + z * Math.cos(1.2);
      const [px, py] = project(x, y2 + .05, z2, R, cx, cy);
      k ? ctx.lineTo(px, py) : ctx.moveTo(px, py);
    });
    ctx.closePath(); ctx.strokeStyle = themeColor('primary', .22); ctx.lineWidth = 1; ctx.stroke();
    if (!reduce) requestAnimationFrame(frame);
  }
  size(); addEventListener('resize', size);
  // Chiều cao hero đổi khi font tải xong hoặc chữ xuống dòng khác: đo lại vị trí
  if (window.ResizeObserver) new ResizeObserver(size).observe(cv);
  new IntersectionObserver(([e]) => { visible = e.isIntersecting; }).observe(cv);
  if (fine) addEventListener('pointermove', (e) => { mx = e.clientX / innerWidth - .5; my = e.clientY / innerHeight - .5; });
  requestAnimationFrame(frame);

  /* ---------- 2. Ảnh sản phẩm xếp lớp: nghiêng theo chuột ---------- */
  const stage = document.getElementById('stage3d'), stack = document.getElementById('stack');
  if (stage && stack && fine && !reduce) {
    stage.addEventListener('pointermove', (e) => {
      const r = stage.getBoundingClientRect(), x = (e.clientX - r.left) / r.width - .5, y = (e.clientY - r.top) / r.height - .5;
      stack.style.transform = `rotateX(${14 - y * 12}deg) rotateY(${-20 + x * 18}deg) rotateZ(2deg)`;
    });
    stage.addEventListener('pointerleave', () => { stack.style.transform = ''; });
  }

  /* ---------- 3. Thẻ nghiêng 3D ---------- */
  if (fine && !reduce) document.querySelectorAll('[data-tilt]').forEach((el) => {
    el.addEventListener('pointermove', (e) => {
      const r = el.getBoundingClientRect();
      el.style.setProperty('--ry', ((e.clientX - r.left) / r.width - .5) * 10 + 'deg');
      el.style.setProperty('--rx', -((e.clientY - r.top) / r.height - .5) * 10 + 'deg');
    });
    el.addEventListener('pointerleave', () => { el.style.setProperty('--rx', '0deg'); el.style.setProperty('--ry', '0deg'); });
  });

  /* ---------- 4. Hiện dần + đếm số + thanh đo ---------- */
  const fmt = (v, dec) => v.toFixed(dec).replace('.', ',');
  function countUp(el) {
    const end = parseFloat(el.dataset.count), dec = el.dataset.count.includes('.') ? 1 : 0, suf = el.dataset.suffix || '';
    if (reduce) { el.textContent = fmt(end, dec) + suf; return; }
    const t0 = performance.now();
    (function step(t) { const k = Math.min((t - t0) / 1400, 1), e = 1 - Math.pow(1 - k, 3);
      el.textContent = fmt(end * e, dec) + suf; if (k < 1) requestAnimationFrame(step); })(t0);
  }
  const fillMeters = (root) => root.querySelectorAll('.meter-fill').forEach((m) => { m.style.width = m.dataset.w + '%'; });
  const io = new IntersectionObserver((es) => es.forEach((e) => {
    if (!e.isIntersecting) return;
    e.target.classList.add('in');
    e.target.querySelectorAll('[data-count]').forEach(countUp);
    fillMeters(e.target);
    io.unobserve(e.target);
  }), { threshold: .15 });
  document.querySelectorAll('.reveal').forEach((el) => io.observe(el));

  /* ---------- 7. Menu điện thoại + mục đang xem + nút cố định đáy ---------- */
  const nav = document.getElementById('nav'), mb = document.getElementById('menu-btn');
  mb.addEventListener('click', () => { const o = nav.classList.toggle('open'); mb.setAttribute('aria-expanded', o); });
  nav.addEventListener('click', (e) => { if (e.target.tagName === 'A') { nav.classList.remove('open'); mb.setAttribute('aria-expanded', false); } });
  const navLinks = [...nav.querySelectorAll('a[href^="#"]')];
  const spy = new IntersectionObserver((es) => es.forEach((e) => {
    if (e.isIntersecting) navLinks.forEach((a) => a.classList.toggle('on', a.getAttribute('href') === '#' + e.target.id));
  }), { rootMargin: '-45% 0px -50% 0px' });
  // Theo dõi mọi section có id: khi đang ở mục không có trong menu thì không gạch chân mục nào
  document.querySelectorAll('section[id]').forEach((s) => spy.observe(s));

  /* ---------- 8. Giao diện sản phẩm dựng bằng code ---------- */
  const mmss = (n) => `${Math.floor(n / 60)}:${String(n % 60).padStart(2, '0')}`;
  // Thanh điểm ở thẻ kết quả: chạy khi thẻ hiện ra
  const barObs = new IntersectionObserver((es) => es.forEach((e) => {
    if (!e.isIntersecting) return;
    e.target.querySelectorAll('.ui-bar i').forEach((b) => { b.style.width = b.dataset.w + '%'; });
    barObs.unobserve(e.target);
  }), { threshold: .3 });
  document.querySelectorAll('.ui-res').forEach((el) => barObs.observe(el));

  // Đồng hồ trong khối 3D ở đầu trang (chỉ trang trí)
  const hT = document.getElementById('hero-timer'), hB = document.getElementById('hero-track');
  let hs = 15;
  const heroTick = () => { hT.textContent = mmss(hs); hB.style.width = (hs / 15 * 100) + '%'; hs = hs <= 1 ? 15 : hs - 1; };
  if (hT && hB) { heroTick(); if (!reduce) setInterval(heroTick, 1000); }

})();
