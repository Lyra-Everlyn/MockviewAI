/* MockViewAI — Đợt 2: đường thời gian 3 giai đoạn (tab, chọn bằng chuột hoặc phím mũi tên) */
(() => {

  /* ---------- 5. Tab hành trình ---------- */
  const tabs = [...document.querySelectorAll('[role=tab]')];
  function select(tab) {
    tabs.forEach((t) => {
      const on = t === tab, p = document.getElementById(t.getAttribute('aria-controls'));
      t.setAttribute('aria-selected', on); t.tabIndex = on ? 0 : -1; p.hidden = !on;
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
