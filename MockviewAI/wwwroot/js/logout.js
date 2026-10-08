function onClickLogout(event) {
    event.preventDefault();
    sessionStorage.clear();

    const logoutForm = document.getElementById('logoutForm');
    if (logoutForm) {
        logoutForm.submit();
    } else {
        console.error("Not found form logout with ID 'logoutForm'");
    }
}