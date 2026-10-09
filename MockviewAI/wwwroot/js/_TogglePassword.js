// Toggle hide/display password
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