// MockViewAI
// Step 1 (Buổi 3): Tích checklist → cập nhật tiến độ ngay.
// Step 2 (Buổi 4): Lưu tiến độ vào Supabase → F5 vẫn còn.
// Step 3 (Buổi 5): Tài khoản (email + mật khẩu) — Nhịp 1 Danh tính (đăng ký / đăng nhập / đăng xuất / giữ phiên)
//                  + Nhịp 2 Quyền dữ liệu (mỗi dòng gắn user_id; RLS: auth.uid() = user_id).

// Khóa publishable: loại khóa an toàn cho trình duyệt. Dữ liệu được bảo vệ bằng RLS trên Supabase.
const SUPABASE_URL = 'https://xqbonoetvwfzucczuuka.supabase.co';
const SUPABASE_KEY = 'sb_publishable_kATAuM1v6Tr_Qy0J9iqbig_8M7V9OKF';
const TABLE = 'checklist_progress';

document.addEventListener('DOMContentLoaded', () => {
  const $ = (id) => document.getElementById(id);
  // Bật/tắt bằng class (thuộc tính hidden bị class 'flex' của Tailwind đè lên).
  const show = (el, on, display = 'flex') => { if (!el) return; el.classList.toggle('hidden', !on); el.classList.toggle(display, on); };

  const boxes = [...document.querySelectorAll('#checklist-items input[type="checkbox"]')];
  const countEl = $('checklist-count');
  const percentEl = $('checklist-percent');
  const bar = $('checklist-bar');
  const fill = $('checklist-bar-fill');
  const doneMsg = $('checklist-done');
  const statusEl = $('save-status');
  const lockEl = $('checklist-locked');
  if (!boxes.length || !countEl) return;

  const auth = {
    openBtn: $('auth-open'), openBtn2: $('auth-open-2'), userBox: $('auth-user'), email: $('auth-email'), logout: $('auth-logout'),
    dialog: $('auth-dialog'), form: $('auth-form'), title: $('auth-title'), emailIn: $('auth-email-input'), passIn: $('auth-password-input'),
    error: $('auth-error'), info: $('auth-info'), submit: $('auth-submit'), switchBtn: $('auth-switch'), switchText: $('auth-switch-text'), close: $('auth-close'),
  };

  const setStatus = (text) => { if (statusEl) statusEl.textContent = text; };
  const db = window.supabase ? window.supabase.createClient(SUPABASE_URL, SUPABASE_KEY) : null;
  let currentUser = null;
  let mode = 'login';

  // ---------- Step 1: hiển thị tiến độ ----------
  function render() {
    const total = boxes.length;
    const done = boxes.filter((b) => b.checked).length;
    const pct = Math.round((done / total) * 100);
    countEl.textContent = `${done} / ${total} hoàn thành`;
    percentEl.textContent = `${pct}% tiến độ`;
    fill.style.width = `${pct}%`;
    bar.setAttribute('aria-valuenow', String(pct));
    boxes.forEach((b) => b.closest('.checklist-item').classList.toggle('is-done', b.checked));
    show(doneMsg, done === total);
  }

  // ---------- Step 3 · Nhịp 1: danh tính ----------
  function setMessage(el, text) { el.textContent = text || ''; show(el, !!text, 'block'); }

  function setMode(next) {
    mode = next;
    const isLogin = mode === 'login';
    auth.title.textContent = isLogin ? 'Đăng nhập' : 'Tạo tài khoản';
    auth.submit.textContent = isLogin ? 'Đăng nhập' : 'Đăng ký';
    auth.switchText.textContent = isLogin ? 'Chưa có tài khoản?' : 'Đã có tài khoản?';
    auth.switchBtn.textContent = isLogin ? 'Đăng ký' : 'Đăng nhập';
    auth.passIn.autocomplete = isLogin ? 'current-password' : 'new-password';
    setMessage(auth.error, ''); setMessage(auth.info, '');
  }

  function openAuth(next = 'login') {
    if (!auth.dialog) return;
    setMode(next);
    if (!auth.dialog.open) auth.dialog.showModal();
    auth.emailIn.focus();
  }

  // Đổi lỗi kỹ thuật thành câu hướng dẫn người dùng sửa (B3 s12).
  function friendlyError(err) {
    const msg = (err && err.message ? err.message : '').toLowerCase();
    if (msg.includes('invalid login credentials')) return 'Email hoặc mật khẩu chưa đúng. Kiểm tra lại giúp bạn nhé.';
    if (msg.includes('email not confirmed')) return 'Bạn chưa xác nhận email. Mở hộp thư (cả mục Spam) và bấm link xác nhận trước khi đăng nhập.';
    if (msg.includes('already registered') || msg.includes('already been registered')) return 'Email này đã có tài khoản. Hãy chuyển sang Đăng nhập.';
    if (msg.includes('password') && msg.includes('6')) return 'Mật khẩu cần ít nhất 6 ký tự.';
    if (msg.includes('rate limit') || msg.includes('too many')) return 'Bạn thao tác hơi nhanh. Đợi khoảng 1 phút rồi thử lại.';
    if (msg.includes('fetch') || msg.includes('network')) return 'Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại.';
    return 'Có lỗi xảy ra, bạn thử lại sau ít phút nhé.';
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setMessage(auth.error, ''); setMessage(auth.info, '');
    const email = auth.emailIn.value.trim();
    const password = auth.passIn.value;
    if (!email) return setMessage(auth.error, 'Bạn chưa nhập email.');
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return setMessage(auth.error, 'Email chưa đúng định dạng (ví dụ: ten@gmail.com).');
    if (password.length < 6) return setMessage(auth.error, 'Mật khẩu cần ít nhất 6 ký tự.');
    if (!db) return setMessage(auth.error, 'Chưa kết nối được máy chủ. Tải lại trang rồi thử lại.');

    const label = auth.submit.textContent;
    auth.submit.disabled = true; auth.submit.textContent = 'Đang xử lý…';
    try {
      if (mode === 'login') {
        const { error } = await db.auth.signInWithPassword({ email, password });
        if (error) return setMessage(auth.error, friendlyError(error));
        auth.passIn.value = '';
        auth.dialog.close();                         // giao diện cập nhật qua onAuthStateChange
      } else {
        const options = location.protocol.startsWith('http') ? { emailRedirectTo: location.origin + location.pathname } : {};
        const { data, error } = await db.auth.signUp({ email, password, options });
        if (error) return setMessage(auth.error, friendlyError(error));
        if (data.user && Array.isArray(data.user.identities) && data.user.identities.length === 0) {
          return setMessage(auth.error, 'Email này đã có tài khoản. Hãy chuyển sang Đăng nhập.');
        }
        if (data.session) { auth.dialog.close(); return; }   // trường hợp dự án tắt xác nhận email
        auth.passIn.value = '';
        setMessage(auth.info, `Đã gửi email xác nhận tới ${email}. Bấm link trong email (xem cả mục Spam), rồi quay lại đăng nhập.`);
      }
    } finally {
      auth.submit.disabled = false; auth.submit.textContent = label;
    }
  }

  function applyUser(user) {
    currentUser = user;
    show(auth.openBtn, !user, 'inline-flex');
    show(auth.userBox, !!user, 'flex');
    if (auth.email) auth.email.textContent = user ? user.email : '';
    const owner = $('auth-email-2'); if (owner) owner.textContent = user ? user.email : '';
    show($('checklist-owner'), !!user, 'flex');
    show(lockEl, !user, 'flex');
    boxes.forEach((b) => { b.disabled = !user; });
    if (!user) {
      boxes.forEach((b) => { b.checked = false; });   // không để lại tiến độ của người vừa đăng xuất
      render();
      setStatus('Đăng nhập để lưu tiến độ chuẩn bị của riêng bạn.');
      return;
    }
    loadProgress();
  }

  // ---------- Step 2: lưu & đọc lại từ Supabase ----------
  async function loadProgress() {
    if (!db || !currentUser) return;
    const uid = currentUser.id;
    boxes.forEach((b) => { b.disabled = true; });
    setStatus('Đang tải tiến độ…');
    // RLS ở database đã chỉ trả về dòng của người đang đăng nhập; lọc thêm user_id cho rõ ràng.
    const { data, error } = await db.from(TABLE).select('item_key, is_done').eq('user_id', uid);
    if (!currentUser || currentUser.id !== uid) return;  // người dùng đã đổi trong lúc chờ
    boxes.forEach((b) => { b.disabled = false; });
    if (error) { setStatus('Chưa tải được tiến độ. Kiểm tra mạng rồi tải lại trang.'); return; }
    const saved = new Map(data.map((row) => [row.item_key, row.is_done]));
    boxes.forEach((b) => { b.checked = saved.get(b.dataset.key) === true; });
    render();
    setStatus('Tiến độ được lưu tự động.');
  }

  async function saveItem(box) {
    const previous = !box.checked;
    if (!db || !currentUser) { box.checked = previous; render(); openAuth('login'); return; }
    box.disabled = true;
    setStatus('Đang lưu…');
    // Ghi kèm user_id; database kiểm tra lại auth.uid() = user_id nên không thể ghi hộ người khác.
    const { error } = await db.from(TABLE).upsert(
      { user_id: currentUser.id, item_key: box.dataset.key, is_done: box.checked },
      { onConflict: 'user_id,item_key' },
    );
    box.disabled = false;
    if (error) { box.checked = previous; render(); setStatus('Chưa lưu được, thử lại.'); return; }
    setStatus('Đã lưu.');                         // chỉ báo "Đã lưu" sau khi Supabase xác nhận
  }

  // ---------- Gắn sự kiện ----------
  boxes.forEach((b) => b.addEventListener('change', () => { render(); saveItem(b); }));
  auth.openBtn && auth.openBtn.addEventListener('click', () => openAuth('login'));
  auth.openBtn2 && auth.openBtn2.addEventListener('click', () => openAuth('login'));
  auth.switchBtn && auth.switchBtn.addEventListener('click', () => setMode(mode === 'login' ? 'signup' : 'login'));
  auth.close && auth.close.addEventListener('click', () => auth.dialog.close());
  auth.form && auth.form.addEventListener('submit', handleSubmit);
  auth.logout && auth.logout.addEventListener('click', async () => { if (db) await db.auth.signOut(); });


  // "Bắt đầu chuẩn bị": chưa đăng nhập → mở hộp đăng nhập; đã đăng nhập → tới checklist.
  document.querySelectorAll('a[href="#checklist"]').forEach((link) => {
    link.addEventListener('click', (e) => {
      e.preventDefault();
      document.getElementById('checklist').scrollIntoView({ behavior: 'smooth' });
      if (!currentUser) { setTimeout(() => openAuth('login'), 350); return; }
      const target = boxes.find((b) => !b.checked) || boxes[0];
      setTimeout(() => target.focus({ preventScroll: true }), 450);
    });
  });

  // Step 4 (Buổi 6) — nút "Kiểm tra DOI" nằm ở assets/doi-check.js (dùng chung với trang bài viết).

  render();
  if (!db) { applyUser(null); setStatus('Chưa kết nối được máy chủ — tải lại trang để thử lại.'); return; }
  // Supabase tự giữ phiên trong trình duyệt; sự kiện INITIAL_SESSION báo phiên hiện có khi mở trang / F5.
  db.auth.onAuthStateChange((event, session) => {
    const user = session ? session.user : null;
    if (event !== 'INITIAL_SESSION' && (user ? user.id : null) === (currentUser ? currentUser.id : null)) return;
    setTimeout(() => applyUser(user), 0);           // không gọi Supabase trực tiếp trong callback
  });
});
