/* MockViewAI — hình 3D riêng cho từng theme ở hero (vẽ trên cùng canvas với quả cầu, xem 1-hero.js).
   - "Đêm trước buổi phỏng vấn" (night): cánh cửa hé mở, ánh đèn ấm hắt ra, 5 viên đá dẫn tới cửa.
       Mỗi việc đã tích trong checklist làm sáng một viên đá và cửa mở rộng thêm.
   - "Sáng ngày phỏng vấn" (morning): sợi chỉ rối dần duỗi ra.
       Checklist càng xong nhiều, sợi chỉ càng thẳng; sợi chỉ "thở" theo nhịp 4 giây hít vào, 6 giây thở ra như bài thở.
   - "Phòng chờ" (waiting): hàng 5 ghế chờ; đốm sáng "bạn" tiến lên một ghế sau mỗi việc đã tích, quầng sáng thở theo nhịp 4–6.
   - "Sau buổi phỏng vấn" (dusk): mặt hồ gợn sóng, sóng lặng dần; checklist càng xong nhiều, mặt hồ càng êm.
   - "Phòng luyện với AI" (original): không có ở đây, 1-hero.js vẽ quả cầu hạt như cũ.
   Màu lấy từ theme qua S.color(vai-trò, độ-đục), nên không có mã màu cứng nào trong file này.

   Mỗi model nhận S = { ctx, W, H, R, cx, cy, t (giây), tx, ty (vị trí chuột -0.5..0.5),
                         color, progress (0..1, đã làm mượt), done (số việc đã tích), reduce } */
