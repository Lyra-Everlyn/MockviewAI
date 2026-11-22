//#region _TogglePassword
/// <summary>
/// [AUTH] 
/// Nút ẩn / hiện mật khẩu. Dùng: &lt;button data-password-toggle="idOCuaInput"&gt;&lt;i class="fa-regular fa-eye"&gt;&lt;/i&gt;&lt;/button&gt;.
/// </summary>
//#endregion
document.addEventListener("DOMContentLoaded", () => {
    const toggleButtons = document.querySelectorAll(
        "[data-password-toggle]"
    );

    toggleButtons.forEach(button => {
        const inputId = button.dataset.passwordToggle;
        const input = document.getElementById(inputId);
        const icon = button.querySelector("i");

        if (!input || !icon) return;

        button.addEventListener("click", () => {
            const isPassword = input.type === "password";

            input.type = isPassword ? "text" : "password";

            // Icon Font Awesome
            icon.classList.toggle("fa-eye", !isPassword);
            icon.classList.toggle("fa-eye-slash", isPassword);

            // Trợ năng cho trình đọc màn hình
            button.setAttribute(
                "aria-label",
                isPassword ? "Ẩn mật khẩu" : "Hiển thị mật khẩu"
            );

            button.setAttribute(
                "aria-pressed",
                String(isPassword)
            );
        });
    });
});
