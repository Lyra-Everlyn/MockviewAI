//#region _SubmitLoading
/// <summary>
/// Màn hình chờ dùng chung cho mọi form có thuộc tính data-loading-overlay="Nội dung thông báo":
/// khoá nút gửi, chặn gửi trùng, mở lại khi người dùng quay về trang (pageshow).
/// Giao diện nằm trong components.css, màu trong các file theme.
/// </summary>
//#endregion
(() => {
    const OVERLAY_ID = 'app-loading-overlay';
    let activeForm = null;
    let savedFocus = null;

    //#region Overlay
    function createOverlay(message) {
        let overlay = document.getElementById(OVERLAY_ID);
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = OVERLAY_ID;
            overlay.className = 'loading-overlay';
            overlay.hidden = true;
            overlay.setAttribute('role', 'alertdialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.setAttribute('aria-labelledby', 'app-loading-title');
            overlay.setAttribute('aria-describedby', 'app-loading-description');
            overlay.innerHTML = `
                <div class="loading-dialog" tabindex="-1">
                    <span class="loading-dialog__spinner" aria-hidden="true"></span>
                    <h2 class="loading-dialog__title" id="app-loading-title">Đang xử lý</h2>
                    <p class="loading-dialog__description" id="app-loading-description"></p>
                    <span class="loading-dialog__note">Vui lòng không gửi yêu cầu nhiều lần.</span>
                </div>`;
            document.body.appendChild(overlay);
        }
        overlay.querySelector('.loading-dialog__description').textContent = message;
        return overlay;
    }

    /// <summary>Ẩn overlay, mở khoá nút và trả focus (khi người dùng bấm Back quay lại trang).</summary>
    function reset() {
        const overlay = document.getElementById(OVERLAY_ID);
        if (overlay) overlay.hidden = true;
        document.body.classList.remove('is-loading');
        if (activeForm) {
            delete activeForm.dataset.submitting;
            const button = activeForm.querySelector('button[type="submit"]');
            if (button) {
                button.disabled = false;
                if (button.dataset.originalHtml !== undefined) {
                    button.innerHTML = button.dataset.originalHtml;
                    delete button.dataset.originalHtml;
                }
            }
        }
        activeForm = null;
        if (savedFocus && savedFocus.isConnected) savedFocus.focus({preventScroll:true});
        savedFocus = null;
    }
    //#endregion

    //#region Sự kiện
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('form[data-loading-overlay]').forEach(form => {
            form.addEventListener('submit', event => {
                if (event.defaultPrevented) return;
                if (form.dataset.submitting === '1') {
                    event.preventDefault();
                    return;
                }
                if (!form.checkValidity()) return;

                // Form có Turnstile: chưa xác minh xong thì để widget tự xử lý (server vẫn kiểm tra token)
                const turnstileWidget = form.querySelector('.cf-turnstile');
                if (turnstileWidget && typeof window.turnstile?.getResponse === 'function') {
                    const response = window.turnstile.getResponse();
                    if (!response) return;
                }

                form.dataset.submitting = '1';
                activeForm = form;
                savedFocus = document.activeElement;
                const button = form.querySelector('button[type="submit"]');
                if (button) {
                    button.dataset.originalHtml = button.innerHTML;
                    button.disabled = true;
                    button.textContent = form.dataset.loadingText || 'Đang xử lý...';
                }
                const overlay = createOverlay(form.dataset.loadingOverlay);
                overlay.hidden = false;
                document.body.classList.add('is-loading');
                overlay.querySelector('.loading-dialog').focus({preventScroll:true});
            });
        });
        window.addEventListener('pageshow', reset);
    });
    //#endregion
})();
