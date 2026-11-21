//#region ScoringOverlay
/// <summary>
/// [AI-INTERVIEW] 
/// Màn hình chờ khi chấm điểm: các bước đổi theo thời gian, đồng hồ đếm, cảnh báo khi rời trang.
/// </summary>
//#endregion
(function (window, document) {
    'use strict';

    //#region Config
    // Bước hiện ra khi đã chờ đủ số giây tương ứng
    const STEPS = [
        { at: 0, icon: 'fa-solid fa-list-check', text: 'Đang tổng hợp các câu trả lời của bạn' },
        { at: 4, icon: 'fa-solid fa-user-tie', text: 'Nhà tuyển dụng đang đọc lại từng câu' },
        { at: 12, icon: 'fa-solid fa-scale-balanced', text: 'Đang đối chiếu với các ý chính cần có' },
        { at: 25, icon: 'fa-solid fa-pen-nib', text: 'Đang viết nhận xét và gợi ý cải thiện' },
        { at: 45, icon: 'fa-solid fa-hourglass-half', text: 'Sắp xong, AI đang hoàn thiện báo cáo' }
    ];
    //#endregion

    let root = null;
    let timer = null;
    let startedAt = 0;

    //#region DOM
    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text) node.textContent = text;
        return node;
    }

    function icon(classes) {
        const i = el('i', classes);
        i.setAttribute('aria-hidden', 'true');
        return i;
    }

    function build() {
        root = el('div', 'scoring-overlay');
        root.setAttribute('role', 'dialog');
        root.setAttribute('aria-modal', 'true');
        root.setAttribute('aria-labelledby', 'scoring-overlay-title');

        const box = el('div', 'scoring-overlay__box');
        box.tabIndex = -1;
        const spinner = el('div', 'scoring-overlay__spinner');
        spinner.setAttribute('aria-hidden', 'true');

        const title = el('h2', 'scoring-overlay__title', 'Đang chấm điểm buổi phỏng vấn');
        title.id = 'scoring-overlay-title';
        const hint = el('p', 'scoring-overlay__hint', 'Thường mất 20–60 giây. Vui lòng không đóng hoặc tải lại trang.');

        const list = el('ol', 'scoring-overlay__steps');
        list.setAttribute('aria-live', 'polite');
        STEPS.forEach(step => {
            const li = el('li', 'scoring-overlay__step');
            li.append(icon(step.icon), el('span', null, step.text));
            li.hidden = true;
            list.append(li);
        });

        const elapsed = el('p', 'scoring-overlay__elapsed', '0 giây');
        elapsed.setAttribute('aria-hidden', 'true');

        box.append(spinner, title, hint, list, elapsed);
        root.append(box);
        document.body.append(root);
    }
    //#endregion

    //#region Progress
    function tick() {
        const seconds = Math.floor((Date.now() - startedAt) / 1000);
        root.querySelector('.scoring-overlay__elapsed').textContent = seconds + ' giây';

        const items = root.querySelectorAll('.scoring-overlay__step');
        let current = 0;
        STEPS.forEach((step, i) => { if (seconds >= step.at) current = i; });
        items.forEach((li, i) => {
            li.hidden = i > current;
            li.classList.toggle('is-done', i < current);
            li.classList.toggle('is-current', i === current);
        });
    }

    function warnBeforeLeave(event) {
        event.preventDefault();
        event.returnValue = '';
    }
    //#endregion

    //#region Public API
    function show() {
        if (!root) build();
        startedAt = Date.now();
        root.hidden = false;
        document.body.classList.add('is-scoring');
        tick();
        clearInterval(timer);
        timer = setInterval(tick, 1000);
        window.addEventListener('beforeunload', warnBeforeLeave);
        root.querySelector('.scoring-overlay__box').focus();
    }

    function hide() {
        clearInterval(timer);
        timer = null;
        window.removeEventListener('beforeunload', warnBeforeLeave);
        document.body.classList.remove('is-scoring');
        if (root) root.hidden = true;
    }

    /// <summary>Gọi ngay trước khi chuyển sang trang kết quả, để không bật cảnh báo rời trang.</summary>
    function release() {
        window.removeEventListener('beforeunload', warnBeforeLeave);
    }

    window.MockviewScoringOverlay = { show: show, hide: hide, release: release };
    //#endregion
})(window, document);
