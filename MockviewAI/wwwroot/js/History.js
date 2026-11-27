//#region History
/// <summary>
/// [HISTORY]
/// Trang Lịch sử: tìm theo tên vị trí (bỏ dấu) và lọc theo trạng thái ngay trên trình duyệt, không gọi server.
/// </summary>
//#endregion
document.addEventListener('DOMContentLoaded', () => {
    const search = document.getElementById('history-search');
    const entries = Array.from(document.querySelectorAll('.history-entry'));
    const filters = Array.from(document.querySelectorAll('.history-filter'));
    const count = document.getElementById('history-count');
    const empty = document.getElementById('history-no-results');
    const clear = document.getElementById('history-clear');
    if (!search || !entries.length) return;

    let activeFilter = 'all';

    // "Lập trình viên" và "lap trinh vien" khớp nhau
    const normalize = text => (text || '').normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd').replace(/Đ/g, 'd')
        .trim().toLocaleLowerCase('vi');

    const matchesFilter = entry => {
        if (activeFilter === 'all') return true;
        if (activeFilter === 'live') return entry.dataset.live === 'true';
        return entry.dataset.state === activeFilter;
    };

    //#region Hiển thị
    const render = () => {
        const query = normalize(search.value);
        let visible = 0;
        entries.forEach(entry => {
            const match = matchesFilter(entry) && normalize(entry.dataset.role).includes(query);
            entry.hidden = !match;
            if (match) visible++;
        });
        if (count) {
            count.textContent = visible === entries.length
                ? `${entries.length} buổi luyện`
                : `${visible} / ${entries.length} buổi luyện`;
        }
        if (empty) empty.hidden = visible !== 0;
    };

    const setFilter = key => {
        activeFilter = key;
        filters.forEach(button => {
            const on = button.dataset.filter === key;
            button.classList.toggle('is-active', on);
            button.setAttribute('aria-pressed', String(on));
        });
        render();
    };
    //#endregion

    //#region Sự kiện
    search.addEventListener('input', render);
    filters.forEach(button => button.addEventListener('click', () => setFilter(button.dataset.filter)));
    clear?.addEventListener('click', () => {
        search.value = '';
        setFilter('all');
        search.focus();
    });
    //#endregion
});
