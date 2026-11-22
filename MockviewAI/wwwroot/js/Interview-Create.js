document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('createSessionForm');
    if (!form) return;

    const tape = document.getElementById('preview-tape');
    const previewText = document.getElementById('preview-text');
    const modeLabel = document.getElementById('preview-mode');

    //#region CV
    // [INTERVIEWER] CV mặc định trên trình duyệt: có thì hiện ô "Hỏi theo CV của tôi", gửi kèm khi bắt đầu
    const cvPicker = document.getElementById('cvPicker');
    const cvPickerName = document.getElementById('cvPickerName');
    const cvPickerEmpty = document.getElementById('cvPickerEmpty');
    const useCv = document.getElementById('useCv');
    const cvJson = document.getElementById('cvJson');
    const defaultCv = window.MockviewCv && window.MockviewCv.hasOwner ? window.MockviewCv.getDefault() : null;

    if (defaultCv) {
        cvPickerName.textContent = 'Dùng CV mặc định: ' + window.MockviewCv.titleOf(defaultCv) +
            '. Nhà tuyển dụng sẽ hỏi thêm về dự án và kinh nghiệm trong CV.';
        cvPicker.hidden = false;
    } else if (cvPickerEmpty) {
        cvPickerEmpty.hidden = false;
    }
    //#endregion

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
        modeLabel.textContent = modeText;
        modeLabel.classList.toggle('is-live', isLive);
        previewText.textContent = `${count} câu với ${name} · ${modeText}${cvText} · khoảng ${count * 2}–${count * 3} phút · câu đầu luôn là giới thiệu bản thân`;
    }

    form.addEventListener('change', updatePreview);
    form.addEventListener('submit', event => {
        if (event.defaultPrevented || !form.checkValidity()) return;
        // _SubmitLoading.js lo phần khoá nút và màn hình chờ
        cvJson.value = defaultCv && useCv.checked ? JSON.stringify(defaultCv.data) : '';
    });
    updatePreview();
});
