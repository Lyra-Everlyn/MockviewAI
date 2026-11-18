/* MockViewAI — Đợt 6: nút "Bắt đầu chuẩn bị" cố định đáy điện thoại, ẩn khi tới checklist/cuối trang */
(() => {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const fine = matchMedia('(pointer: fine)').matches;

  if (!document.getElementById('mobile-cta')) return;
  const mcta = document.getElementById('mobile-cta'), hide = new Set();
  const ctaObs = new IntersectionObserver((es) => {
    es.forEach((e) => e.isIntersecting ? hide.add(e.target) : hide.delete(e.target));
    mcta.classList.toggle('off', hide.size > 0);
  });
  ['top', 'checklist-sec', 'cta-cuoi'].forEach((id) => { const el = document.getElementById(id); el && ctaObs.observe(el); });
  const ft = document.querySelector('footer'); ft && ctaObs.observe(ft);
})();

/* Băng chuyền bài viết: mũi tên, số thứ tự, thanh tiến độ, kéo bằng chuột */
(() => {
  const track = document.getElementById('post-track');
  if (!track) return;
  const prev = document.getElementById('car-prev'), next = document.getElementById('car-next');
  const count = document.getElementById('car-count'), bar = document.getElementById('car-bar');
  const cards = [...track.querySelectorAll('.post-card')];
  const step = () => (cards[1] ? cards[1].offsetLeft - cards[0].offsetLeft : track.clientWidth);
  function update() {
    const max = track.scrollWidth - track.clientWidth;
    const i = Math.min(cards.length - 1, Math.round(track.scrollLeft / step()));
    prev.disabled = track.scrollLeft <= 4;
    next.disabled = track.scrollLeft >= max - 4;
    const vis = Math.max(1, Math.round(track.clientWidth / step()));
    const last = Math.min(cards.length, i + vis);
    count.textContent = vis > 1 && last > i + 1 ? `${i + 1}–${last} / ${cards.length}` : `${i + 1} / ${cards.length}`;
    bar.style.width = Math.min(100, vis / cards.length * 100) + '%';
    bar.style.marginLeft = (max > 0 ? track.scrollLeft / max : 0) * (100 - Math.min(100, vis / cards.length * 100)) + '%';
  }
  prev.addEventListener('click', () => track.scrollBy({ left: -step(), behavior: 'smooth' }));
  next.addEventListener('click', () => track.scrollBy({ left: step(), behavior: 'smooth' }));
  track.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowRight') { e.preventDefault(); next.click(); }
    if (e.key === 'ArrowLeft') { e.preventDefault(); prev.click(); }
  });
  track.addEventListener('scroll', () => requestAnimationFrame(update), { passive: true });
  addEventListener('resize', update);
  // Kéo ngang bằng chuột (điện thoại đã vuốt được sẵn)
  if (matchMedia('(pointer: fine)').matches) {
    let x0 = 0, s0 = 0, down = false, moved = false;
    track.addEventListener('pointerdown', (e) => { if (e.pointerType !== 'mouse' || e.button !== 0) return; down = true; moved = false; x0 = e.clientX; s0 = track.scrollLeft; });
    addEventListener('pointermove', (e) => {
      if (!down) return; const dx = e.clientX - x0;
      if (Math.abs(dx) > 6) { moved = true; track.classList.add('dragging'); track.scrollLeft = s0 - dx; }
    });
    addEventListener('pointerup', () => {
      if (!down) return; down = false;
      if (moved) { track.classList.remove('dragging'); const i = Math.round(track.scrollLeft / step()); track.scrollTo({ left: i * step(), behavior: 'smooth' }); }
    });
    track.addEventListener('click', (e) => { if (moved) { e.preventDefault(); moved = false; } }, true);
  }
  update();
})();
