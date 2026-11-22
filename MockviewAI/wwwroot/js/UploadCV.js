//#region UploadCV
/// <summary>
/// [USER CV] Chọn / kéo thả CV, kiểm tra nhanh. Đủ số CV thì khoá form.
/// </summary>
//#endregion
document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('CvFile');
    const zone = document.getElementById('cvDropzone');
    const form = document.getElementById('cvUploadForm');
    if (!input || !zone || !form) return;

    //#region Config
    const selected = document.getElementById('cvSelected');
    const name = document.getElementById('cvSelectedName');
    const size = document.getElementById('cvSelectedSize');
    const error = document.getElementById('cvFileError');
    const remove = document.getElementById('cvRemoveFile');
    const MAX_BYTES = 10 * 1024 * 1024;
    const ALLOWED_EXT = /\.pdf$/i;
    //#endregion

    //#region Limit
    const limitNotice = document.getElementById('cvLimitNotice');
    const submitButton = form.querySelector('button[type="submit"]');
    const isFull = !!(window.MockviewCv && window.MockviewCv.hasOwner && window.MockviewCv.isFull());
    if (isFull) {
        if (limitNotice) limitNotice.hidden = false;
        if (submitButton) submitButton.disabled = true;
        input.disabled = true;
    }
    //#endregion

    //#region Validation
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

        const wrongType = file.type && file.type !== 'application/pdf';
        if (!ALLOWED_EXT.test(file.name) || wrongType) {
            displayError('Chỉ hỗ trợ CV định dạng PDF.');
        } else if (file.size > MAX_BYTES) {
            displayError('Tệp vượt quá 10 MB. Vui lòng chọn CV nhỏ hơn.');
        } else if (file.size === 0) {
            displayError('Tệp rỗng. Vui lòng chọn một CV khác.');
        }
    }
    //#endregion

    //#region Events
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
        if (!files?.length || isFull) return;
        try {
            input.files = files;
            update();
        } catch {
            displayError('Không thể nhận tệp kéo thả. Vui lòng nhấn “Chọn tệp CV”.');
        }
    });
    form.addEventListener('submit', event => {
        if (isFull) {
            event.preventDefault();
            return;
        }
        update();
        if (!form.checkValidity()) {
            event.preventDefault();
            input.reportValidity();
        }
    });
    //#endregion
});
