//#region LogoutConfirm
/// <summary>
/// Trang xác nhận đăng xuất (mở từ landing page): trước khi gửi form, xoá dữ liệu tạm
/// và CV lưu trên trình duyệt như nút Đăng xuất trong ứng dụng (xem _Logout.js).
/// </summary>
//#endregion
(function () {
    const form = document.getElementById('logoutConfirmForm');
    if (!form) return;
    form.addEventListener('submit', function () {
        try { sessionStorage.clear(); } catch (e) { }
        if (typeof clearStoredCvs === 'function') clearStoredCvs();
    });
})();
