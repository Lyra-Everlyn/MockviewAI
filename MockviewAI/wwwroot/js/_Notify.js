document.addEventListener('DOMContentLoaded', () => {
    if (typeof Swal === 'undefined' || !window.SystemAlerts) return;
    
    const {success, error, info} = window.SystemAlerts;
    const common = {confirmButtonColor: '#5565ed', customClass: {popup: 'app-alert-dialog'}};

    if (success) {
        Swal.fire({...common, 
            icon: 'success', 
            title: 'Thành công!', 
            text: success,
            position: 'center', 
            toast: false, 
            showConfirmButton: false, 
            timer: 3000,
            timerProgressBar: true
        });
    } else if (error) {
        Swal.fire({...common, 
            icon: 'error', 
            title: 'Đã xảy ra lỗi', 
            text: error,
            position: 'center', 
            confirmButtonText: 'Đóng'});
    } else if (info) {
        Swal.fire({...common, 
            icon: 'info', 
            title: 'Thông báo', 
            text: info,
            position: 'center', 
            confirmButtonText: 'Đã hiểu'});
    }
});