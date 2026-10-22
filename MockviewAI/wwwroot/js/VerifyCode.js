// Ô nhập mã OTP 6 số (dùng chung cho Xác thực đăng ký và Quên mật khẩu).
// 22/10: sửa lỗi bộ gõ tiếng Việt (Unikey/EVKey chế độ VIE). Bộ gõ nhớ cả "từ" đang gõ và viết lại cả từ
// mỗi lần gõ phím, nên khi code tự nhảy sang ô kế tiếp, ô mới nhận "11", "111"... Cách xử lý:
//  - Mỗi ô chỉ giữ CHỮ SỐ CUỐI CÙNG (luôn là phím vừa gõ), bỏ mọi ký tự không phải số.
//  - Nếu một ô nhận đủ 6 số (dán, điện thoại tự điền mã từ SMS/email) thì chia đều vào 6 ô.
//  - Chuẩn hoá cả khi bộ gõ kết thúc ghép chữ (compositionend).
document.addEventListener("DOMContentLoaded", function () {
    const inputs = Array.from(document.querySelectorAll('.otp-input'));
    const finalCodeInput = document.getElementById('finalCode');
    const form = document.getElementById('verifyForm');

    if (!form || inputs.length === 0) return;

    function fillFrom(start, digits) {
        let i = start;
        for (const d of digits) {
            if (i >= inputs.length) break;
            inputs[i].value = d;
            i++;
        }
        focusBox(Math.min(i, inputs.length - 1));
    }

    function focusBox(i) {
        const box = inputs[i];
        if (!box) return;
        box.focus();
        box.select();
    }

    function normalize(input, index) {
        const digits = input.value.replace(/[^0-9]/g, '');

        if (digits.length >= inputs.length) {
            // Đủ cả mã (dán / tự điền): điền từ ô đầu tiên
            fillFrom(0, digits.slice(0, inputs.length));
            return;
        }

        input.value = digits.slice(-1);   // chỉ giữ số vừa gõ
        if (input.value !== '' && index < inputs.length - 1) focusBox(index + 1);
    }

    inputs.forEach((input, index) => {
        input.addEventListener('input', function (e) {
            if (e.isComposing) return;   // đang ghép chữ: đợi compositionend
            normalize(this, index);
        });

        input.addEventListener('compositionend', function () {
            normalize(this, index);
        });

        input.addEventListener('focus', function () { this.select(); });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && this.value === '' && index > 0) {
                e.preventDefault();
                inputs[index - 1].value = '';
                focusBox(index - 1);
            } else if (e.key === 'ArrowLeft' && index > 0) {
                e.preventDefault();
                focusBox(index - 1);
            } else if (e.key === 'ArrowRight' && index < inputs.length - 1) {
                e.preventDefault();
                focusBox(index + 1);
            }
        });

        // Dán cả mã vào bất kỳ ô nào
        input.addEventListener('paste', function (e) {
            e.preventDefault();
            const pasted = (e.clipboardData || window.clipboardData).getData('text').replace(/[^0-9]/g, '');
            if (!pasted) return;
            if (pasted.length >= inputs.length) fillFrom(0, pasted.slice(0, inputs.length));
            else fillFrom(index, pasted);
        });
    });

    // Ghép 6 ô thành mã gửi lên server; thiếu số thì báo ngay, không gửi
    form.addEventListener('submit', function (e) {
        const code = inputs.map(input => input.value.replace(/[^0-9]/g, '').slice(-1)).join('');
        if (code.length !== inputs.length) {
            e.preventDefault();
            const firstEmpty = inputs.findIndex(input => input.value === '');
            focusBox(firstEmpty >= 0 ? firstEmpty : 0);
            return;
        }
        finalCodeInput.value = code;
        const button = form.querySelector('button[type="submit"]');
        if (button) {
            button.disabled = true;
            button.textContent = 'Verifying...';
        }
    });
});
