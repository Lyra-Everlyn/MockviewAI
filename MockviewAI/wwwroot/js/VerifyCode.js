// Generic six-digit OTP behavior for verification forms.
// The shared _SubmitLoading.js alone manages the submitting state/overlay.
document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('verifyForm');
    const finalCode = document.getElementById('finalCode');
    if (!form || !finalCode) return;

    const inputs = [...form.querySelectorAll('.otp-input')];
    const message = document.getElementById('otp-message');
    if (!inputs.length) return;

    const digitsOnly = value => value.replace(/[^0-9]/g, '');
    const focusInput = index => {
        const input = inputs[index];
        if (!input) return;
        input.focus();
        input.select();
    };
    const clearMessage = () => {
        if (message) message.textContent = '';
        inputs.forEach(input => input.removeAttribute('aria-invalid'));
    };
    const fill = (start, raw) => {
        const digits = digitsOnly(raw);
        if (!digits) return;
        const from = digits.length >= inputs.length ? 0 : start;
        digits.slice(0, inputs.length - from).split('').forEach((digit, offset) => {
            inputs[from + offset].value = digit;
        });
        focusInput(Math.min(from + digits.length, inputs.length - 1));
        clearMessage();
    };

    inputs.forEach((input, index) => {
        input.addEventListener('input', event => {
            if (event.isComposing) return;
            const digits = digitsOnly(input.value);
            if (digits.length > 1) {
                input.value = '';
                fill(index, digits);
            } else {
                input.value = digits;
                if (digits && index < inputs.length - 1) focusInput(index + 1);
                clearMessage();
            }
        });
        input.addEventListener('compositionend', () => {
            const digits = digitsOnly(input.value);
            if (digits.length > 1) { input.value = ''; fill(index, digits); }
            else { input.value = digits; if (digits && index < inputs.length - 1) focusInput(index + 1); }
        });
        input.addEventListener('focus', () => input.select());
        input.addEventListener('keydown', event => {
            if (event.key === 'Backspace' && !input.value && index > 0) {
                event.preventDefault();
                inputs[index - 1].value = '';
                focusInput(index - 1);
            } else if (event.key === 'ArrowLeft' && index > 0) {
                event.preventDefault(); focusInput(index - 1);
            } else if (event.key === 'ArrowRight' && index < inputs.length - 1) {
                event.preventDefault(); focusInput(index + 1);
            }
        });
        input.addEventListener('paste', event => {
            event.preventDefault();
            const raw = event.clipboardData?.getData('text') ?? '';
            if (!digitsOnly(raw)) return;
            fill(index, raw);
        });
    });

    // Assemble code before the shared loading submit listener executes.
    form.addEventListener('submit', event => {
        const code = inputs.map(input => digitsOnly(input.value).slice(0, 1)).join('');
        if (code.length !== inputs.length) {
            event.preventDefault();
            if (message) message.textContent = 'Vui lòng nhập đủ 6 chữ số.';
            const firstEmpty = inputs.findIndex(input => !digitsOnly(input.value));
            if (firstEmpty >= 0) {
                inputs[firstEmpty].setAttribute('aria-invalid', 'true');
                focusInput(firstEmpty);
            }
            return;
        }
        finalCode.value = code;
    });
});
