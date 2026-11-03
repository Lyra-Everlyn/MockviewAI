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

            // Toggle password visibility
            input.type = isPassword ? "text" : "password";

            // Update Font Awesome icon
            icon.classList.toggle("fa-eye", !isPassword);
            icon.classList.toggle("fa-eye-slash", isPassword);

            // Update accessibility attributes
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
