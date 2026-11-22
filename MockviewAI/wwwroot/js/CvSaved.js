//#region CvSaved
/// <summary>
/// [USER CV] 
// Lưu CV vừa chốt vào trình duyệt rồi chuyển về /CV. Đủ số CV thì báo lỗi, không ghi đè.
/// </summary>
//#endregion
document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    const status = document.getElementById('cvSavedStatus');
    const errorBox = document.getElementById('cvSavedError');
    const errorText = document.getElementById('cvSavedErrorText');
    const actions = document.getElementById('cvSavedActions');

    function fail(message) {
        status.hidden = true;
        errorText.textContent = message;
        errorBox.hidden = false;
        actions.hidden = false;
    }

    let data;
    try {
        data = JSON.parse(document.getElementById('cvSavedData').textContent || '{}');
    } catch (e) {
        fail('Dữ liệu CV không hợp lệ. Vui lòng tải CV lên lại.');
        return;
    }

    if (!window.MockviewCv) {
        fail('Không tải được bộ lưu CV. Vui lòng tải lại trang.');
        return;
    }

    const result = window.MockviewCv.add(data);
    if (result.ok) {
        try { sessionStorage.setItem('mockview.cv.flash', 'saved'); } catch (e) { }
        window.location.replace('/CV');
        return;
    }

    const messages = {
        full: 'Bạn đã lưu đủ ' + window.MockviewCv.max + ' CV. Hãy xoá bớt một CV trong "CV của tôi" rồi tải lên lại.',
        storage: 'Trình duyệt không cho lưu dữ liệu (chế độ ẩn danh hoặc đã chặn bộ nhớ trang web). Hãy dùng cửa sổ thường rồi thử lại.',
        no_owner: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
    };
    fail(messages[result.reason] || 'Không lưu được CV. Vui lòng thử lại.');
});
