/* MockViewAI — bài tập thở 4–6 (thay quả cầu cũ).
   Hình 3D vẽ trên canvas, cùng phong cách cánh cửa ở hero:
   - Vòng tròn nằm trên mặt sàn = một nhịp 10 giây, chia sẵn 40% hít vào / 60% thở ra.
     Đèn chạy quanh vòng cho biết đang ở đâu trong nhịp và còn bao lâu.
   - Đèn bàn ở giữa: hít vào thì đèn sáng dần, quầng sáng trên sàn rộng ra; thở ra thì dịu xuống.
   Chữ "Hít vào / Thở ra" và số giây nằm DƯỚI hình, trên nền đặc, để luôn đọc rõ.
   Giảm chuyển động: không xoay góc nhìn, vòng vẫn chạy để biết tiến độ. Đọc pha cho trình đọc màn hình.
   Điện thoại hỗ trợ rung: rung nhẹ khi đổi pha, để người nhắm mắt vẫn theo được. */
(() => {
  const canvas = document.getElementById('b-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const $ = (id) => document.getElementById(id);
  const phaseEl = $('b-phase'), secEl = $('b-sec'), live = $('b-live');
  const start = $('b-start'), stop = $('b-stop'), count = $('b-count'), stage = $('b-stage');

  const IN = 4, OUT = 6, CYCLE = IN + OUT, ROUNDS = 6;
  const IDLE_TEXT = `${ROUNDS} nhịp, khoảng 1 phút`;

  /* ---------- Màu lấy từ theme ---------- */
  let tokens = {};
  const readTokens = () => {
    const cs = getComputedStyle(document.documentElement);
    const v = (n) => cs.getPropertyValue('--c-' + n).trim().split(/\s+/).join(',');
    tokens = { primary: v('primary'), primary2: v('primary-2'), hi: v('hi'), muted: v('muted'), accent: v('accent'), bg: v('bg-deep') || v('bg'),
      light: document.documentElement.dataset.theme === 'morning' };
  };
  const c = (name, a) => `rgba(${tokens[name]},${a})`;

  /* ---------- Phép chiếu 3D đơn giản ---------- */
  let W = 0, H = 0, S = 1;
  function resize() {
    const r = canvas.getBoundingClientRect(), dpr = Math.min(devicePixelRatio || 1, 2);
    W = r.width; H = r.height; S = Math.min(W, H);
    canvas.width = Math.round(W * dpr); canvas.height = Math.round(H * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }
  function camera(ay) {
    const ax = 0.42, cY = Math.cos(ay), sY = Math.sin(ay), cX = Math.cos(ax), sX = Math.sin(ax);
    const cx = W / 2, cy = H * 0.6, R = S * 0.58, f = 3.2;
    return (x, y, z) => {
      const x1 = x * cY - z * sY, z1 = x * sY + z * cY;
      const y2 = y * cX - z1 * sX, z2 = y * sX + z1 * cX;
      const k = f / (f + z2);
      return { x: cx + x1 * R * k, y: cy - y2 * R * k, z: z2 };
    };
  }
  const ring = (P, y, r, n = 48) => Array.from({ length: n }, (_, i) => { const a = (i / n) * Math.PI * 2; return P(Math.cos(a) * r, y, Math.sin(a) * r); });
  function hull(pts) {   // bao lồi (monotone chain) để tô khối nón, khối trụ từ các điểm đã chiếu
    const p = pts.slice().sort((a, b) => a.x - b.x || a.y - b.y), cross = (o, a, b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    const lo = [], up = [];
    for (const q of p) { while (lo.length >= 2 && cross(lo[lo.length - 2], lo[lo.length - 1], q) <= 0) lo.pop(); lo.push(q); }
    for (const q of p.reverse()) { while (up.length >= 2 && cross(up[up.length - 2], up[up.length - 1], q) <= 0) up.pop(); up.push(q); }
    return lo.slice(0, -1).concat(up.slice(0, -1));
  }
  const poly = (pts, fill, stroke, lw = 1) => {
    ctx.beginPath(); pts.forEach((q, i) => (i ? ctx.lineTo(q.x, q.y) : ctx.moveTo(q.x, q.y))); ctx.closePath();
    if (fill) { ctx.fillStyle = fill; ctx.fill(); }
    if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = lw; ctx.stroke(); }
  };

  /* ---------- Vẽ một khung hình ----------
     p = vị trí trong nhịp (0..1, null khi chưa bắt đầu), lvl = độ sáng đèn (0..1), t = giây (để xoay nhẹ) */
  function draw(p, lvl, t) {
    ctx.clearRect(0, 0, W, H);
    const P = camera(reduce ? -0.35 : -0.35 + Math.sin(t * 0.12) * 0.08);
    const RR = 1, N = 120, split = IN / CYCLE;

    // Vòng trên sàn: chia đoạn để vẽ nửa sau trước, nửa trước sau đèn
    const seg = [];
    for (let i = 0; i < N; i++) {
      const u0 = i / N, u1 = (i + 1) / N;
      const a0 = -Math.PI / 2 + u0 * Math.PI * 2, a1 = -Math.PI / 2 + u1 * Math.PI * 2;
      const q0 = P(Math.cos(a0) * RR, 0, Math.sin(a0) * RR), q1 = P(Math.cos(a1) * RR, 0, Math.sin(a1) * RR);
      seg.push({ q0, q1, u: u0, z: (q0.z + q1.z) / 2 });
    }
    const drawSeg = (s) => {
      const isIn = s.u < split, done = p !== null && s.u < p;
      ctx.beginPath(); ctx.moveTo(s.q0.x, s.q0.y); ctx.lineTo(s.q1.x, s.q1.y);
      ctx.lineCap = 'round';
      ctx.lineWidth = S * (done ? 0.022 : 0.012);
      ctx.strokeStyle = done ? (isIn ? c('primary', 0.95) : c('accent', 0.95)) : c('muted', isIn ? 0.34 : 0.22);
      ctx.stroke();
    };
    const back = seg.filter((s) => s.z >= 0), front = seg.filter((s) => s.z < 0);

    // Quầng sáng trên sàn (dưới cùng)
    const pool = ring(P, 0, 0.32 + lvl * 0.42, 40);
    poly(pool, c('primary', tokens.light ? 0.06 + lvl * 0.14 : 0.04 + lvl * 0.14));
    back.forEach(drawSeg);

    // Vạch mốc: đầu nhịp và điểm chuyển sang thở ra
    [0, split].forEach((u) => {
      const a = -Math.PI / 2 + u * Math.PI * 2, q0 = P(Math.cos(a) * 0.93, 0, Math.sin(a) * 0.93), q1 = P(Math.cos(a) * 1.07, 0, Math.sin(a) * 1.07);
      ctx.beginPath(); ctx.moveTo(q0.x, q0.y); ctx.lineTo(q1.x, q1.y); ctx.lineWidth = 1.5; ctx.strokeStyle = c('hi', 0.4); ctx.stroke();
    });

    // Chùm sáng từ chao đèn xuống sàn
    const shadeY = 1.02, shadeBot = 0.27, shadeTop = 0.1, shadeH = 0.3;
    const cone = hull(ring(P, shadeY - shadeH, shadeBot * 0.96, 28).concat(ring(P, 0.01, 0.3 + lvl * 0.4, 28)));
    poly(cone, c('primary', tokens.light ? 0.04 + lvl * 0.1 : 0.03 + lvl * 0.1));

    // Đế đèn, thân, chao đèn
    const metal = tokens.light ? c('hi', 0.7) : c('hi', 0.4);
    poly(hull(ring(P, 0, 0.2, 24).concat(ring(P, 0.05, 0.2, 24))), tokens.light ? c('hi', 0.85) : c('bg', 0.95), c('hi', tokens.light ? 0.3 : 0.28));
    poly(ring(P, 0.05, 0.2, 24), tokens.light ? c('hi', 0.7) : c('hi', 0.16));
    const b0 = P(0, 0.05, 0), b1 = P(0, shadeY - shadeH, 0);
    ctx.beginPath(); ctx.moveTo(b0.x, b0.y); ctx.lineTo(b1.x, b1.y); ctx.lineWidth = Math.max(2, S * 0.012); ctx.strokeStyle = metal; ctx.stroke();
    const shade = hull(ring(P, shadeY - shadeH, shadeBot, 32).concat(ring(P, shadeY, shadeTop, 32)));
    // Chao đèn: đặc, sáng lên theo nhịp thở (vẽ một lớp nền đặc trước để chao không trong suốt)
    poly(shade, tokens.light ? c('hi', 0.92) : c('bg', 1));
    poly(shade, c('primary2', tokens.light ? 0.15 + lvl * 0.5 : 0.25 + lvl * 0.7), c('primary', 0.4 + lvl * 0.5), 1.2);

    front.forEach(drawSeg);

    // Đèn chạy trên vòng
    if (p !== null) {
      const a = -Math.PI / 2 + p * Math.PI * 2, q = P(Math.cos(a) * RR, 0, Math.sin(a) * RR);
      ctx.beginPath(); ctx.arc(q.x, q.y, S * 0.05, 0, Math.PI * 2); ctx.fillStyle = c(p < split ? 'primary' : 'accent', 0.22); ctx.fill();
      ctx.beginPath(); ctx.arc(q.x, q.y, S * 0.024, 0, Math.PI * 2); ctx.fillStyle = c('hi', 0.98); ctx.fill();
    }
  }

  /* ---------- Điều khiển nhịp thở ---------- */
  const ease = (x) => 0.5 - Math.cos(Math.PI * x) / 2;
  let running = false, t0 = 0, raf = 0, lastPhase = '', lastSec = -1, idleLvl = 0.3;

  function setPhase(text, sec) {
    phaseEl.textContent = text;
    secEl.textContent = sec === '' ? '' : String(sec);
    stage.dataset.phase = text === 'Hít vào' ? 'in' : text === 'Thở ra' ? 'out' : '';
  }
  function frame(now) {
    const e = Math.max(0, (now - t0) / 1000);   // khung đầu có thể mang mốc thời gian sớm hơn t0
    if (e >= ROUNDS * CYCLE) { finish(); return; }
    const k = e % CYCLE, round = Math.floor(e / CYCLE) + 1, inhale = k < IN;
    const lvl = inhale ? ease(k / IN) : 1 - ease((k - IN) / OUT);
    const phase = inhale ? 'Hít vào' : 'Thở ra', sec = Math.ceil(inhale ? IN - k : CYCLE - k);
    if (phase !== lastPhase) {
      lastPhase = phase;
      live.textContent = `${phase} ${inhale ? IN : OUT} giây`;
      if (navigator.vibrate) navigator.vibrate(inhale ? 40 : [20, 60, 20]);
      count.textContent = `Nhịp ${round} / ${ROUNDS}`;
    }
    if (sec !== lastSec) { lastSec = sec; setPhase(phase, sec); }
    draw(k / CYCLE, lvl, now / 1000);
    raf = requestAnimationFrame(frame);
  }
  function begin() {
    running = true; lastPhase = ''; lastSec = -1;
    stop.disabled = false; stop.focus();      // chuyển focus trước khi khóa nút Bắt đầu, người dùng bàn phím không bị lạc
    start.disabled = true;
    t0 = performance.now(); raf = requestAnimationFrame(frame);
    // Điện thoại: nút Bắt đầu nằm dưới hình, nên đưa hình vào giữa màn hình để người dùng nhìn thấy nhịp
    const r = stage.getBoundingClientRect();
    if (r.top < 64 || r.bottom > innerHeight) stage.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
  }
  function end(phaseText, countText, liveText) {
    running = false; cancelAnimationFrame(raf);
    const hadFocus = document.activeElement === stop;
    start.disabled = false; stop.disabled = true;
    if (hadFocus) start.focus();
    setPhase(phaseText, ''); count.textContent = countText; live.textContent = liveText;
    drawIdle();
  }
  const finish = () => end('Xong rồi', `Hoàn thành ${ROUNDS} nhịp. Bạn có thể làm lại bất cứ lúc nào.`, 'Đã xong 6 nhịp thở');
  start.addEventListener('click', begin);
  stop.addEventListener('click', () => end('Đã dừng', IDLE_TEXT, 'Đã dừng bài thở'));

  /* ---------- Lúc chưa chạy: vẽ một khung tĩnh, vẽ lại khi đổi theme / đổi kích thước ---------- */
  function drawIdle() { if (!running) draw(null, idleLvl, 0); }
  readTokens(); resize(); drawIdle();
  new ResizeObserver(() => { resize(); drawIdle(); }).observe(canvas);
  document.addEventListener('mv:themechange', () => { readTokens(); drawIdle(); });
})();
