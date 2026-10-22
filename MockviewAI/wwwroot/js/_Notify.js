document.addEventListener('DOMContentLoaded', () => {
    if (window.SystemAlerts && typeof Swal !== 'undefined') {
        const successMsg = window.SystemAlerts.success;
        const errorMsg = window.SystemAlerts.error;

        if (successMsg) {
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: successMsg,
                timer: 3000,
                showConfirmButton: false,
                toast: true,
                position: 'bottom-right'
            });
        }

        // Thông báo quan trọng cần người dùng đọc (vd "Đã gửi mã tới email"): hiện giữa màn hình, có nút OK
        const infoMsg = window.SystemAlerts.info;
        if (infoMsg) {
            Swal.fire({
                icon: 'info',
                title: 'Thông báo',
                text: infoMsg,
                confirmButtonText: 'Đã hiểu',
                confirmButtonColor: '#4361ee'
            });
        }

        if (errorMsg) {
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: errorMsg,
                confirmButtonColor: '#4361ee'
            });
        }
    }
});
