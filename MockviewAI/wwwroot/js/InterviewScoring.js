//#region InterviewScoring
/// <summary>
/// [AI-SCORING]
/// Trang chờ chấm điểm (Views/Interview/Scoring.cshtml): server vừa thử chấm và AI đang bận, nên trang bắt đầu bằng đếm ngược
/// rồi tự gọi /Interview/Finish bằng AI, tối đa 3 lần (giống InterviewRoom.js). Người dùng bấm "Chấm lại" (ngay)
/// hoặc "Xem kết quả ngay" (chấm sơ bộ) bất cứ lúc nào.
/// </summary>
//#endregion
(function () {
    'use strict';

    const page = document.getElementById('scoring-page');
    if (!page) return;

    const sessionId = parseInt(page.dataset.sessionId, 10);
    const token = page.querySelector('input[name="__RequestVerificationToken"]').value;
    const message = document.getElementById('room-message');
    const btnRetry = document.getElementById('btn-retry-score');
    const btnFallback = document.getElementById('btn-fallback-score');

    // Giây chờ trước mỗi lần tự chấm lại (+ ngẫu nhiên 0–10 giây để cả lớp không chấm lại cùng lúc)
    const AUTO_RESCORE_DELAYS = [20, 35, 50];
    let autoCount = 0;
    let timer = null;

    function showMessage(text) {
        message.textContent = text;
        message.hidden = !text;
    }

    function setButtons(enabled) {
        btnRetry.disabled = !enabled;
        btnFallback.disabled = !enabled;
    }

    function cancelTimer() {
        if (timer) clearInterval(timer);
        timer = null;
    }

    function scheduleAutoRescore() {
        let left = AUTO_RESCORE_DELAYS[autoCount++] + Math.floor(Math.random() * 11);
        const tick = function () {
            showMessage('AI đang bận vì nhiều người chấm cùng lúc. Tự chấm lại bằng AI sau ' + left + ' giây (lần '
                + autoCount + '/' + AUTO_RESCORE_DELAYS.length + '). Bạn có thể chờ, bấm "Chấm lại" ngay, hoặc xem kết quả sơ bộ.');
            if (left-- <= 0) score(false);
        };
        tick();
        timer = setInterval(tick, 1000);
    }

    async function score(fallback) {
        cancelTimer();
        setButtons(false);
        showMessage(fallback ? 'Đang chấm sơ bộ...' : 'Đang chấm điểm bằng AI, vui lòng không tắt trang...');
        if (!fallback) window.MockviewScoringOverlay?.show();
        try {
            const response = await fetch('/Interview/Finish', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ sessionId: sessionId, fallback: !!fallback })
            });
            const body = await response.json().catch(() => ({}));
            if (!response.ok) {
                const error = new Error(body.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
                error.body = body;
                throw error;
            }
            window.MockviewScoringOverlay?.release();
            window.location.href = body.redirectUrl;
        } catch (error) {
            window.MockviewScoringOverlay?.hide();
            setButtons(true);
            const retryable = !fallback && !!(error.body && error.body.retryable);
            if (retryable && autoCount < AUTO_RESCORE_DELAYS.length) {
                scheduleAutoRescore();
                return;
            }
            const configError = !!(error.body && error.body.configError);
            btnRetry.hidden = configError;
            showMessage(error.message + ' Bạn có thể chấm lại bằng AI, hoặc xem kết quả ngay (chấm sơ bộ, không cần AI).');
        }
    }

    btnRetry.addEventListener('click', () => score(false));
    btnFallback.addEventListener('click', () => score(true));

    // Server vừa thử chấm và AI bận -> bắt đầu bằng đếm ngược (không gọi lại ngay)
    scheduleAutoRescore();
})();
