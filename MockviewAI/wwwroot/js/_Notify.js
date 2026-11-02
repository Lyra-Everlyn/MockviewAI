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

        const infoMsg = window.SystemAlerts.info;
        if (infoMsg) {
            Swal.fire({
                icon: 'info',
                title: 'Notify',
                text: infoMsg,
                confirmButtonText: 'Ok',
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