window.MVHeroModels = (() => {
  const TAU = Math.PI * 2;

  // Xoay quanh trục Y (ay) rồi trục X (ax), sau đó chiếu phối cảnh như quả cầu.
  function camera(ay, ax, R, cx, cy) {
    const cY = Math.cos(ay), sY = Math.sin(ay), cX = Math.cos(ax), sX = Math.sin(ax);
    return (x, y, z) => {
      const x1 = x * cY - z * sY, z1 = x * sY + z * cY;
      const y2 = y * cX - z1 * sX, z2 = y * sX + z1 * cX;
      const s = 3.2 / (3.2 + z2);
      return [cx + x1 * R * s, cy + y2 * R * s, s, z2];
    };
  }

  const poly = (ctx, pts) => {
    ctx.beginPath();
    pts.forEach((p, i) => (i ? ctx.lineTo(p[0], p[1]) : ctx.moveTo(p[0], p[1])));
    ctx.closePath();
  };

  // Số giả ngẫu nhiên cố định (hạt bụi luôn ở cùng chỗ mỗi lần tải trang)
  const rand = (i) => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };

  /* ================= Đêm: cánh cửa và lối đi 5 bước ================= */
  // Cửa cao hơn khối giao diện mẫu để ánh sáng lộ ra phía trên và hai bên; lối đi nằm dưới khối đó
  const DOOR = { x0: -0.58, x1: 0.58, top: -1.6, base: 0.75, z: 0.6 };
  const STEPS = Array.from({ length: 5 }, (_, i) => ({        // i = 0 gần người xem nhất (việc đầu tiên)
    x: Math.sin(i * 1.15 + 0.4) * 0.18 + 0.05,
    z: -0.95 + i * 0.33
  }));
  const MOTES = Array.from({ length: 28 }, (_, i) => ({ x: rand(i) - 0.5, z: rand(i + 50), sp: 0.04 + rand(i + 99) * 0.06, ph: rand(i + 7) }));

  function night(S) {
    const { ctx, R, cx, cy, t, tx, ty, color, progress, done, reduce } = S;
    const sway = reduce ? 0 : Math.sin(t * 0.25) * 0.05;
    const P = camera(-0.42 + tx * 0.35 + sway, 0.3 + ty * 0.12, R, cx, cy);
    const { x0, x1, top, base, z } = DOOR;
    const open = 0.3 + 0.95 * progress;                       // góc mở cửa (rad): hé 17° → gần 72°

    // 1. Ánh sáng hắt xuống sàn (từ khe cửa ra phía người xem)
    const spill = [P(x0 + 0.05, base, z), P(x1, base, z), P(x1 + 0.45, base, -1.2), P(x0 - 0.25, base, -1.2)];
    const gS = ctx.createLinearGradient(0, spill[0][1], 0, spill[2][1]);
    gS.addColorStop(0, color('primary', 0.28 + progress * 0.12));
    gS.addColorStop(1, color('primary', 0));
    ctx.save(); ctx.globalCompositeOperation = 'lighter';
    poly(ctx, spill); ctx.fillStyle = gS; ctx.fill();
    ctx.restore();

    // 2. Lối đi: 5 viên đá, vẽ từ xa tới gần
    for (let i = STEPS.length - 1; i >= 0; i--) {
      const st = STEPS[i], w = 0.17, d = 0.1;
      const q = [P(st.x - w, base, st.z - d), P(st.x + w, base, st.z - d), P(st.x + w, base, st.z + d), P(st.x - w, base, st.z + d)];
      const lit = i < done, next = i === done;
      poly(ctx, q);
      if (lit) {
        ctx.save(); ctx.shadowColor = color('primary', 0.9); ctx.shadowBlur = 18 * q[0][2];
        ctx.fillStyle = color('primary', 0.75); ctx.fill(); ctx.restore();
        ctx.strokeStyle = color('primary-3', 0.9);
      } else {
        const pulse = next && !reduce ? 0.18 + 0.14 * (0.5 + 0.5 * Math.sin(t * 2.2)) : 0.14;
        ctx.fillStyle = color('surface-6', 0.55); ctx.fill();
        ctx.strokeStyle = color('primary-2', next ? pulse + 0.25 : 0.22);
      }
      ctx.lineWidth = 1.2 * q[0][2]; ctx.stroke();
    }

    // 3. Khung cửa và ánh đèn bên trong
    const fr = [P(x0, top, z), P(x1, top, z), P(x1, base, z), P(x0, base, z)];
    const gL = ctx.createLinearGradient(fr[0][0], fr[0][1], fr[2][0], fr[2][1]);
    gL.addColorStop(0, color('primary-3', 0.85));
    gL.addColorStop(0.55, color('primary', 0.6));
    gL.addColorStop(1, color('primary-2', 0.35));
    ctx.save(); ctx.shadowColor = color('primary', 0.8); ctx.shadowBlur = 50;
    poly(ctx, fr); ctx.fillStyle = gL; ctx.fill(); ctx.restore();

    // 4. Hạt bụi bay chậm trong luồng sáng (đứng yên khi người dùng chọn giảm chuyển động)
    for (const m of MOTES) {
      const life = reduce ? m.ph : (m.ph + t * m.sp) % 1;
      const p = P(m.x * 0.9 + 0.1, base - life * 1.7, z - 0.15 - m.z * 1.2);
      const a = Math.sin(life * Math.PI) * 0.55;
      ctx.fillStyle = color('primary-3', a);
      ctx.beginPath(); ctx.arc(p[0], p[1], 1.1 * p[2], 0, TAU); ctx.fill();
    }

    // 5. Cánh cửa: bản lề bên trái, mở về phía người xem
    const ex = x0 + Math.cos(open), ez = z - Math.sin(open);
    const leaf = [P(x0, top, z), P(ex, top, ez), P(ex, base, ez), P(x0, base, z)];
    poly(ctx, leaf);
    const gD = ctx.createLinearGradient(leaf[0][0], 0, leaf[1][0], 0);
    gD.addColorStop(0, color('surface-2', 0.96));
    gD.addColorStop(1, color('surface-7', 0.96));
    ctx.fillStyle = gD; ctx.fill();
    ctx.strokeStyle = color('primary-2', 0.35); ctx.lineWidth = 1; ctx.stroke();
    // hai ô pa-nô trên cánh cửa
    [[-1.45, -0.45], [-0.3, 0.62]].forEach(([y0, y1]) => {
      const a = 0.16, b = 0.84, at = (k) => [x0 + Math.cos(open) * k, z - Math.sin(open) * k];
      const [ax_, az] = at(a), [bx, bz] = at(b);
      poly(ctx, [P(ax_, y0, az), P(bx, y0, bz), P(bx, y1, bz), P(ax_, y1, az)]);
      ctx.strokeStyle = color('primary-3', 0.16); ctx.stroke();
    });
    const knob = P(x0 + Math.cos(open) * 0.88, -0.38, z - Math.sin(open) * 0.88);
    ctx.fillStyle = color('primary-2', 0.9);
    ctx.beginPath(); ctx.arc(knob[0], knob[1], 3.2 * knob[2], 0, TAU); ctx.fill();

    // 6. Viền khung cửa (vẽ sau cùng cho rõ nét)
    ctx.strokeStyle = color('primary-3', 0.7); ctx.lineWidth = 2.2;
    ctx.beginPath(); ctx.moveTo(fr[3][0], fr[3][1]); ctx.lineTo(fr[0][0], fr[0][1]); ctx.lineTo(fr[1][0], fr[1][1]); ctx.lineTo(fr[2][0], fr[2][1]); ctx.stroke();
  }

  /* ================= Sáng: sợi chỉ rối duỗi dần ================= */
  const N = 420;
  let phase = 0, last = null, intro = null;
  const smooth = (k) => k * k * (3 - 2 * k);
  // Nhịp thở 10 giây: 4 giây hít vào (0 → 1), 6 giây thở ra (1 → 0)
  const breath = (t) => { const p = t % 10; return p < 4 ? smooth(p / 4) : smooth(1 - (p - 4) / 6); };

  function morning(S) {
    const { ctx, W, R, cx, cy, t, tx, ty, color, progress, reduce } = S;
    const wide = W > 900;
    if (intro === null) intro = t;
    const dt = last === null ? 0 : Math.min(t - last, 0.1); last = t;

    // Mức "duỗi": 0 = rối hẳn, 1 = thẳng. Lúc mở trang sợi chỉ tự gỡ rối trong khoảng 2,5 giây.
    let level = 0.3 + 0.7 * progress;
    if (!reduce) level = Math.min(1, level * smooth(Math.min(1, (t - intro) / 2.5)) + 0.06 * breath(t));
    const k = 1 - smooth(Math.max(0, Math.min(1, level)));     // độ rối
    if (!reduce) phase += dt * (0.25 + 0.6 * k);
    // Khối giao diện mẫu che giữa hero: máy tính đặt sợi chỉ ở dải ngay dưới khối, điện thoại đặt ngay trên khối
    const oy = wide ? 0.86 : -1.5;
    const P = camera(Math.sin(phase) * 0.5 * k + tx * 0.4, -0.18 + ty * 0.25, R, cx, cy + oy * R);
    const half = Math.min(1.45, (W * 0.47) / R);               // nửa chiều dài khi duỗi thẳng
    const flat = wide ? 0.3 : 0.26;                            // nén chiều cao cho vừa dải trống

    const pts = new Array(N);
    for (let i = 0; i < N; i++) {
      const u = i / (N - 1), a = u * TAU, w = reduce ? 0 : t;
      // dáng thẳng: một đường lượn nhẹ
      const sx = -half + 2 * half * u, sy = 0.1 * Math.sin(TAU * 1.2 * u + w * 0.6), sz = 0.22 * Math.cos(TAU * 0.8 * u + w * 0.4);
      // dáng rối: chồng nhiều vòng sóng lên nhau
      const kx = (0.55 * Math.sin(2 * a) + 0.42 * Math.sin(5 * a + 0.7) + 0.22 * Math.cos(9 * a)) * half / 1.15;
      const ky = (0.55 * Math.cos(3 * a) + 0.35 * Math.sin(7 * a + 1.3)) * flat;
      const kz = 0.5 * Math.sin(4 * a + 0.4) + 0.3 * Math.cos(6 * a);
      pts[i] = P(sx + (kx - sx) * k, sy + (ky - sy) * k, sz + (kz - sz) * k);
    }

    // bóng mờ dưới sợi chỉ khi đã thẳng: cảm giác nằm yên
    if (k < 0.6) {
      const gy = cy + (oy + 0.25) * R;
      const g = ctx.createRadialGradient(cx, gy, 0, cx, gy, R * half);
      g.addColorStop(0, color('primary', 0.12 * (1 - k / 0.6)));
      g.addColorStop(1, color('primary', 0));
      ctx.fillStyle = g; ctx.fillRect(cx - R * half, gy - R * 0.5, R * half * 2, R);
    }

    ctx.lineCap = 'round';
    for (let i = 1; i < N; i++) {
      const a = pts[i - 1], b = pts[i];
      const front = Math.max(0, Math.min(1, (1 - (a[3] + b[3]) / 2) / 2));   // 0 = sau, 1 = trước
      ctx.strokeStyle = front > 0.5 ? color('primary', 0.45 + front * 0.5) : color('accent', 0.25 + front * 0.45);
      ctx.lineWidth = (1 + front * 2.2) * a[2];
      ctx.beginPath(); ctx.moveTo(a[0], a[1]); ctx.lineTo(b[0], b[1]); ctx.stroke();
    }
    // đầu sợi chỉ: một hạt cườm nhỏ
    const end = pts[N - 1];
    ctx.fillStyle = color('primary', 0.95);
    ctx.beginPath(); ctx.arc(end[0], end[1], 4 * end[2], 0, TAU); ctx.fill();
    ctx.strokeStyle = color('surface', 0.9); ctx.lineWidth = 1.5; ctx.stroke();
  }

  /* ================= Phòng chờ: hàng ghế chờ ================= */
  // 5 ghế = 5 việc trong checklist. Đốm sáng "bạn" ngồi ở ghế ứng với số việc đã xong và tiến dần lên;
  // xong cả 5 thì đốm sáng rời ghế cuối, sẵn sàng vào phòng. Quầng sáng quanh đốm "thở" theo nhịp 4–6.
  const CHAIRS = 5, GAP = 0.5;
  const chairX = (i) => (i - (CHAIRS - 1) / 2) * GAP;
  let seat = null;                                               // vị trí hiện tại của đốm sáng (làm mượt)

  function waiting(S) {
    const { ctx, W, R, cx, cy, t, tx, ty, color, done, reduce } = S;
    const wide = W > 900;
    const oy = wide ? 0.82 : -1.5;                               // máy tính: dưới khối giao diện mẫu; điện thoại: phía trên
    // thu nhỏ cho cả hàng 5 ghế (nửa chiều rộng ≈ 1,2 đơn vị, tính cả phối cảnh) nằm gọn trong nửa phải / bề ngang điện thoại
    const k = Math.min(0.78, (W * (wide ? 0.4 : 0.44)) / (R * 1.2 * 1.2));
    const P = camera(-0.22 + tx * 0.3, 0.3 + ty * 0.12, R * k, wide ? cx - R * 0.14 : cx, cy + oy * R);   // lệch trái một chút để đốm sáng cuối không sát mép
    const FLOOR = 0.3, SEAT = 0, BACK = -0.36, w = 0.16, d = 0.15;

    // bóng sàn mờ dưới hàng ghế
    const fl = [P(chairX(0) - 0.4, FLOOR, -0.4), P(chairX(4) + 0.4, FLOOR, -0.4), P(chairX(4) + 0.4, FLOOR, 0.4), P(chairX(0) - 0.4, FLOOR, 0.4)];
    const gF = ctx.createLinearGradient(0, fl[2][1], 0, fl[0][1]);
    gF.addColorStop(0, color('primary', 0.1)); gF.addColorStop(1, color('primary', 0));
    poly(ctx, fl); ctx.fillStyle = gF; ctx.fill();

    for (let i = 0; i < CHAIRS; i++) {
      const x = chairX(i), lit = i < done;
      const line = color(lit ? 'primary' : 'muted', lit ? 0.85 : 0.5);
      ctx.lineWidth = 1.3; ctx.strokeStyle = line;
      // chân ghế
      [[-w, -d], [w, -d], [-w, d], [w, d]].forEach(([dx, dz]) => {
        const a = P(x + dx, SEAT + 0.04, dz), b2 = P(x + dx, FLOOR, dz);
        ctx.beginPath(); ctx.moveTo(a[0], a[1]); ctx.lineTo(b2[0], b2[1]); ctx.stroke();
      });
      // lưng ghế (ở phía xa, vẽ trước mặt ngồi)
      const back = [P(x - w, BACK, d), P(x + w, BACK, d), P(x + w, SEAT, d), P(x - w, SEAT, d)];
      poly(ctx, back); ctx.fillStyle = lit ? color('primary', 0.22) : color('surface-6', 0.55); ctx.fill(); ctx.stroke();
      // mặt ngồi
      const top = [P(x - w, SEAT, -d), P(x + w, SEAT, -d), P(x + w, SEAT, d), P(x - w, SEAT, d)];
      poly(ctx, top); ctx.fillStyle = lit ? color('primary', 0.35) : color('surface-7', 0.6); ctx.fill(); ctx.stroke();
      const lip = [P(x - w, SEAT, -d), P(x + w, SEAT, -d), P(x + w, SEAT + 0.04, -d), P(x - w, SEAT + 0.04, -d)];
      poly(ctx, lip); ctx.fillStyle = lit ? color('primary', 0.5) : color('surface-7', 0.8); ctx.fill();
    }

    // đốm sáng "bạn": ghế thứ (số việc đã xong); xong hết thì đứng phía trước ghế cuối
    const target = done >= CHAIRS ? chairX(CHAIRS - 1) + GAP * 0.9 : chairX(Math.min(done, CHAIRS - 1));
    if (seat === null || reduce) seat = target; else seat += (target - seat) * 0.05;
    const lift = done >= CHAIRS ? -0.25 : -0.14;
    const p = P(seat, lift, 0), b = reduce ? 0.5 : breath(t);
    const halo = ctx.createRadialGradient(p[0], p[1], 0, p[0], p[1], R * k * (0.22 + 0.12 * b) * p[2]);
    halo.addColorStop(0, color('primary', 0.45)); halo.addColorStop(1, color('primary', 0));
    ctx.fillStyle = halo; ctx.beginPath(); ctx.arc(p[0], p[1], R * k * (0.22 + 0.12 * b) * p[2], 0, TAU); ctx.fill();
    const core = ctx.createRadialGradient(p[0] - 3, p[1] - 3, 1, p[0], p[1], 9 * p[2]);
    core.addColorStop(0, color('surface', 1)); core.addColorStop(1, color('primary', 0.95));
    ctx.fillStyle = core; ctx.beginPath(); ctx.arc(p[0], p[1], 9 * p[2], 0, TAU); ctx.fill();
  }

  /* ================= Sau buổi phỏng vấn: mặt hồ gợn sóng ================= */
  const DROP_EVERY = 3.4;                                       // giây giữa hai giọt rơi
  const dropAt = (n) => ({ x: (rand(n * 3 + 1) - 0.5) * 1.8, z: (rand(n * 3 + 2) - 0.5) * 1.2, t0: n * DROP_EVERY });

  function dusk(S) {
    const { ctx, W, R, cx, cy, t, tx, ty, color, progress, reduce } = S;
    const wide = W > 900;
    const oy = wide ? 0.9 : -1.45;                               // máy tính: dưới khối giao diện mẫu; điện thoại: phía trên
    const half = Math.min(1.35, (W * 0.45) / R);
    const P = camera(-0.15 + tx * 0.3, 0.38 + ty * 0.1, R, cx, cy + oy * R);
    const now = reduce ? 2.2 : t;
    const calm = 1 - 0.6 * progress;                             // checklist xong càng nhiều, sóng càng nhỏ
    const last = Math.floor(now / DROP_EVERY), drops = [];
    for (let n = Math.max(0, last - 3); n <= last; n++) drops.push(dropAt(n));

    // vệt sáng mờ của trời chiều trên mặt hồ
    const gy = P(0, 0, 0.2)[1];
    const glow = ctx.createRadialGradient(cx, gy, 0, cx, gy, R * half);
    glow.addColorStop(0, color('primary', 0.14));
    glow.addColorStop(1, color('primary', 0));
    ctx.fillStyle = glow; ctx.fillRect(cx - R * half, gy - R * 0.45, R * half * 2, R * 0.9);

    const NX = 44, NZ = 13;
    for (let j = NZ - 1; j >= 0; j--) {                        // vẽ hàng xa trước
      const z = -0.6 + (j / (NZ - 1)) * 1.5;
      for (let i = 0; i < NX; i++) {
        const x = -half + (i / (NX - 1)) * 2 * half;
        let h = 0.012 * Math.sin(x * 3 + now * 0.8) * Math.cos(z * 2 + now * 0.5);
        for (const d of drops) {
          const age = now - d.t0, dist = Math.hypot(x - d.x, z - d.z);
          if (age < 0 || dist > age * 0.6 + 0.05) continue;     // sóng chưa lan tới
          h += 0.14 * calm * Math.exp(-age * 0.45) * Math.exp(-dist * 1.1) * Math.sin(dist * 9 - age * 4);
        }
        const p = P(x, -h, z), front = Math.max(0, Math.min(1, (1 - p[3]) / 1.6));
        const edge = Math.min(1, (half - Math.abs(x)) / 0.35);  // mờ dần ở hai mép
        const a = (0.18 + front * 0.55) * edge;
        ctx.fillStyle = h > 0.004 ? color('primary-2', Math.min(1, a + h * 5)) : color('accent', a * 0.8);
        ctx.beginPath(); ctx.arc(p[0], p[1], (0.7 + front * 1.3) * p[2] + Math.max(0, h) * 14, 0, TAU); ctx.fill();
      }
    }
  }

  return { night: { draw: night }, morning: { draw: morning }, waiting: { draw: waiting }, dusk: { draw: dusk } };
})();
