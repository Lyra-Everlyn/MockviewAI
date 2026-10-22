// [AI-INTERVIEW] Phòng phỏng vấn: nói hoặc gõ câu trả lời -> gửi lên server -> sang câu tiếp -> chấm điểm.
// Giọng nói dùng SpeechRecognition của trình duyệt (Chrome/Edge). Trình duyệt không hỗ trợ thì dùng nút "Gõ câu trả lời".
// 21/10 (sáng): [#17] đọc câu hỏi + "Nghe lại"; [#18] kiểm tra micro; [#3] đo khoảng lặng khi nói.
// 21/10 (chiều): sóng âm đo từ micro (thấy sóng = hệ thống nghe thấy bạn); chữ hiện trực tiếp khi nói;
//                báo lỗi rõ ràng (vd Opera không nhận giọng nói); xác nhận "Đã ghi nhận câu trả lời";
//                câu trả lời bị từ chối thì nói lại; AI chấm lỗi thì "Chấm lại" / kết quả dự phòng.

(function () {
    const room = document.getElementById('interview-room');
    if (!room) return;
    const sessionId = parseInt(room.dataset.sessionId, 10);
    const totalQuestions = parseInt(room.dataset.total, 10);
    const token = room.querySelector('input[name="__RequestVerificationToken"]').value;

    // Các phần tử trên trang
    const questionText = document.getElementById('question-text');
    const transcriptText = document.getElementById('transcript-text');
    const transcriptMeta = document.getElementById('transcript-meta');
    const answerInput = document.getElementById('answer-input');
    const lastAnswer = document.getElementById('last-answer');
    const roomWave = document.getElementById('room-wave');
    const btnSpeak = document.getElementById('btn-speak');
    const btnType = document.getElementById('btn-type');
    const btnSkip = document.getElementById('btn-skip');
    const btnSend = document.getElementById('btn-send');
    const btnStop = document.getElementById('btn-stop-session');
    const btnResume = document.getElementById('btn-resume');
    const btnFinish = document.getElementById('btn-finish');
    const btnReplay = document.getElementById('btn-replay');
    const btnRetryScore = document.getElementById('btn-retry-score');
    const btnFallbackScore = document.getElementById('btn-fallback-score');
    const statusTag = document.getElementById('status-tag');
    const voiceNote = document.getElementById('voice-note');
    const message = document.getElementById('room-message');
    const modal = document.getElementById('stop-modal');
    const answeredCount = document.getElementById('answered-count');

    const PLACEHOLDER = 'Câu trả lời của bạn sẽ hiện ở đây.';
    const MIN_PAUSE_SECONDS = 1;       // im lặng ngắn hơn 1 giây là ngắt hơi bình thường, không tính
    const NO_SOUND_HINT_MS = 6000;     // sau chừng này mà micro chưa nghe thấy gì -> nhắc kiểm tra micro
    const NO_TEXT_HINT_MS = 6000;      // nghe thấy tiếng nhưng chừng này vẫn chưa ra chữ -> báo trình duyệt không nhận giọng nói
    const SOUND_LEVEL = 0.06;          // ngưỡng mức âm thanh coi là "có tiếng"

    let committedText = '';            // chữ đã chốt từ các lần nhận dạng trước (khi trình duyệt tự ngắt rồi nối tiếp)
    let spokenText = '';               // toàn bộ chữ nhận từ giọng nói của câu hiện tại
    let isTyping = false;              // đang gõ tay hay không
    let speakStartedAt = null;         // thời điểm bắt đầu nói
    let speakSeconds = null;           // tổng thời gian nói (giây)
    let recognition = null;
    let isListening = false;
    let busy = false;                  // đang chờ server trả lời
    let scoreFailures = 0;             // số lần AI chấm lỗi liên tiếp
    let hintTimer = null;

    // Opera/Opera GX có webkitSpeechRecognition nhưng thường không kết nối được dịch vụ nhận giọng nói
    const isOpera = /OPR\/|Opera/i.test(navigator.userAgent);
    const OPERA_WARNING = 'Bạn đang dùng Opera: trình duyệt này thường KHÔNG chuyển giọng nói thành chữ được. ' +
        'Để nói trực tiếp, hãy mở trang bằng Chrome hoặc Edge. Hoặc bấm "Bỏ qua, tôi sẽ gõ chữ" để gõ câu trả lời.';

    // ---------- [#3] Ghi sự kiện theo thời gian ----------
    // Chỉ giữ trên trình duyệt (không gửi lên server). Server chỉ nhận danh sách khoảng lặng.
    // Xem nhanh khi chạy thử: mở F12 > Console, gõ  mockviewEvents
    let questionShownAt = Date.now();
    let lastSpeechAt = null;      // lần cuối nhận được chữ từ giọng nói
    let pauses = [];              // các khoảng lặng (giây) của câu đang trả lời
    const events = [];
    window.mockviewEvents = events;

    function logEvent(type, detail) {
        events.push({ type: type, atSeconds: Math.round((Date.now() - questionShownAt) / 100) / 10, detail: detail || null });
    }

    // ---------- Hàm gọi server ----------
    async function postJson(url, data) {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(data)
        });
        if (response.status === 429) throw new Error('Bạn thao tác quá nhanh. Vui lòng đợi một chút rồi thử lại.');
        const body = await response.json().catch(() => ({}));
        if (!response.ok) {
            const error = new Error(body.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
            error.body = body;   // retry = true: câu trả lời bị từ chối, cho nói lại
            throw error;
        }
        return body;
    }

    // kind: 'success' | 'error' | undefined
    function showMessage(text, kind) {
        message.textContent = text;
        message.hidden = !text;
        message.classList.remove('is-success', 'is-error');
        if (text && kind) message.classList.add('is-' + kind);
    }

    function setBusy(value) {
        busy = value;
        [btnSpeak, btnType, btnSkip, btnSend, btnStop, btnFinish, btnReplay].forEach(b => b.disabled = value);
    }

    // Nhãn dưới avatar: "Đang hỏi..." / "Đang lắng nghe..." / "Chờ bạn trả lời"
    function setStatus(text, cssClass) {
        statusTag.textContent = text;
        statusTag.classList.remove('is-listening', 'is-waiting');
        if (cssClass) statusTag.classList.add(cssClass);
    }

    // ---------- Sóng âm đo trực tiếp từ micro ----------
    // Dùng Web Audio (getUserMedia): chạy được cả khi trình duyệt không nhận dạng được giọng nói.
    // Sóng động = micro đang thu được tiếng của bạn.
    const WAVE_BARS = 24;
    let audioStream = null;
    let audioContext = null;
    let analyser = null;
    let waveFrame = 0;
    let meterBars = [];
    let meterContainer = null;
    let onSoundCallback = null;
    let heardSound = false;

    function buildWave(container) {
        container.innerHTML = '';
        for (let i = 0; i < WAVE_BARS; i++) container.appendChild(document.createElement('span'));
    }

    async function startMeter(container, onSound) {
        stopMeter();
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error('unsupported');
        }
        audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        audioContext = new AudioCtx();
        if (audioContext.state === 'suspended') await audioContext.resume();

        const source = audioContext.createMediaStreamSource(audioStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        analyser.smoothingTimeConstant = 0.6;
        source.connect(analyser);

        meterContainer = container;
        meterBars = Array.prototype.slice.call(container.children);
        onSoundCallback = onSound;
        heardSound = false;
        container.hidden = false;

        const data = new Uint8Array(analyser.frequencyBinCount);
        const step = 2;   // mỗi cột gộp 2 dải tần, lấy từ dải thứ 1 trở đi (giọng nói nằm ở tần số thấp-vừa)

        function draw() {
            analyser.getByteFrequencyData(data);
            let sum = 0;
            for (let i = 0; i < meterBars.length; i++) {
                const from = 1 + i * step;
                const value = ((data[from] || 0) + (data[from + 1] || 0)) / (2 * 255);
                sum += value;
                meterBars[i].style.height = Math.min(100, 10 + value * 130) + '%';
            }
            const level = sum / meterBars.length;
            const hearing = level > SOUND_LEVEL;
            meterContainer.classList.toggle('is-hearing', hearing);
            if (hearing && !heardSound) {
                heardSound = true;
                if (onSoundCallback) onSoundCallback();
            }
            waveFrame = requestAnimationFrame(draw);
        }
        draw();
    }

    function stopMeter() {
        if (waveFrame) cancelAnimationFrame(waveFrame);
        waveFrame = 0;
        if (audioStream) audioStream.getTracks().forEach(t => t.stop());
        audioStream = null;
        if (audioContext) { try { audioContext.close(); } catch (e) { } }
        audioContext = null;
        analyser = null;
        if (meterContainer) {
            meterContainer.classList.remove('is-hearing');
            meterBars.forEach(b => b.style.height = '');
        }
        meterContainer = null;
        meterBars = [];
        onSoundCallback = null;
    }

    function describeMeterError(error) {
        const name = error && error.name;
        if (name === 'NotAllowedError' || name === 'SecurityError')
            return 'Trình duyệt đang chặn micro. Bấm biểu tượng ổ khoá cạnh địa chỉ web > cho phép Micro, rồi thử lại.';
        if (name === 'NotFoundError' || name === 'OverconstrainedError')
            return 'Không tìm thấy micro. Hãy cắm/bật micro rồi thử lại.';
        if (name === 'NotReadableError')
            return 'Micro đang được ứng dụng khác sử dụng. Hãy tắt ứng dụng đó rồi thử lại.';
        return 'Không bật được micro trên trình duyệt này.';
    }

    // ---------- [#17] Đọc câu hỏi (text to speech) ----------
    const synth = window.speechSynthesis;
    let vietnameseVoice = null;

    function findVietnameseVoice() {
        if (!synth) return;
        vietnameseVoice = synth.getVoices().find(v => v.lang && v.lang.toLowerCase().startsWith('vi')) || null;
        voiceNote.hidden = !!vietnameseVoice;
        btnReplay.hidden = !vietnameseVoice;
    }

    function speakQuestion() {
        if (!synth || !vietnameseVoice || isListening) return;
        synth.cancel();   // dừng câu đang đọc dở (nếu có)
        const utterance = new SpeechSynthesisUtterance(questionText.textContent.trim());
        utterance.voice = vietnameseVoice;
        utterance.lang = vietnameseVoice.lang;
        utterance.rate = 0.95;   // chậm hơn một chút cho dễ nghe
        utterance.onstart = function () { setStatus('Đang hỏi...'); logEvent('tts_start'); };
        utterance.onend = function () { setStatus('Chờ bạn trả lời', 'is-waiting'); logEvent('tts_end'); };
        utterance.onerror = function () { setStatus('Chờ bạn trả lời', 'is-waiting'); };
        synth.speak(utterance);
    }

    function stopSpeaking() {
        if (synth) synth.cancel();
    }

    if (synth) {
        findVietnameseVoice();
        // Danh sách giọng thường tải chậm, trình duyệt báo lại khi có
        synth.addEventListener('voiceschanged', findVietnameseVoice);
    } else {
        voiceNote.hidden = false;
        btnReplay.hidden = true;
    }
    setStatus('Chờ bạn trả lời', 'is-waiting');

    btnReplay.addEventListener('click', function () {
        logEvent('replay_clicked');
        speakQuestion();
    });

    // ---------- Chữ hiện trực tiếp ----------
    // finalPart: phần đã chắc chắn (đậm). interimPart: phần trình duyệt đang nhận dạng (nhạt).
    function renderTranscript(finalPart, interimPart) {
        transcriptText.textContent = '';
        if (!finalPart && !interimPart) {
            transcriptText.textContent = PLACEHOLDER;
            transcriptMeta.hidden = true;
            return;
        }
        transcriptText.appendChild(document.createTextNode(finalPart));
        if (interimPart) {
            const span = document.createElement('span');
            span.className = 'interim';
            span.textContent = (finalPart ? ' ' : '') + interimPart;
            transcriptText.appendChild(span);
        }
        const words = ((finalPart + ' ' + interimPart).trim().match(/\S+/g) || []).length;
        transcriptMeta.textContent = 'Đã ghi nhận ' + words + ' từ. Bấm "Dừng nói" rồi "Gửi câu trả lời" khi bạn nói xong.';
        transcriptMeta.hidden = false;
    }

    // ---------- Nói (speech to text) ----------
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    function describeSpeechError(code) {
        switch (code) {
            case 'not-allowed':
            case 'service-not-allowed':
                return 'Trình duyệt đang chặn micro. Bấm biểu tượng ổ khoá cạnh địa chỉ web > cho phép Micro, rồi thử lại.';
            case 'audio-capture':
                return 'Không tìm thấy micro. Hãy cắm/bật micro rồi thử lại.';
            case 'network':
                return 'Trình duyệt này không kết nối được dịch vụ chuyển giọng nói thành chữ (thường gặp ở Opera). ' +
                    'Hãy mở bằng Chrome hoặc Edge, hoặc gõ câu trả lời.';
            case 'language-not-supported':
                return 'Trình duyệt chưa hỗ trợ nhận giọng nói tiếng Việt. Hãy dùng Chrome/Edge hoặc gõ câu trả lời.';
            default:
                return 'Không nhận được giọng nói. Hãy kiểm tra micro hoặc bấm "Gõ câu trả lời".';
        }
    }

    function clearHintTimer() {
        if (hintTimer) clearTimeout(hintTimer);
        hintTimer = null;
    }

    function setupRecognition() {
        recognition = new SpeechRecognition();
        recognition.lang = 'vi-VN';
        recognition.continuous = true;
        recognition.interimResults = true;

        recognition.onresult = function (event) {
            clearHintTimer();
            showMessage('');

            // [#3] Khoảng cách giữa 2 lần nhận được chữ = một khoảng lặng
            const now = Date.now();
            if (lastSpeechAt !== null) {
                const gap = (now - lastSpeechAt) / 1000;
                if (gap >= MIN_PAUSE_SECONDS) {
                    pauses.push(Math.round(gap * 10) / 10);
                    logEvent('pause', gap.toFixed(1) + 's');
                }
            } else {
                logEvent('first_word');
            }
            lastSpeechAt = now;

            let finalPart = '';
            let interimPart = '';
            for (let i = 0; i < event.results.length; i++) {
                const result = event.results[i];
                if (result.isFinal) finalPart += result[0].transcript + ' ';
                else interimPart += result[0].transcript;
            }
            const solid = (committedText + ' ' + finalPart).trim();
            const interim = interimPart.trim();
            spokenText = (solid + ' ' + interim).trim();
            renderTranscript(solid, interim);
        };

        recognition.onerror = function (event) {
            logEvent('mic_error', event.error);
            if (event.error === 'no-speech' || event.error === 'aborted') return;   // im lặng: không phải lỗi, vẫn nghe tiếp
            // Lỗi thật: dừng nghe và chuyển sang gõ chữ để người dùng không bị kẹt
            stopListening();
            showMessage(describeSpeechError(event.error), 'error');
            if (event.error === 'network' || event.error === 'language-not-supported') toggleTyping(true);
        };

        recognition.onend = function () {
            // Trình duyệt tự ngắt khi im lặng lâu: nếu người dùng chưa bấm dừng thì nối tiếp, giữ lại chữ đã có
            if (isListening) {
                committedText = spokenText;
                try { recognition.start(); return; } catch (e) { }
            }
            stopListening();
        };
    }

    function startListening() {
        if (!SpeechRecognition) {
            showMessage('Trình duyệt này chưa hỗ trợ nhận giọng nói. Hãy dùng Chrome/Edge hoặc bấm "Gõ câu trả lời".', 'error');
            toggleTyping(true);
            return;
        }
        if (isOpera) showMessage('Bạn đang dùng Opera: có thể sẽ không ra chữ. Nếu vậy hãy dùng Chrome/Edge hoặc gõ câu trả lời.');
        else showMessage('');

        if (!recognition) setupRecognition();
        stopSpeaking();   // không để micro thu lại giọng đọc câu hỏi
        committedText = '';
        spokenText = '';
        speakSeconds = null;
        pauses = [];
        lastSpeechAt = null;
        lastAnswer.hidden = true;
        renderTranscript('', '');
        speakStartedAt = Date.now();
        isListening = true;
        btnSpeak.lastChild.textContent = ' Dừng nói';
        setStatus('Đang lắng nghe...', 'is-listening');
        logEvent('listen_start');

        try {
            recognition.start();
        } catch (e) {
            // start() báo lỗi khi đang chạy dở: bỏ qua, lần bấm sau sẽ chạy lại
            logEvent('mic_error', 'start_failed');
        }

        // Sóng âm: cho người dùng thấy micro có thu được tiếng không (độc lập với việc ra chữ)
        buildWave(roomWave);
        startMeter(roomWave, function () {
            setStatus('Đang nghe bạn nói...', 'is-listening');
            logEvent('sound_detected');
        }).catch(function (error) {
            roomWave.hidden = true;
            logEvent('meter_error', error && error.name);
            if (error && error.name !== 'unsupported') {
                stopListening();
                showMessage(describeMeterError(error), 'error');
            }
        });

        clearHintTimer();
        hintTimer = setTimeout(function () {
            if (!isListening || lastSpeechAt !== null) return;
            if (heardSound) {
                // Có tiếng nhưng chưa ra chữ: lỗi nằm ở nhận dạng giọng nói của trình duyệt, không phải micro
                showMessage('Micro nghe thấy tiếng của bạn nhưng trình duyệt chưa chuyển được thành chữ. ' +
                    'Hãy thử Chrome/Edge hoặc bấm "Gõ câu trả lời".', 'error');
            } else {
                showMessage('Chưa nghe thấy tiếng. Hãy kiểm tra micro đã bật chưa và nói to hơn một chút.', 'error');
            }
        }, heardSound ? NO_TEXT_HINT_MS : NO_SOUND_HINT_MS);
    }

    function stopListening() {
        if (!isListening) return;
        isListening = false;
        clearHintTimer();
        speakSeconds = (Date.now() - speakStartedAt) / 1000;
        btnSpeak.lastChild.textContent = ' Bấm để nói';
        setStatus('Chờ bạn trả lời', 'is-waiting');
        logEvent('listen_stop', speakSeconds.toFixed(1) + 's');
        try { recognition.stop(); } catch (e) { }
        stopMeter();
        roomWave.hidden = true;
        btnSend.hidden = spokenText === '';
        if (spokenText !== '') renderTranscript(spokenText, '');   // chốt chữ, bỏ phần "nhạt"
    }

    btnSpeak.addEventListener('click', function () {
        if (isTyping) toggleTyping(false);
        isListening ? stopListening() : startListening();
    });

    // ---------- Gõ tay ----------
    function toggleTyping(on) {
        isTyping = on;
        answerInput.hidden = !on;
        transcriptText.hidden = on;
        transcriptMeta.hidden = on || transcriptMeta.hidden;
        btnSend.hidden = !on;
        if (on) answerInput.focus();
    }

    btnType.addEventListener('click', function () {
        if (isListening) stopListening();
        toggleTyping(!isTyping);
    });

    // ---------- Gửi, bỏ qua ----------
    function currentAnswer() {
        return isTyping ? answerInput.value.trim() : spokenText;
    }

    function shorten(text, max) {
        return text.length <= max ? text : text.substring(0, max).trim() + '...';
    }

    // Xoá câu trả lời đang có để người dùng nói/gõ lại
    function resetAnswer() {
        committedText = '';
        spokenText = '';
        speakSeconds = null;
        pauses = [];
        lastSpeechAt = null;
        answerInput.value = '';
        renderTranscript('', '');
        btnSend.hidden = !isTyping;
    }

    async function submit(skipped) {
        if (busy) return;
        if (isListening) stopListening();
        stopSpeaking();

        const answer = currentAnswer();
        if (!skipped && !answer) {
            showMessage('Vui lòng nói hoặc gõ câu trả lời trước khi gửi.', 'error');
            return;
        }

        setBusy(true);
        showMessage('Đang gửi câu trả lời...');
        logEvent(skipped ? 'skipped' : 'answer_sent');
        try {
            const result = await postJson('/Interview/Answer', {
                sessionId: sessionId,
                answer: skipped ? null : answer,
                durationSeconds: isTyping ? null : speakSeconds,
                pausesSeconds: isTyping || skipped ? null : pauses,
                skipped: skipped
            });
            answeredCount.textContent = result.answeredCount;

            // Xác nhận để người dùng biết hệ thống đã nhận
            lastAnswer.textContent = result.notice
                ? result.notice
                : skipped ? 'Đã bỏ qua câu này.' : '✓ Đã ghi nhận câu trả lời của bạn: "' + shorten(answer, 140) + '"';
            lastAnswer.hidden = false;
            showMessage('');
            setStatus('Đã nhận câu trả lời ✓', 'is-waiting');

            if (result.done) {
                await finishSession(false);   // hết câu hỏi: chấm điểm luôn
            } else {
                showNextQuestion(result.nextNumber, result.nextQuestion);
                setBusy(false);
            }
        } catch (error) {
            showMessage(error.message, 'error');
            setBusy(false);
            if (error.body && error.body.retry) resetAnswer();   // bị từ chối: xoá để nói lại
        }
    }

    btnSend.addEventListener('click', () => submit(false));
    btnSkip.addEventListener('click', () => submit(true));

    // Đổi sang câu hỏi tiếp theo, xóa câu trả lời cũ
    function showNextQuestion(number, text) {
        questionText.textContent = text;
        document.querySelectorAll('.badge-step, .mobile-step').forEach(el => {
            el.textContent = 'Câu ' + number + ' / ' + totalQuestions;
        });
        document.querySelector('.progress-fill').style.width = ((number - 1) * 100 / totalQuestions) + '%';

        resetAnswer();
        toggleTyping(false);
        btnSend.hidden = true;

        events.length = 0;   // mỗi câu một dòng thời gian mới
        questionShownAt = Date.now();
        logEvent('question_shown', number);
        speakQuestion();     // [#17] tự đọc câu hỏi mới
        // Nếu không có giọng đọc, đưa nhãn trạng thái về "Chờ bạn trả lời" sau khi hiện xác nhận
        setTimeout(function () { if (!isListening) setStatus('Chờ bạn trả lời', 'is-waiting'); }, 1500);
    }

    // ---------- Kết thúc và chấm điểm ----------
    // AI chấm lỗi: giữ buổi luyện, hiện nút "Chấm lại". Lỗi quá 2 lần thì thêm nút xem kết quả dự phòng (chỉ tính cách nói).
    async function finishSession(fallback) {
        if (isListening) stopListening();
        stopSpeaking();
        modal.style.display = 'none';
        setBusy(true);
        btnRetryScore.hidden = true;
        btnFallbackScore.hidden = true;
        showMessage('Đang chấm điểm, vui lòng đợi vài giây...');
        logEvent('finish', fallback ? 'fallback' : 'ai');
        try {
            const result = await postJson('/Interview/Finish', { sessionId: sessionId, fallback: !!fallback });
            window.location.href = result.redirectUrl;
        } catch (error) {
            scoreFailures++;
            let text = error.message;
            // Lỗi cấu hình (sai model/key): chấm lại cũng không được -> chỉ cho xem kết quả dự phòng
            const configError = !!(error.body && error.body.configError);
            btnRetryScore.hidden = configError;
            if (configError) {
                text += ' Bạn có thể xem kết quả dự phòng (chỉ tính phần cách nói).';
                btnFallbackScore.hidden = false;
            } else if (scoreFailures >= 2) {
                text += ' AI vẫn chưa phản hồi. Bạn có thể xem kết quả dự phòng (chỉ tính phần cách nói).';
                btnFallbackScore.hidden = false;
            }
            showMessage(text, 'error');
        }
    }

    btnRetryScore.addEventListener('click', () => finishSession(false));
    btnFallbackScore.addEventListener('click', () => finishSession(true));

    btnStop.addEventListener('click', function () {
        if (isListening) stopListening();
        stopSpeaking();
        modal.style.display = 'flex';
    });
    btnResume.addEventListener('click', function () { modal.style.display = 'none'; });
    btnFinish.addEventListener('click', () => finishSession(false));

    // ---------- [#18] Kiểm tra micro trước khi vào ----------
    const micCheck = document.getElementById('mic-check');
    const micTestText = document.getElementById('mic-test-text');
    const micTestStatus = document.getElementById('mic-test-status');
    const micWave = document.getElementById('mic-wave');
    const browserWarning = document.getElementById('browser-warning');
    const btnMicTest = document.getElementById('btn-mic-test');
    const btnMicOk = document.getElementById('btn-mic-ok');
    const btnMicSkip = document.getElementById('btn-mic-skip');
    const MIC_CHECKED_KEY = 'mockview-mic-checked-' + sessionId;
    const MIC_PLACEHOLDER = 'Chữ bạn nói sẽ hiện ở đây.';
    let micTester = null;
    let micTesting = false;
    let micGotText = false;
    let micHintTimer = null;

    function micStatus(text, ok) {
        micTestStatus.textContent = text;
        micTestStatus.className = 'mic-test-status ' + (ok === true ? 'ok' : ok === false ? 'error' : '');
    }

    function alreadyChecked() {
        try { return sessionStorage.getItem(MIC_CHECKED_KEY) === '1'; } catch (e) { return false; }
    }

    function endMicTest() {
        micTesting = false;
        if (micHintTimer) clearTimeout(micHintTimer);
        micHintTimer = null;
        if (micTester) { try { micTester.stop(); } catch (e) { } }
        stopMeter();
        btnMicTest.disabled = false;
        btnMicTest.textContent = 'Nói thử lại';
    }

    function closeMicCheck(how) {
        endMicTest();
        try { sessionStorage.setItem(MIC_CHECKED_KEY, '1'); } catch (e) { }
        micCheck.style.display = 'none';
        logEvent('mic_check_' + how);
        if (how === 'skip') toggleTyping(true);
        speakQuestion();   // bấm nút = có thao tác của người dùng, trình duyệt mới cho phép đọc
    }

    function startMicTest() {
        endMicTest();
        micTesting = true;
        micGotText = false;
        micTestText.textContent = MIC_PLACEHOLDER;
        micStatus('Đang bật micro...');
        btnMicTest.disabled = true;
        btnMicTest.textContent = 'Đang nghe...';

        // 1. Sóng âm: chứng minh micro có thu được tiếng không
        startMeter(micWave, function () {
            if (!micGotText) micStatus('Đã nghe thấy tiếng của bạn. Micro hoạt động.', true);
        }).then(function () {
            if (!heardSound) micStatus('Micro đã bật. Hãy nói thử: "Xin chào, tôi sẵn sàng phỏng vấn".');
        }).catch(function (error) {
            micStatus(describeMeterError(error), false);
            endMicTest();
        });

        // 2. Chuyển giọng nói thành chữ (nếu trình duyệt hỗ trợ)
        if (!SpeechRecognition) {
            micStatus('Trình duyệt này chưa chuyển được giọng nói thành chữ. Hãy dùng Chrome/Edge, hoặc bấm "Bỏ qua, tôi sẽ gõ chữ".', false);
            return;
        }
        micTester = new SpeechRecognition();
        micTester.lang = 'vi-VN';
        micTester.interimResults = true;
        micTester.onresult = function (event) {
            let text = '';
            for (let i = 0; i < event.results.length; i++) text += event.results[i][0].transcript + ' ';
            micTestText.textContent = text.trim() || MIC_PLACEHOLDER;
            micGotText = true;
            micStatus('Micro hoạt động tốt: hệ thống đã nhận được chữ. Bấm "Vào phỏng vấn" để bắt đầu.', true);
        };
        micTester.onerror = function (event) {
            if (event.error === 'no-speech') {
                micStatus('Chưa nghe thấy giọng nói. Hãy nói to hơn rồi bấm "Nói thử lại".', false);
            } else {
                micStatus(describeSpeechError(event.error), false);
            }
        };
        micTester.onend = function () {
            // Kết thúc lượt thử: nếu có tiếng mà không có chữ thì nói rõ nguyên nhân
            if (micTesting && !micGotText && heardSound) {
                micStatus('Micro nghe thấy tiếng của bạn nhưng trình duyệt không chuyển được thành chữ. ' +
                    'Hãy dùng Chrome/Edge, hoặc bấm "Bỏ qua, tôi sẽ gõ chữ".', false);
            }
            endMicTest();
        };
        try {
            micTester.start();
        } catch (e) {
            micStatus('Không bật được nhận giọng nói. Hãy thử lại hoặc bấm "Bỏ qua, tôi sẽ gõ chữ".', false);
            endMicTest();
        }
    }

    if (micCheck) {
        buildWave(micWave);
        if (isOpera || !SpeechRecognition) {
            browserWarning.textContent = !SpeechRecognition
                ? 'Trình duyệt này chưa hỗ trợ chuyển giọng nói thành chữ. Hãy mở bằng Chrome hoặc Edge, hoặc bấm "Bỏ qua, tôi sẽ gõ chữ".'
                : OPERA_WARNING;
            browserWarning.hidden = false;
        }
        if (alreadyChecked()) micCheck.style.display = 'none';
        btnMicTest.addEventListener('click', startMicTest);
        btnMicOk.addEventListener('click', () => closeMicCheck('ok'));
        btnMicSkip.addEventListener('click', () => closeMicCheck('skip'));
    }

    logEvent('question_shown', 'first');
})();
