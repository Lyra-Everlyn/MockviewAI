//#region AudioRecorder
/// <summary>
/// [SPEECH] Ghi âm câu trả lời và gửi lên server để chuyển thành chữ (Groq Whisper + Gemini chuẩn hoá).
/// -- Ghi từ một BẢN SAO của luồng micro (stream.clone()): trang có thể tắt luồng gốc (bộ đo âm lượng)
///    mà không làm mất dữ liệu đang ghi.
/// -- Tự chọn định dạng trình duyệt hỗ trợ: webm/opus (Chrome, Edge, Firefox), mp4 (Safari).
/// -- Không phụ thuộc giao diện: InterviewRoom.js quyết định khi nào ghi, khi nào gửi.
/// </summary>
//#endregion
(function (window) {
    'use strict';

    //#region Config
    // Thứ tự ưu tiên: opus nén tốt, Whisper đọc trực tiếp; mp4 cho Safari
    const CANDIDATE_TYPES = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4', 'audio/ogg;codecs=opus', 'audio/ogg'];
    // Bản sửa 18/11: 96 kbps như mặc định của SoundLab (32 kbps làm mất chi tiết khi nói nhỏ / có ồn). 2 phút ≈ 1,4 MB
    const BITS_PER_SECOND = 96000;
    const TIMESLICE_MS = 1000;          // lấy dữ liệu mỗi giây để không mất phần đầu nếu trình duyệt lỗi giữa chừng
    const MIN_DURATION_MS = 600;        // ngắn hơn: coi như chưa nói gì, không gửi
    const MIN_BYTES = 1500;
    //#endregion

    //#region Support
    /// <summary>Trình duyệt có ghi âm được không.</summary>
    function isSupported() {
        return !!(window.MediaRecorder && navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    }

    /// <summary>Chọn định dạng đầu tiên trình duyệt hỗ trợ; chuỗi rỗng = để trình duyệt tự chọn.</summary>
    function pickMimeType() {
        if (!window.MediaRecorder || typeof MediaRecorder.isTypeSupported !== 'function') return '';
        for (let i = 0; i < CANDIDATE_TYPES.length; i++) {
            if (MediaRecorder.isTypeSupported(CANDIDATE_TYPES[i])) return CANDIDATE_TYPES[i];
        }
        return '';
    }

    /// <summary>Phần mở rộng file theo định dạng (server và Whisper dựa vào đây để giải mã).</summary>
    function extensionFor(mimeType) {
        const type = (mimeType || '').toLowerCase();
        if (type.indexOf('mp4') >= 0 || type.indexOf('aac') >= 0) return '.mp4';
        if (type.indexOf('ogg') >= 0) return '.ogg';
        return '.webm';
    }
    //#endregion

    //#region Recording session
    /// <summary>
    /// Bắt đầu ghi từ một luồng micro đang mở.
    /// Trả về đối tượng { stop(): Promise<recording|null>, abort() }.
    /// recording = { blob, mimeType, extension, durationMs } hoặc null nếu quá ngắn / không có dữ liệu.
    /// </summary>
    function start(stream) {
        const ownStream = stream.clone();
        const mimeType = pickMimeType();
        const options = { audioBitsPerSecond: BITS_PER_SECOND };
        if (mimeType) options.mimeType = mimeType;

        const recorder = new MediaRecorder(ownStream, options);
        const chunks = [];
        const startedAt = Date.now();
        let finished = false;

        recorder.ondataavailable = function (event) {
            if (event.data && event.data.size > 0) chunks.push(event.data);
        };

        function releaseTracks() {
            ownStream.getTracks().forEach(function (t) { t.stop(); });
        }

        function stop() {
            return new Promise(function (resolve) {
                if (finished) { resolve(null); return; }
                finished = true;
                const durationMs = Date.now() - startedAt;

                recorder.onstop = function () {
                    releaseTracks();
                    const type = recorder.mimeType || mimeType || 'audio/webm';
                    const blob = new Blob(chunks, { type: type });
                    if (durationMs < MIN_DURATION_MS || blob.size < MIN_BYTES) { resolve(null); return; }
                    resolve({ blob: blob, mimeType: type, extension: extensionFor(type), durationMs: durationMs });
                };
                recorder.onerror = function () { releaseTracks(); resolve(null); };

                try {
                    if (recorder.state === 'inactive') recorder.onstop();
                    else recorder.stop();
                } catch (e) {
                    releaseTracks();
                    resolve(null);
                }
            });
        }

        function abort() {
            if (finished) return;
            finished = true;
            recorder.onstop = null;
            recorder.ondataavailable = null;
            try { if (recorder.state !== 'inactive') recorder.stop(); } catch (e) { }
            releaseTracks();
            chunks.length = 0;
        }

        recorder.start(TIMESLICE_MS);
        return { stop: stop, abort: abort };
    }
    //#endregion

    //#region Server
    /// <summary>
    /// Gửi bản ghi lên server. Trả về { rawText, text, normalized, empty }.
    /// Lỗi: ném Error có thuộc tính body = { message, retry, configError } (giống postJson của InterviewRoom).
    /// </summary>
    async function transcribe(url, antiForgeryToken, sessionId, recording, browserText) {
        const form = new FormData();
        form.append('sessionId', String(sessionId));
        form.append('audio', recording.blob, 'answer' + recording.extension);
        if (browserText) form.append('browserText', browserText.slice(0, 4000));

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': antiForgeryToken },
            body: form
        });
        if (response.status === 429) {
            const tooFast = new Error('Bạn thao tác quá nhanh. Vui lòng đợi một chút rồi thử lại.');
            tooFast.body = { retry: true };
            throw tooFast;
        }
        const body = await response.json().catch(function () { return {}; });
        if (!response.ok) {
            const error = new Error(body.message || 'Không chuyển được giọng nói thành chữ.');
            error.body = body;
            throw error;
        }
        return body;
    }
    //#endregion

    window.MockviewRecorder = { isSupported: isSupported, start: start, transcribe: transcribe };
})(window);
