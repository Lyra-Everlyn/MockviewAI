// NOTE: This script is used to handle input errors in forms. It provides functions to show and clear error messages, as well as to toggle password visibility.


// 1. Display error under input box
function showError(input, message) {
    const group = input.closest(".input-group");
    input.classList.add("input-error");
    const error = document.createElement("div");
    error.className = "input-error-message";
    error.innerHTML = `
        <i class="fas fa-exclamation-circle"></i>
        ${message}
    `;
    group.appendChild(error);
}

// 2. Delete error under input box
function clearErrors(scope = document) {
    scope.querySelectorAll(".input-error").forEach(el => el.classList.remove("input-error"));
    scope.querySelectorAll(".input-error-message").forEach(el => el.remove());
}

// 3. Delete error when typing new in input box
document.querySelectorAll("input").forEach(input => {
    input.addEventListener("input", function () {
        this.classList.remove("input-error");
        const group = this.closest(".input-group");

        if (group) {
            const error = group.querySelector(".input-error-message");
            if (error) error.remove();
        }
    });
});

// 4. Toggle hide/display password
function togglePass(inputId, icon) {
    const input = document.getElementById(inputId);
    if (input.type === "password") {
        input.type = "text";
        icon.classList.replace('far', 'fas');
        icon.classList.replace('fa-eye', 'fa-eye-slash');
    }
    else {
        input.type = "password";
        icon.classList.replace('fas', 'far');
        icon.classList.replace('fa-eye-slash', 'fa-eye');
    }
}