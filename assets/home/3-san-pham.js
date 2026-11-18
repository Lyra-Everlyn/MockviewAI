/* MockViewAI — Đợt 3: demo phòng phỏng vấn trong hộp thoại */
(() => {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const fine = matchMedia('(pointer: fine)').matches;

  const mmss = (n) => `${Math.floor(n / 60)}:${String(n % 60).padStart(2, '0')}`;
  // Demo phòng phỏng vấn: suy nghĩ 15s → ghi âm (chữ hiện dần) → câu hỏi nối tiếp
  const room = document.getElementById('demo-room');
  if (room) {
    const $d = (id) => document.getElementById(id);
    const pill = room.querySelector('.ui-pill'), q = $d('d-q'), fol = $d('d-follow'), tl = $d('d-tlabel'),
      tm = $d('d-timer'), tr = $d('d-track'), ans = $d('d-ans'), go = $d('d-go'), skip = $d('d-skip'), live = $d('d-live');
    const QS = [
      { q: 'Chào bạn, hãy giới thiệu ngắn về bản thân bạn.', a: 'Em tên Hà, sinh viên năm cuối ngành Công nghệ thông tin. Em thích làm web và đã làm vài dự án nhóm…' },
      { q: 'Bạn cho chị biết thêm bạn đang học gì và đang ở giai đoạn nào nhé?', a: 'Em đang làm đồ án tốt nghiệp: web bán sách bằng ASP.NET, đã có khoảng 50 bạn dùng thử…', follow: true },
    ];
    let step = 0, state = 'think', left = 15, timer = null, typer = null, started = false;
    const say = (t) => { live.textContent = t; };
    function setThink() {
      const Q = QS[step]; state = 'think'; left = 15; clearInterval(timer); clearInterval(typer);
      room.classList.remove('is-rec'); q.textContent = Q.q; fol.hidden = !Q.follow;
      pill.className = 'ui-pill'; pill.textContent = Q.follow ? 'Chờ bạn trả lời' : 'Đang suy nghĩ…';
      tl.textContent = 'Thời gian suy nghĩ'; ans.className = 'ph'; ans.textContent = 'Câu trả lời của bạn sẽ hiện ở đây.';
      go.disabled = false; go.querySelector('span').textContent = 'Trả lời ngay'; skip.disabled = false;
      tm.textContent = mmss(left); tr.style.width = '100%';
      timer = setInterval(() => { left--; tm.textContent = mmss(left); tr.style.width = (left / 15 * 100) + '%'; if (left <= 0) startRec(); }, 1000);
      say(`Câu hỏi: ${Q.q}`);
    }
    function startRec() {
      if (state !== 'think') return;
      state = 'rec'; clearInterval(timer); room.classList.add('is-rec');
      pill.className = 'ui-pill rec'; pill.textContent = 'Đang ghi âm'; tl.textContent = 'Thời gian trả lời (tối đa 2 phút)';
      go.querySelector('span').textContent = 'Gửi câu trả lời'; go.disabled = true; skip.disabled = true; say('Micro đã bật, đang ghi âm.');
      let sec = 0; tm.textContent = mmss(0); tr.style.width = '0%';
      timer = setInterval(() => { sec++; tm.textContent = mmss(sec); tr.style.width = (sec / 120 * 100) + '%'; }, 1000);
      const text = QS[step].a; let i = 0; ans.className = ''; ans.textContent = '';
      const caret = document.createElement('span'); caret.className = 'ui-caret';
      if (reduce) { ans.textContent = text; go.disabled = false; return; }
      typer = setInterval(() => {
        i++; ans.textContent = text.slice(0, i); ans.appendChild(caret);
        if (i >= text.length) { clearInterval(typer); go.disabled = false; }
      }, 38);
    }
    function send() {
      clearInterval(timer); clearInterval(typer); room.classList.remove('is-rec');
      if (step < QS.length - 1) { step++; setThink(); return; }
      state = 'done'; pill.className = 'ui-pill'; pill.textContent = 'Đã xong buổi'; tl.textContent = 'Đã gửi';
      go.querySelector('span').textContent = 'Làm lại'; go.disabled = false; skip.disabled = true;
      say('Đã xong buổi luyện.');
    }
    go.addEventListener('click', () => {
      if (state === 'think') startRec();
      else if (state === 'rec') send();
      else { step = 0; setThink(); }
    });
    skip.addEventListener('click', () => { if (state === 'think') { say('Đã bỏ qua, câu này tính Sai.'); send(); } });
    // Chỉ chạy đồng hồ khi khối demo hiện trên màn hình
    const dd = document.getElementById('demo-dialog');
    document.getElementById('open-demo').addEventListener('click', () => { dd.showModal(); step = 0; setThink(); });
    document.getElementById('demo-close').addEventListener('click', () => dd.close());
    dd.addEventListener('click', (e) => { if (e.target === dd) dd.close(); });
    dd.addEventListener('close', () => { clearInterval(timer); clearInterval(typer); state = 'think'; });
  }
})();
