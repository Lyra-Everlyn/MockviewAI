// MockViewAI — Step 4 (Buổi 6): nút "Kiểm tra DOI" dùng chung cho trang chủ và trang bài viết.
// Luồng: trình duyệt → Edge Function `doi-check` (Supabase) → Crossref → hiện kết quả.
// Chỉ gửi chuỗi DOI. Không cần đăng nhập, không có khóa bí mật (publishable key là khóa công khai).
(function () {
  const FN_URL = 'https://xqbonoetvwfzucczuuka.supabase.co/functions/v1/doi-check';
  const PUBLISHABLE_KEY = 'sb_publishable_kATAuM1v6Tr_Qy0J9iqbig_8M7V9OKF';

  // Giao diện riêng của nút → dùng được cả ở trang có Tailwind lẫn trang không có.
  const css = `
  .doi-btn{display:inline-flex;align-items:center;gap:.4rem;padding:.4rem .8rem;border:1px solid #1a61a1;border-radius:.5rem;
    background:#fff;color:#1a61a1;font:600 .8125rem/1.2 'Be Vietnam Pro',system-ui,sans-serif;cursor:pointer;min-height:36px}
  .doi-btn:hover{background:#edf4ff}.doi-btn:disabled{opacity:.6;cursor:wait}
  .doi-btn:focus-visible{outline:2px solid #1a61a1;outline-offset:2px}
  .doi-result{margin-top:.5rem;font:400 .875rem/1.5 'Be Vietnam Pro',system-ui,sans-serif;color:#43474e}
  .doi-result p{margin:.15rem 0}.doi-result .ok{color:#1a61a1;font-weight:600}.doi-result .err{color:#ba1a1a}
  .doi-result .k{font-weight:600;color:#121d26}`;
  const style = document.createElement('style');
  style.textContent = css;
  document.head.appendChild(style);

  function line(label, value) {
    const p = document.createElement('p');
    const k = document.createElement('span');
    k.className = 'k';
    k.textContent = label + ': ';
    p.append(k, document.createTextNode(value)); // textContent → dữ liệu bên ngoài không chèn được HTML
    return p;
  }

  function setup(holder) {
    const doi = holder.dataset.doi;
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'doi-btn';
    btn.textContent = 'Kiểm tra DOI trên Crossref';
    const result = document.createElement('div');
    result.className = 'doi-result';
    result.setAttribute('aria-live', 'polite');
    holder.append(btn, result);

    btn.addEventListener('click', async () => {
      btn.disabled = true; // chặn bấm liên tục → không gọi trùng
      btn.textContent = 'Đang kiểm tra…';
      result.replaceChildren();
      let msg = null;
      try {
        const res = await fetch(FN_URL, {
          method: 'POST',
          headers: { apikey: PUBLISHABLE_KEY, 'Content-Type': 'application/json' },
          body: JSON.stringify({ doi }),
        });
        const data = await res.json();
        if (!res.ok || !data.ok) msg = data.error || 'Chưa kiểm tra được, thử lại sau.';
        else if (!data.found) msg = 'Không tìm thấy DOI này trên Crossref.';
        else {
          const head = document.createElement('p');
          head.className = 'ok';
          head.textContent = '✓ Đã xác minh trên Crossref';
          const authors = (data.authors || []).join('; ') + (data.authorCount > 3 ? ' và cộng sự' : '');
          result.append(head, line('Tên bài gốc', data.title || '—'), line('Tạp chí', data.journal || '—'),
            line('Năm', String(data.year || '—')), line('Tác giả', authors || '—'));
          btn.textContent = 'Kiểm tra lại';
        }
      } catch (_e) {
        msg = 'Chưa kiểm tra được (mất mạng?). Thử lại sau.';
      }
      if (msg) {
        const p = document.createElement('p');
        p.className = 'err';
        p.textContent = msg;
        result.append(p);
        btn.textContent = 'Thử lại';
      }
      btn.disabled = false;
    });
  }

  function init() { document.querySelectorAll('.doi-check[data-doi]').forEach(setup); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
