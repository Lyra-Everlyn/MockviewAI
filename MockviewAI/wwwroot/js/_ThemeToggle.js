//#region _ThemeToggle
/// <summary>
/// [THEME] 
/// Điều khiển nút chuyển giao diện sáng / tối.
/// -- Lưu lựa chọn vào localStorage (khoá "mockview-theme") thông qua window.MockviewTheme (định nghĩa trong _ThemeHead).
/// -- Đồng bộ giữa các tab đang mở cùng lúc (sự kiện storage).
/// -- Cập nhật trạng thái trợ năng của nút (aria-pressed, aria-label) và theme của Cloudflare Turnstile.
/// </summary>
//#endregion
(function (window, document) {
    'use strict';

    var core = window.MockviewTheme;

    // Trang chưa nhúng _ThemeHead, hoặc script đã được nạp một lần rồi -> không làm gì
    if (!core || core.toggleReady) return;
    core.toggleReady = true;

    var TOGGLE_SELECTOR = '[data-theme-toggle]';

    //#region Button state
    /// <summary>
    /// Cập nhật trạng thái trợ năng cho mọi nút chuyển theme trên trang.
    /// Phần hiển thị (icon, chữ) do CSS lo theo html[data-theme].
    /// </summary>
    function syncButtons(theme) {
        var isDark = theme === core.themes.dark;
        var buttons = document.querySelectorAll(TOGGLE_SELECTOR);

        for (var i = 0; i < buttons.length; i++) {
            buttons[i].setAttribute('aria-pressed', String(isDark));
            buttons[i].setAttribute('aria-label', isDark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối');
        }
    }
    //#endregion

    //#region Turnstile
    /// <summary>
    /// Đặt theme cho widget Cloudflare Turnstile chưa được vẽ.
    /// Widget đã vẽ (đã có iframe) thì giữ màu cũ tới lần tải trang sau, vì Turnstile không đổi theme sau khi vẽ.
    /// </summary>
    function syncTurnstile(theme) {
        var widgets = document.querySelectorAll('.cf-turnstile');

        for (var i = 0; i < widgets.length; i++) {
            if (!widgets[i].querySelector('iframe')) {
                widgets[i].setAttribute('data-theme', theme);
            }
        }
    }
    //#endregion

    //#region Actions
    /// <summary>Áp dụng theme lên trang rồi đồng bộ các thành phần phụ thuộc.</summary>
    function render(theme) {
        var applied = core.apply(theme);
        syncButtons(applied);
        syncTurnstile(applied);
        return applied;
    }

    /// <summary>Đảo theme đang hiển thị và ghi nhớ lựa chọn.</summary>
    function toggle() {
        var next = core.current() === core.themes.dark ? core.themes.light : core.themes.dark;
        core.save(next);
        render(next);
    }
    //#endregion

    //#region Events
    // Uỷ quyền sự kiện: một listener dùng chung cho mọi nút chuyển theme trên trang
    document.addEventListener('click', function (event) {
        var target = event.target;
        if (target && target.closest && target.closest(TOGGLE_SELECTOR)) {
            toggle();
        }
    });

    // Người dùng đổi theme ở tab khác -> tab này đổi theo
    window.addEventListener('storage', function (event) {
        if (event.key === core.storageKey) {
            render(core.normalize(event.newValue));
        }
    });
    //#endregion

    render(core.current());
})(window, document);
