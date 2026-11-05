document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('createSessionForm');
    if (!form) return;

    const tape = document.getElementById('preview-tape');
    const previewText = document.getElementById('preview-text');
    const modeLabel = document.getElementById('preview-mode');
    const startButton = document.getElementById('startInterviewButton');

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
        modeLabel.textContent = modeText;
        modeLabel.classList.toggle('is-live', isLive);
        previewText.textContent = `${count} câu với ${name} · ${modeText} · khoảng ${count * 2}–${count * 3} phút · câu đầu luôn là giới thiệu bản thân`;
    }

    form.addEventListener('change', updatePreview);
    form.addEventListener('submit', event => {
        if (event.defaultPrevented || !form.checkValidity()) return;
        // Only disable on valid submit; no second submit handler or overlay is needed.
        // The shared _SubmitLoading.js handles overlay for forms with data-loading-text.
    });
    updatePreview();
});
