/* Upload CV: progressive enhancement only. Real file validation must also run on the server. */
document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('CvFile');
    const zone = document.getElementById('cvDropzone');
    const form = document.getElementById('cvUploadForm');
    if (!input || !zone || !form) return; // review state has no file picker

    const selected = document.getElementById('cvSelected');
    const name = document.getElementById('cvSelectedName');
    const size = document.getElementById('cvSelectedSize');
    const error = document.getElementById('cvFileError');
    const remove = document.getElementById('cvRemoveFile');
    const MAX_BYTES = 10 * 1024 * 1024; // Align this value with your backend request/file limit.
    const ALLOWED = /\.(pdf|jpe?g|png)$/i;

    function displayError(message) {
        error.textContent = message || '';
        error.hidden = !message;
        input.setCustomValidity(message || '');
    }
    function update() {
        const file = input.files?.[0];
        displayError('');
        selected.hidden = !file;
        if (!file) return;
        name.textContent = file.name;
        size.textContent = file.size < 1024 * 1024
            ? `${(file.size / 1024).toFixed(1)} KB`
            : `${(file.size / 1024 / 1024).toFixed(2)} MB`;
        if (!ALLOWED.test(file.name)) {
            displayError('Chỉ hỗ trợ định dạng PDF, JPG, JPEG hoặc PNG.');
        } else if (file.size > MAX_BYTES) {
            displayError('Tệp vượt quá 10 MB. Vui lòng chọn CV nhỏ hơn.');
        } else if (file.size === 0) {
            displayError('Tệp rỗng. Vui lòng chọn một CV khác.');
        }
    }

    input.addEventListener('change', update);
    remove.addEventListener('click', () => {
        input.value = '';
        update();
        input.focus();
    });
    ['dragenter', 'dragover'].forEach(eventName => zone.addEventListener(eventName, event => {
        event.preventDefault();
        zone.classList.add('is-dragging');
    }));
    ['dragleave', 'drop'].forEach(eventName => zone.addEventListener(eventName, event => {
        event.preventDefault();
        zone.classList.remove('is-dragging');
    }));
    zone.addEventListener('drop', event => {
        const files = event.dataTransfer?.files;
        if (!files?.length) return;
        // Assign only actual user-selected files to the native file input.
        try {
            input.files = files;
            update();
        } catch {
            displayError('Không thể nhận tệp kéo thả. Vui lòng nhấn “Chọn tệp CV”.');
        }
    });
    form.addEventListener('submit', event => {
        update();
        if (!form.checkValidity()) {
            event.preventDefault();
            input.reportValidity();
        }
    });
});
