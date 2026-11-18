/* MockViewAI — Đợt 2: tab hành trình 3 giai đoạn */
(() => {
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const fine = matchMedia('(pointer: fine)').matches;

  const fillMeters = (root) => root.querySelectorAll('.meter-fill').forEach((m) => { m.style.width = m.dataset.w + '%'; });
  /* ---------- 5. Tab hành trình ---------- */
  const tabs = [...document.querySelectorAll('[role=tab]')];
  function select(tab) {
    tabs.forEach((t) => {
      const on = t === tab, p = document.getElementById(t.getAttribute('aria-controls'));
      t.setAttribute('aria-selected', on); t.tabIndex = on ? 0 : -1; p.hidden = !on;
      if (on) { p.classList.add('in'); requestAnimationFrame(() => fillMeters(p)); }
    });
  }
  tabs.forEach((t, i) => {
    t.addEventListener('click', () => select(t));
    t.addEventListener('keydown', (e) => {
      const k = { ArrowRight: 1, ArrowLeft: -1 }[e.key]; if (!k) return;
      const n = tabs[(i + k + tabs.length) % tabs.length]; n.focus(); select(n);
    });
  });
})();
