/* MockViewAI — Đợt 4: bài tập thở 4–6 */
(() => {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const fine = matchMedia('(pointer: fine)').matches;

  if (!document.getElementById('b-orb')) return;
  /* ---------- 6. Bài thở 4–6 ---------- */
  const orb = document.getElementById('b-orb'), phase = document.getElementById('b-phase'), secEl = document.getElementById('b-sec');
  const start = document.getElementById('b-start'), stop = document.getElementById('b-stop'), count = document.getElementById('b-count');
  const ROUNDS = 6; let timer = null, tick = null, round = 0;
  function reset(msg) {
    clearTimeout(timer); clearInterval(tick); orb.classList.remove('in', 'out');
    phase.textContent = msg || 'Sẵn sàng'; secEl.textContent = ''; start.disabled = false; stop.disabled = true;
  }
  function countdown(n) { secEl.textContent = n; clearInterval(tick); tick = setInterval(() => { n--; secEl.textContent = n > 0 ? n : ''; if (n <= 0) clearInterval(tick); }, 1000); }
  function inhale() {
    round++; count.textContent = `Nhịp ${round} / ${ROUNDS}`;
    orb.classList.remove('out'); orb.classList.add('in'); phase.textContent = 'Hít vào'; countdown(4);
    timer = setTimeout(exhale, 4000);
  }
  function exhale() {
    orb.classList.add('out'); orb.classList.remove('in'); phase.textContent = 'Thở ra'; countdown(6);
    timer = setTimeout(() => { if (round < ROUNDS) inhale(); else { reset('Xong rồi'); count.textContent = `Hoàn thành ${ROUNDS} nhịp. Bạn có thể làm lại bất cứ lúc nào.`; } }, 6000);
  }
  start.addEventListener('click', () => { round = 0; start.disabled = true; stop.disabled = false; inhale(); });
  stop.addEventListener('click', () => { reset('Đã dừng'); count.textContent = `${ROUNDS} nhịp · khoảng 1 phút`; });
})();
