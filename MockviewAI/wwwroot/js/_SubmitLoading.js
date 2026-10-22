// Hiệu ứng chờ khi gửi form (22/10). Form nào có data-loading-text sẽ:
//  - khoá nút gửi + đổi chữ (tránh bấm nhiều lần -> gửi nhiều email),
//  - hiện lớp phủ giữa màn hình với vòng xoay + câu mô tả (data-loading-overlay), để người dùng biết hệ thống đang xử lý.
// Không cần file CSS riêng: style được chèn một lần bằng JS.
(function () {
    const STYLE_ID = 'mv-submit-loading-style';

    function ensureStyle() {
        if (document.getElementById(STYLE_ID)) return;
        const style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent =
            '.mv-loading-overlay{position:fixed;inset:0;background:rgba(15,23,42,.45);display:flex;align-items:center;justify-content:center;z-index:9999}' +
            '.mv-loading-box{background:#fff;border-radius:16px;padding:28px 32px;max-width:340px;width:calc(100% - 32px);text-align:center;box-shadow:0 20px 40px rgba(0,0,0,.2);font-family:inherit}' +
            '.mv-spinner{width:44px;height:44px;margin:0 auto 14px;border:4px solid #e0e7ff;border-top-color:#4361ee;border-radius:50%;animation:mv-spin .8s linear infinite}' +
            '.mv-loading-text{margin:0;color:#1e293b;font-weight:600;line-height:1.5}' +
            '@keyframes mv-spin{to{transform:rotate(360deg)}}';
        document.head.appendChild(style);
    }

    function showOverlay(text) {
        ensureStyle();
        const overlay = document.createElement('div');
        overlay.className = 'mv-loading-overlay';
        overlay.setAttribute('role', 'status');
        overlay.setAttribute('aria-live', 'polite');
        overlay.innerHTML = '<div class="mv-loading-box"><div class="mv-spinner"></div><p class="mv-loading-text"></p></div>';
        overlay.querySelector('.mv-loading-text').textContent = text;
        document.body.appendChild(overlay);
        return overlay;
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form[data-loading-text]').forEach(function (form) {
            const button = form.querySelector('button[type="submit"]');
            const originalText = button ? button.textContent : '';
            let overlay = null;

            // Chỉ chạy khi form hợp lệ (trình duyệt đã kiểm tra required/email) -> sự kiện submit mới xảy ra
            form.addEventListener('submit', function (e) {
                if (e.defaultPrevented) return;
                if (form.dataset.submitting === '1') { e.preventDefault(); return; }
                form.dataset.submitting = '1';
                if (button) {
                    button.disabled = true;
                    button.textContent = form.dataset.loadingText;
                }
                if (form.dataset.loadingOverlay) overlay = showOverlay(form.dataset.loadingOverlay);
            });

            // Bấm "Back" của trình duyệt quay lại trang: mở khoá nút, gỡ lớp phủ
            window.addEventListener('pageshow', function () {
                form.dataset.submitting = '';
                if (button) { button.disabled = false; button.textContent = originalText; }
                if (overlay) { overlay.remove(); overlay = null; }
            });
        });
    });
})();
