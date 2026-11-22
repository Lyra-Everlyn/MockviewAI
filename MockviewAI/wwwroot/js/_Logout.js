//#region Logout
/// <summary>
/// Đăng xuất: xoá dữ liệu tạm và CV lưu trên trình duyệt rồi gửi form.
/// </summary>
//#endregion
function onClickLogout(event) {
    event.preventDefault();
    sessionStorage.clear();
    clearStoredCvs();

    const logoutForm = document.getElementById('logoutForm');
    if (logoutForm) {
        logoutForm.submit();
    } else {
        console.error("Not found form logout with ID 'logoutForm'");
    }
}

function clearStoredCvs() {
    try {
        const keys = [];
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && key.indexOf('mockview.cv.') === 0) keys.push(key);
        }
        keys.forEach(key => localStorage.removeItem(key));
    } catch (e) { }
}
