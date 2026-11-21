/* MockViewAI — câu hỏi "Buổi phỏng vấn của bạn khi nào?" ở hero.
   Mỗi lựa chọn đưa người dùng tới đúng công cụ cần dùng lúc đó:
   - Còn vài tuần / Vài ngày nữa → checklist, làm nổi các việc đúng thời điểm
   - Hôm nay → bài thở 1 phút
   - Vừa phỏng vấn xong → tab "Sau phỏng vấn" trong phần Hành trình
   Lựa chọn được nhớ trên trình duyệt (localStorage "mv-when") để lần sau mở lại vẫn thấy. */
(() => {
  // Link từ trang nghiên cứu, ví dụ index.html?giai-doan=sau#hanh-trinh: mở sẵn đúng tab trong phần Hành trình
  const stageTab = { truoc: 't-truoc', trong: 't-trong', sau: 't-sau' }[new URLSearchParams(location.search).get('giai-doan')];
  if (stageTab) document.getElementById(stageTab)?.click();

  const ask = document.getElementById('when-ask');
  if (!ask) return;
  const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const KEY = 'mv-when';
  const opts = [...ask.querySelectorAll('.when-opt')];
  const list = document.getElementById('checklist-items');
  const items = list ? [...list.querySelectorAll('.checklist-item')] : [];
  const box = document.getElementById('cl-focus');
  const boxText = document.getElementById('cl-focus-text');
  const boxClear = document.getElementById('cl-focus-x');
  const boxList = document.getElementById('cl-focus-list');

  const MSG = {
    tuan: 'Còn vài tuần, bắt đầu từ hai việc này:',
    ngay: 'Còn vài ngày, tập trung vào hai việc này:',
    homnay: 'Hôm nay, ngay trước khi vào phòng:'
  };

  // Làm nổi các việc trong checklist khớp với thời điểm đã chọn
  function highlight(when) {
    const on = !!(when && MSG[when]);
    items.forEach((li) => li.classList.toggle('is-focus', on && li.dataset.when === when));
    if (list) list.classList.toggle('has-focus', on);
    if (box) {
      box.hidden = !on;
      if (on) {
        boxText.textContent = MSG[when];
        // Ghi tên các việc ra ngoài hộp checklist: chưa đăng nhập (hộp bị khóa) vẫn đọc được
        boxList.replaceChildren(...items.filter((li) => li.dataset.when === when).map((li) => {
          const el = document.createElement('li');
          el.textContent = li.querySelector('.t')?.textContent || '';
          return el;
        }));
      }
    }
  }

  function markPressed(when) {
    opts.forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.when === when)));
  }

  function save(when) {
    try { when ? localStorage.setItem(KEY, when) : localStorage.removeItem(KEY); } catch (e) { /* trình duyệt chặn lưu: bỏ qua */ }
  }

  // Cuộn tới khu vực rồi chuyển focus bàn phím vào đó (người dùng bàn phím/trình đọc màn hình không bị lạc)
  function goTo(id, focusEl) {
    const sec = document.getElementById(id);
    if (!sec) return;
    sec.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' });
    const target = focusEl || sec;
    if (!focusEl && !sec.hasAttribute('tabindex')) sec.setAttribute('tabindex', '-1');
    setTimeout(() => target.focus({ preventScroll: true }), reduce ? 0 : 600);
  }

  function choose(when) {
    markPressed(when);
    save(when);
    highlight(when);
    if (when === 'tuan' || when === 'ngay') {
      goTo('checklist-sec', items.find((li) => li.dataset.when === when)?.querySelector('input:not(:disabled)') || null);
    } else if (when === 'homnay') {
      goTo('tho', document.getElementById('b-start'));
    } else if (when === 'xong') {
      const tab = document.getElementById('t-sau');
      if (tab) tab.click(); // 2-hanh-trinh.js xử lý việc mở tab
      goTo('hanh-trinh', tab);
    }
  }

  opts.forEach((b) => b.addEventListener('click', () => choose(b.dataset.when)));

  if (boxClear) boxClear.addEventListener('click', () => {
    highlight(null);
    markPressed(null);
    save(null);
  });

  // Mở lại trang: khôi phục lựa chọn cũ nhưng không tự cuộn
  let saved = null;
  try { saved = localStorage.getItem(KEY); } catch (e) { saved = null; }
  if (saved && opts.some((b) => b.dataset.when === saved)) {
    markPressed(saved);
    highlight(saved);
  }
})();
