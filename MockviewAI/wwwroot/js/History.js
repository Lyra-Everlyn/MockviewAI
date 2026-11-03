// History page: local, role-name-only search. No server-side request.
document.addEventListener('DOMContentLoaded', () => {
    const search = document.getElementById('history-search');
    const entries = Array.from(document.querySelectorAll('.history-entry'));
    const count = document.getElementById('history-count');
    const empty = document.getElementById('history-no-results');
    const clear = document.getElementById('history-clear');
    if (!search || !entries.length) return;

    const normalize = text => (text || '').normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd').replace(/Đ/g, 'd')
        .trim().toLocaleLowerCase('vi');

    const render = () => {
        const query = normalize(search.value);
        let visible = 0;
        entries.forEach(entry => {
            const match = normalize(entry.dataset.role).includes(query);
            entry.hidden = !match;
            if (match) visible++;
        });
        if (count) {
            count.textContent = query
                ? `${visible} / ${entries.length} buổi phù hợp với tìm kiếm.`
                : `${entries.length} buổi luyện · Chọn một buổi để xem chi tiết.`;
        }
        if (empty) empty.hidden = visible !== 0;
    };

    search.addEventListener('input', render);
    clear?.addEventListener('click', () => {
        search.value = '';
        render();
        search.focus();
    });
});
