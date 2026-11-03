// Generic submit overlay for any form with data-loading-overlay.
// CSS lives in components.css, colors in theme-indigo.css.
(() => {
    const OVERLAY_ID = 'app-loading-overlay';
    let activeForm = null;
    let savedFocus = null;

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

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('form[data-loading-overlay]').forEach(form => {
            form.addEventListener('submit', event => {
                if (event.defaultPrevented) return;
                if (form.dataset.submitting === '1') {
                    event.preventDefault();
                    return;
                }
                if (!form.checkValidity()) return;

                // Turnstile is only present on selected forms. The backend must still validate its token.
                const turnstileWidget = form.querySelector('.cf-turnstile');
                if (turnstileWidget && typeof window.turnstile?.getResponse === 'function') {
                    const response = window.turnstile.getResponse();
                    if (!response) return; // Do not override the widget's own verification behavior.
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
})();
