//#region Interview-Create
/// <summary>
/// [AI-INTERVIEW] 
/// Trang Tạo buổi luyện: xem trước dải băng số câu, chế độ, thời lượng; gửi kèm CV mặc định nếu người dùng chọn.
/// </summary>
//#endregion
document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('createSessionForm');
    if (!form) return;

    const tape = document.getElementById('preview-tape');
    const previewText = document.getElementById('preview-text');
    const modeLabel = document.getElementById('preview-mode');

    //#region [INTERVIEWER] CV mặc định
    // Có CV mặc định trên trình duyệt thì hiện ô "Hỏi theo CV của tôi", gửi kèm khi bắt đầu
    const cvPicker = document.getElementById('cvPicker');
    const cvPickerName = document.getElementById('cvPickerName');
    const cvPickerEmpty = document.getElementById('cvPickerEmpty');
    const useCv = document.getElementById('useCv');
    const cvJson = document.getElementById('cvJson');
    const defaultCv = window.MockviewCv && window.MockviewCv.hasOwner ? window.MockviewCv.getDefault() : null;
    const defaultOverlayText = form.dataset.loadingOverlay;
    const cvOverlayText = 'Đang soạn câu hỏi theo CV của bạn (khoảng 15–30 giây)...';
    const roleOverlayText = 'Đang soạn câu hỏi theo vị trí ứng tuyển (khoảng 15–30 giây)...';

    if (defaultCv) {
        cvPickerName.textContent = 'Dùng CV mặc định: ' + window.MockviewCv.titleOf(defaultCv) +
            '. Một số câu hỏi sẽ được soạn theo dự án và kinh nghiệm trong CV.';
        cvPicker.hidden = false;
    } else if (cvPickerEmpty) {
        cvPickerEmpty.hidden = false;
    }
    //#endregion

    //#region [IT-ROLES] Gợi ý vị trí từ CV
    // AI đọc CV mặc định rồi chọn sẵn vị trí phù hợp; người dùng đã tự chọn thì không ghi đè. Kết quả nhớ theo CV trong phiên trình duyệt.
    const roleHint = document.getElementById('roleSuggestHint');
    let userPickedRole = false;
    form.querySelectorAll('input[name="position"]').forEach(input => input.addEventListener('change', event => {
        if (event.isTrusted) userPickedRole = true;
    }));

    function applySuggestion(s) {
        const input = s && form.querySelector(`input[name="position"][value="${CSS.escape(s.key)}"]`);
        if (!input) return;
        if (!userPickedRole) {
            input.checked = true;
            updatePreview();
        }
        roleHint.textContent = `Gợi ý từ CV: ${s.name}${s.reason ? ' — ' + s.reason : ''} Bạn có thể chọn vị trí khác.`;
        roleHint.hidden = false;
    }

    async function suggestRole() {
        if (!defaultCv || !roleHint) return;
        const cacheKey = 'mockview:role:' + (defaultCv.id || '');
        try {
            const cached = sessionStorage.getItem(cacheKey);
            if (cached) { applySuggestion(JSON.parse(cached)); return; }
        } catch { /* sessionStorage bị chặn: gọi lại server */ }

        roleHint.textContent = 'Đang đọc CV để gợi ý vị trí phù hợp...';
        roleHint.hidden = false;
        try {
            const body = new FormData();
            body.append('cvJson', JSON.stringify(defaultCv.data));
            body.append('__RequestVerificationToken', form.querySelector('input[name="__RequestVerificationToken"]').value);
            const response = await fetch('/Interview/SuggestRole', { method: 'POST', body });
            if (!response.ok) throw new Error('HTTP ' + response.status);
            const suggestion = await response.json();
            try { sessionStorage.setItem(cacheKey, JSON.stringify(suggestion)); } catch { /* bỏ qua */ }
            applySuggestion(suggestion);
        } catch {
            roleHint.hidden = true;   // gợi ý lỗi: người dùng tự chọn như bình thường
        }
    }
    //#endregion

    //#region Xem trước
    function selected(name) {
        return form.querySelector(`input[name="${name}"]:checked`);
    }

    function updatePreview() {
        const interviewer = selected('interviewer');
        const countInput = selected('questionCount');
        const modeInput = selected('mode');
        if (!interviewer || !countInput || !modeInput) return;

        const count = Number(countInput.value);
        const name = interviewer.dataset.name || interviewer.value;
        const isLive = modeInput.value === 'live';

        tape.replaceChildren();
        const fragment = document.createDocumentFragment();
        for (let index = 0; index < count; index++) {
            const segment = document.createElement('span');
            segment.className = 'interview-create-tape__segment';
            fragment.appendChild(segment);
        }
        tape.appendChild(fragment);

        const modeText = isLive ? 'Thực chiến' : 'Luyện tập';
        const cvText = defaultCv && useCv.checked ? ' · hỏi theo CV' : '';
        const position = selected('position');
        const roleText = position ? ' · ' + (position.dataset.name || position.value) : '';
        // [IT-ROLES] Nam + vị trí chưa có bộ đề kỹ thuật -> AI soạn câu theo vị trí
        const aiRole = interviewer.value === 'nam' && position && position.dataset.aiTech === '1';
        // [CV-QUESTIONS] Soạn câu theo CV / theo vị trí mất 15–30 giây -> đổi chữ màn hình chờ (_SubmitLoading.js đọc lúc bấm gửi)
        form.dataset.loadingOverlay = cvText ? cvOverlayText : aiRole ? roleOverlayText : defaultOverlayText;
        modeLabel.textContent = modeText;
        modeLabel.classList.toggle('is-live', isLive);
        previewText.textContent = `${count} câu với ${name}${roleText} · ${modeText}${cvText} · khoảng ${count * 2}–${count * 3} phút · câu đầu luôn là giới thiệu bản thân`;
    }
    //#endregion

    //#region Sự kiện
    form.addEventListener('change', updatePreview);
    form.addEventListener('submit', event => {
        if (event.defaultPrevented || !form.checkValidity()) return;
        // _SubmitLoading.js lo phần khoá nút và màn hình chờ
        cvJson.value = defaultCv && useCv.checked ? JSON.stringify(defaultCv.data) : '';
    });
    updatePreview();
    suggestRole();
    //#endregion
});
