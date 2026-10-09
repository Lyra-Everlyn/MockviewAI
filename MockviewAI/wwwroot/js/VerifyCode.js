document.addEventListener("DOMContentLoaded", function () {
    const inputs = document.querySelectorAll('.otp-input');
    const finalCodeInput = document.getElementById('finalCode');
    const form = document.getElementById('verifyForm');

    if (!form || inputs.length === 0) return;

    inputs.forEach((input, index) => {
        // Number only input
        input.addEventListener('input', function (e) {
            this.value = this.value.replace(/[^0-9]/g, '');

            if (this.value !== '') {
                if (index < inputs.length - 1) {
                    inputs[index + 1].focus();
                }
            }
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && this.value === '') {
                if (index > 0) {
                    inputs[index - 1].focus();
                }
            }
        });

        // Support pasting the entire code into the first input
        input.addEventListener('paste', function (e) {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text').replace(/[^0-9]/g, '').slice(0, 6);
            for (let i = 0; i < pastedData.length; i++) {
                if (i < inputs.length) {
                    inputs[i].value = pastedData[i];
                    if (i < inputs.length - 1) inputs[i + 1].focus();
                }
            }
        });
    });

    // Merge the values of all inputs into the hidden input before form submission
    form.addEventListener('submit', function () {
        let code = '';
        inputs.forEach(input => code += input.value);
        finalCodeInput.value = code;
    });
});