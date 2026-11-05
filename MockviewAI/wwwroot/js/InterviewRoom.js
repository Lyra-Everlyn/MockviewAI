// MockviewAI | Interview Room
// Điều khiển câu hỏi, nhận dạng giọng nói, chấm điểm và chế độ Thực chiến.

(function () {
    const room = document.getElementById('interview-room');
    if (!room) return;
    const sessionId = parseInt(room.dataset.sessionId, 10);
    const totalQuestions = parseInt(room.dataset.total, 10);
    const token = room.querySelector('input[name="__RequestVerificationToken"]').value;

    // Tham chiếu DOM
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
    const btnClear = document.getElementById('btn-clear-answer');
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
    const MIN_PAUSE_SECONDS = 1;
    const NO_SOUND_HINT_MS = 6000;
    const NO_TEXT_HINT_MS = 6000;
    const SOUND_LEVEL = 0.06;

    // ============================================================================
    // CHẾ ĐỘ THỰC CHIẾN
    // ============================================================================
    // Đọc câu hỏi → suy nghĩ → tự bật micro → trả lời → tự gửi khi hết giờ.
    // Thực chiến giới hạn nghe lại và không cho sửa câu trả lời, trừ khi cần gõ thay thế.
    const isLive = room.dataset.mode === 'live';
    const THINK_SECONDS = parseInt(room.dataset.think || '15', 10);
    const MAX_ANSWER_SECONDS = parseInt(room.dataset.maxAnswer || '120', 10);
    const MAX_REPLAYS = parseInt(room.dataset.maxReplays || '1', 10);
    const TTS_SAFETY_MS = 25000;
    const liveTimer = document.getElementById('live-timer');
    const livePhaseLabel = document.getElementById('live-phase');
    const liveClock = document.getElementById('live-clock');
    const liveBarFill = document.getElementById('live-bar-fill');
    let livePhase = 'idle';
    let liveRemaining = 0;
    let liveTotal = 1;
    let liveInterval = null;
    let replaysUsed = 0;
    let liveQuestionToken = 0;

    // ============================================================================
    // PHÂN TÍCH GIỌNG NÓI
    // ============================================================================
    const VA = window.VoiceAnalysis || null;
    const FRAME_SECONDS = 0.1;
    const volumeHint = document.getElementById('volume-hint');
    const voiceLevel = document.getElementById('voice-level');
    let voiceFrames = [];
    let lastVoice = null;

    let committedText = '';
    let spokenText = '';
    let isTyping = false;
    let speakStartedAt = null;
    let speakSeconds = null;
    let recognition = null;
    let isListening = false;
    let busy = false;
    let scoreFailures = 0;
    let hintTimer = null;

    const isOpera = /OPR\/|Opera/i.test(navigator.userAgent);
    const OPERA_WARNING = 'Bạn đang dùng Opera: trình duyệt này thường KHÔNG chuyển giọng nói thành chữ được. ' +
        'Để nói trực tiếp, hãy mở trang bằng Chrome hoặc Edge. Hoặc bấm "Bỏ qua, tôi sẽ gõ chữ" để gõ câu trả lời.';

    // ============================================================================
    // THEO DÕI SỰ KIỆN
    // ============================================================================
    // Sự kiện chỉ lưu tại trình duyệt; server nhận danh sách khoảng lặng.

    let questionShownAt = Date.now();
    let lastSpeechAt = null;
    let pauses = [];
    const events = [];
    window.mockviewEvents = events;

    function logEvent(type, detail) {
        events.push({ type: type, atSeconds: Math.round((Date.now() - questionShownAt) / 100) / 10, detail: detail || null });
    }

    // ============================================================================
    // GIAO TIẾP VỚI SERVER
    // ============================================================================
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
            error.body = body;
            throw error;
        }
        return body;
    }

    function showMessage(text, kind) {
        message.textContent = text;
        message.hidden = !text;
        message.classList.remove('is-success', 'is-error');
        if (text && kind) message.classList.add('is-' + kind);
    }

    function setBusy(value) {
        busy = value;
        [btnSpeak, btnType, btnSkip, btnSend, btnStop, btnFinish, btnReplay, btnClear].forEach(b => b.disabled = value);
    }

    function setStatus(text, cssClass) {
        statusTag.textContent = text;
        statusTag.classList.remove('is-listening', 'is-waiting');
        if (cssClass) statusTag.classList.add(cssClass);
    }

    // ============================================================================
    // ĐO ÂM THANH TỪ MICRO
    // ============================================================================
    // Web Audio đo tín hiệu micro độc lập với SpeechRecognition.

    const WAVE_BARS = 24;
    let audioStream = null;
    let audioContext = null;
    let analyser = null;
    let waveFrame = 0;
    let meterBars = [];
    let meterContainer = null;
    let onSoundCallback = null;
    let heardSound = false;
    let timeAnalyser = null;
    let timeBuffer = null;
    let onFrameCallback = null;
    let lastFrameAt = 0;

    function buildWave(container) {
        container.innerHTML = '';
        for (let i = 0; i < WAVE_BARS; i++) container.appendChild(document.createElement('span'));
    }

    async function startMeter(container, onSound, onFrame) {
        stopMeter();
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            throw new Error('unsupported');
        }
        // Tắt tự động khuếch đại để chỉ số âm lượng phản ánh giọng nói thực tế.
        audioStream = await navigator.mediaDevices.getUserMedia({
            audio: { autoGainControl: false, noiseSuppression: false, echoCancellation: true }
        });
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        audioContext = new AudioCtx();
        if (audioContext.state === 'suspended') await audioContext.resume();

        const source = audioContext.createMediaStreamSource(audioStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        analyser.smoothingTimeConstant = 0.6;
        source.connect(analyser);
        if (VA && onFrame) {
            timeAnalyser = audioContext.createAnalyser();
            timeAnalyser.fftSize = 2048;
            timeBuffer = new Float32Array(timeAnalyser.fftSize);
            source.connect(timeAnalyser);
            onFrameCallback = onFrame;
            lastFrameAt = 0;
        }

        meterContainer = container;
        meterBars = Array.prototype.slice.call(container.children);
        onSoundCallback = onSound;
        heardSound = false;
        container.hidden = false;

        const data = new Uint8Array(analyser.frequencyBinCount);
        const step = 2;

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

            const now = performance.now();
            if (onFrameCallback && timeAnalyser && now - lastFrameAt >= FRAME_SECONDS * 1000) {
                lastFrameAt = now;
                timeAnalyser.getFloatTimeDomainData(timeBuffer);
                const db = VA.rmsDb(timeBuffer);
                onFrameCallback({ db: db, pitch: db > -60 ? VA.estimatePitch(timeBuffer, audioContext.sampleRate) : null });
            }
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
        onFrameCallback = null;
        timeAnalyser = null;
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

    // ============================================================================
    // ĐỌC CÂU HỎI (TEXT-TO-SPEECH)
    // ============================================================================
    const synth = window.speechSynthesis;
    let vietnameseVoice = null;

    function findVietnameseVoice() {
        if (!synth) return;
        vietnameseVoice = synth.getVoices().find(v => v.lang && v.lang.toLowerCase().startsWith('vi')) || null;
        voiceNote.hidden = !!vietnameseVoice;
        btnReplay.hidden = !vietnameseVoice;
        updateReplayButton();
    }

    function updateReplayButton() {
        if (!isLive) return;
        const left = MAX_REPLAYS - replaysUsed;
        btnReplay.hidden = !vietnameseVoice || left <= 0;
        btnReplay.lastChild.textContent = ' Nghe lại (còn ' + Math.max(left, 0) + ')';
    }

    function speakQuestion(onDone) {
        const done = typeof onDone === 'function' ? onDone : null;
        if (!synth || !vietnameseVoice || isListening) { if (done) done(); return; }
        synth.cancel();
        const utterance = new SpeechSynthesisUtterance(questionText.textContent.trim());
        utterance.voice = vietnameseVoice;
        utterance.lang = vietnameseVoice.lang;
        utterance.rate = 0.95;
        utterance.onstart = function () { setStatus('Đang hỏi...'); logEvent('tts_start'); };
        utterance.onend = function () { setStatus('Chờ bạn trả lời', 'is-waiting'); logEvent('tts_end'); if (done) done(); };
        utterance.onerror = function () { setStatus('Chờ bạn trả lời', 'is-waiting'); if (done) done(); };
        synth.speak(utterance);
    }

    function stopSpeaking() {
        if (synth) synth.cancel();
    }

    if (synth) {
        findVietnameseVoice();

        synth.addEventListener('voiceschanged', findVietnameseVoice);
    } else {
        voiceNote.hidden = false;
        btnReplay.hidden = true;
    }
    setStatus('Chờ bạn trả lời', 'is-waiting');

    btnReplay.addEventListener('click', function () {
        if (isLive) {

            if (livePhase === 'answer' || livePhase === 'sending' || replaysUsed >= MAX_REPLAYS) return;
            replaysUsed++;
            updateReplayButton();
        }
        logEvent('replay_clicked');
        speakQuestion();
    });

    // ============================================================================
    // HIỂN THỊ BẢN CHÉP GIỌNG NÓI
    // ============================================================================

    function renderTranscript(finalPart, interimPart) {
        transcriptText.textContent = '';
        btnClear.hidden = isLive || isTyping || (!finalPart && !interimPart);
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
        transcriptMeta.textContent = isLive
            ? 'Đã ghi nhận ' + words + ' từ. Nói xong thì bấm "Xong, gửi câu trả lời" (hết giờ sẽ tự gửi).'
            : 'Đã ghi nhận ' + words + ' từ. Bấm "Dừng nói" rồi "Gửi câu trả lời" khi bạn nói xong.';
        transcriptMeta.hidden = false;
    }

    // ============================================================================
    // NHẬN DẠNG GIỌNG NÓI
    // ============================================================================
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
            if (event.error === 'no-speech' || event.error === 'aborted') return;

            stopListening();
            showMessage(describeSpeechError(event.error), 'error');
            if (event.error === 'network' || event.error === 'language-not-supported') toggleTyping(true);
        };

        recognition.onend = function () {
            // Khi nhận dạng tự ngắt, nối phiên nghe và giữ lại phần văn bản đã nhận.
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
        stopSpeaking();
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

            logEvent('mic_error', 'start_failed');
        }

        buildWave(roomWave);
        voiceFrames = [];
        lastVoice = null;
        startMeter(roomWave, function () {
            setStatus('Đang nghe bạn nói...', 'is-listening');
            logEvent('sound_detected');
        }, onRoomVoiceFrame).catch(function (error) {
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
        if (VA && voiceFrames.length) lastVoice = VA.summarize(voiceFrames, FRAME_SECONDS);
        showVolumeHint(false);
        btnSend.hidden = isLive || spokenText === '';
        if (spokenText !== '') renderTranscript(spokenText, '');
    }

    btnSpeak.addEventListener('click', function () {
        if (isLive) { liveSpeakClick(); return; }
        if (isTyping) toggleTyping(false);
        isListening ? stopListening() : startListening();
    });

    // ============================================================================
    // TRẢ LỜI BẰNG VĂN BẢN
    // ============================================================================
    function toggleTyping(on) {
        isTyping = on;
        answerInput.hidden = !on;
        transcriptText.hidden = on;
        transcriptMeta.hidden = on || transcriptMeta.hidden;
        btnSend.hidden = !on;
        btnClear.hidden = isLive || on || spokenText === '';
        if (on) answerInput.focus();
    }

    btnType.addEventListener('click', function () {
        if (isListening) stopListening();
        toggleTyping(!isTyping);
    });

    // ============================================================================
    // GỬI VÀ BỎ QUA CÂU TRẢ LỜI
    // ============================================================================
    function currentAnswer() {
        return isTyping ? answerInput.value.trim() : spokenText;
    }

    function shorten(text, max) {
        return text.length <= max ? text : text.substring(0, max).trim() + '...';
    }

    function resetAnswer() {
        committedText = '';
        spokenText = '';
        speakSeconds = null;
        pauses = [];
        lastSpeechAt = null;
        answerInput.value = '';
        renderTranscript('', '');
        btnSend.hidden = !isTyping;
        voiceFrames = [];
        lastVoice = null;
        showVolumeHint(false);
        if (voiceLevel) voiceLevel.hidden = true;
    }

    // Dùng abort() để hủy kết quả đang nhận dạng, tránh văn bản cũ xuất hiện lại.

    function clearRecording() {
        if (busy) return;
        if (isListening) {
            isListening = false;
            clearHintTimer();
            try { recognition.abort(); } catch (e) { }
            stopMeter();
            roomWave.hidden = true;
            btnSpeak.lastChild.textContent = ' Bấm để nói';
        }
        resetAnswer();
        lastAnswer.hidden = true;
        setStatus('Chờ bạn trả lời', 'is-waiting');
        showMessage('Đã xoá ghi âm. Bấm "Bấm để nói" để trả lời lại.');
        logEvent('answer_cleared');
    }

    btnClear.addEventListener('click', clearRecording);

    let currentIsFollowUp = !!(document.getElementById('followup-note') && !document.getElementById('followup-note').hidden);

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

                voiceLevelDb: !skipped && !isTyping && lastVoice ? lastVoice.levelDb : null,
                quietRatio: !skipped && !isTyping && lastVoice ? lastVoice.quietRatio : null,
                pitchVariation: !skipped && !isTyping && lastVoice ? lastVoice.pitchVariation : null,
                skipped: skipped
            });
            answeredCount.textContent = result.answeredCount;

            lastAnswer.textContent = result.notice
                ? result.notice
                : skipped && currentIsFollowUp ? 'Đã bỏ qua câu hỏi thêm (không bị tính Sai).'
                    : skipped ? (isLive ? 'Đã bỏ qua câu này. Thực chiến: câu bỏ qua được tính là Sai.' : 'Đã bỏ qua câu này.')
                        : '✓ Đã ghi nhận câu trả lời của bạn: "' + shorten(answer, 140) + '"';
            lastAnswer.hidden = false;
            showMessage('');
            setStatus('Đã nhận câu trả lời ✓', 'is-waiting');

            if (result.done) {
                await finishSession(false);
            } else {
                showNextQuestion(result.nextNumber, result.nextQuestion, result.nextIsFollowUp);
                setBusy(false);
            }
        } catch (error) {
            showMessage(error.message, 'error');
            setBusy(false);
            if (error.body && error.body.retry) resetAnswer();
            if (isLive && livePhase === 'sending') livePhase = 'answer';
        }
    }

    btnSend.addEventListener('click', () => submit(false));
    btnSkip.addEventListener('click', function () {
        if (isLive) {
            if (busy || livePhase === 'sending') return;
            liveStopClock();
            livePhase = 'sending';
        }
        submit(true);
    });

    function showNextQuestion(number, text, isFollowUp) {
        questionText.textContent = text;

        const followUpNote = document.getElementById('followup-note');
        if (followUpNote) followUpNote.hidden = !isFollowUp;
        currentIsFollowUp = !!isFollowUp;

        btnSkip.textContent = currentIsFollowUp ? 'Bỏ qua câu hỏi thêm' : isLive ? 'Bỏ qua (tính Sai)' : 'Bỏ qua câu này';
        document.querySelectorAll('.badge-step, .mobile-step').forEach(el => {
            el.textContent = 'Câu ' + number + ' / ' + totalQuestions;
        });
        document.querySelector('.progress-fill').style.width = ((number - 1) * 100 / totalQuestions) + '%';

        document.querySelectorAll('.tape-room .seg').forEach(function (seg, i) {
            seg.classList.toggle('done', i < number - 1);
            seg.classList.toggle('now', i === number - 1);
        });

        resetAnswer();
        toggleTyping(false);
        btnSend.hidden = true;

        events.length = 0;
        questionShownAt = Date.now();
        logEvent(isFollowUp ? 'followup_shown' : 'question_shown', number);
        if (isLive) liveBeginQuestion();
        else speakQuestion();

        setTimeout(function () { if (!isListening) setStatus('Chờ bạn trả lời', 'is-waiting'); }, 1500);
    }

    // ============================================================================
    // KẾT THÚC VÀ CHẤM ĐIỂM
    // ============================================================================
    // Nếu chấm AI lỗi, cho phép thử lại hoặc dùng kết quả sơ bộ không cần AI.
    // Lỗi cấu hình không thể giải quyết bằng thử lại nên chỉ cho xem kết quả sơ bộ.
    async function finishSession(fallback) {
        if (isLive) { liveStopClock(); livePhase = 'idle'; if (liveTimer) liveTimer.hidden = true; }
        if (isListening) stopListening();
        stopSpeaking();
        modal.style.display = 'none';
        setBusy(true);
        btnRetryScore.hidden = true;
        btnFallbackScore.hidden = true;
        showMessage('Đang chấm điểm. AI có thể mất 1–2 phút, vui lòng không tắt trang...');
        logEvent('finish', fallback ? 'fallback' : 'ai');
        try {
            const result = await postJson('/Interview/Finish', { sessionId: sessionId, fallback: !!fallback });
            window.location.href = result.redirectUrl;
        } catch (error) {
            scoreFailures++;
            let text = error.message;
            // Lỗi cấu hình không thể giải quyết bằng thử lại nên chỉ cho xem kết quả sơ bộ.
            const configError = !!(error.body && error.body.configError);
            btnRetryScore.hidden = configError;
            btnFallbackScore.hidden = false;
            text += configError
                ? ' Bạn có thể xem kết quả ngay (chấm sơ bộ, không cần AI).'
                : (scoreFailures >= 2 ? ' AI vẫn chưa phản hồi.' : '') + ' Bạn có thể chấm lại bằng AI, hoặc xem kết quả ngay (chấm sơ bộ, không cần AI).';
            showMessage(text, 'error');
        }
    }

    btnRetryScore.addEventListener('click', () => finishSession(false));
    btnFallbackScore.addEventListener('click', () => finishSession(true));

    btnStop.addEventListener('click', function () {
        if (isLive) liveStopClock();
        if (isListening) stopListening();
        stopSpeaking();
        modal.style.display = 'flex';
    });
    btnResume.addEventListener('click', function () {
        modal.style.display = 'none';
        if (isLive) liveResume();
    });
    btnFinish.addEventListener('click', () => finishSession(false));

    // ============================================================================
    // KIỂM TRA MICRO TRƯỚC BUỔI PHỎNG VẤN
    // ============================================================================
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
    const micLevel = document.getElementById('mic-level');
    let micFrames = [];

    const MIC_MIN_VOICED = 0.6;

    function onMicVoiceFrame(frame) {
        micFrames.push(frame);
        if (micFrames.length % 5 === 0) updateMicLevel();
    }

    function updateMicLevel() {
        if (!micLevel || !VA || !micFrames.length) return;
        const s = VA.summarize(micFrames, FRAME_SECONDS, MIC_MIN_VOICED);
        if (!s) return;
        const label = VA.levelLabel(s.levelDb);
        micLevel.hidden = false;
        micLevel.dataset.level = label === 'Vừa' ? 'ok' : 'warn';
        micLevel.textContent = label === 'Vừa' ? 'Âm lượng giọng: Vừa, người phỏng vấn nghe rõ.'
            : label === 'Nhỏ' ? 'Âm lượng giọng: Nhỏ. Hãy nói to hơn hoặc ngồi gần micro.'
                : 'Âm lượng giọng: Quá to, dễ rè. Hãy ngồi xa micro một chút.';
    }

    function micStatus(text, ok) {
        micTestStatus.textContent = text;
        micTestStatus.className = 'mic-test-status ' + (ok === true ? 'ok' : ok === false ? 'error' : '');
    }

    function alreadyChecked() {
        try { return sessionStorage.getItem(MIC_CHECKED_KEY) === '1'; } catch (e) { return false; }
    }

    function endMicTest() {
        if (micTesting) updateMicLevel();
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

        if (isLive) liveBeginQuestion();
        else speakQuestion();
    }

    function startMicTest() {
        endMicTest();
        micTesting = true;
        micGotText = false;
        micTestText.textContent = MIC_PLACEHOLDER;
        micStatus('Đang bật micro...');
        btnMicTest.disabled = true;
        btnMicTest.textContent = 'Đang nghe...';

        micFrames = [];
        if (micLevel) micLevel.hidden = true;
        startMeter(micWave, function () {
            if (!micGotText) micStatus('Đã nghe thấy tiếng của bạn. Micro hoạt động.', true);
        }, onMicVoiceFrame).then(function () {
            if (!heardSound) micStatus('Micro đã bật. Hãy nói thử: "Xin chào, tôi sẵn sàng phỏng vấn".');
        }).catch(function (error) {
            micStatus(describeMeterError(error), false);
            endMicTest();
        });

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
            // Bỏ qua sự kiện của lần thử micro đã kết thúc để tránh ảnh hưởng phiên chính.
            if (!micTesting) return;

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

    // ============================================================================
    // PHẢN HỒI ÂM LƯỢNG GIỌNG NÓI
    // ============================================================================
    function showVolumeHint(on) {
        if (volumeHint) volumeHint.hidden = !on;
    }

    function onRoomVoiceFrame(frame) {
        voiceFrames.push(frame);
        if (voiceFrames.length % 5 !== 0) return;
        showVolumeHint(VA.isQuietNow(voiceFrames, FRAME_SECONDS));
        const recent = VA.summarize(voiceFrames.slice(-30), FRAME_SECONDS);
        if (voiceLevel && recent) {
            const label = VA.levelLabel(recent.levelDb);
            voiceLevel.hidden = false;
            voiceLevel.dataset.level = label === 'Vừa' ? 'ok' : 'warn';
            voiceLevel.textContent = 'Âm lượng: ' + label;
        }
    }

    // ============================================================================
    // VÒNG ĐỜI CÂU HỎI THỰC CHIẾN
    // ============================================================================
    function formatClock(seconds) {
        const s = Math.max(0, seconds);
        return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0');
    }

    function setSpeakLabel(text) {
        btnSpeak.lastChild.textContent = ' ' + text;
    }

    function liveStopClock() {
        if (liveInterval) clearInterval(liveInterval);
        liveInterval = null;
    }

    function liveShow(phase, label, seconds, total) {
        liveTimer.hidden = false;
        liveTimer.dataset.phase = phase;
        liveTimer.dataset.low = phase === 'answer' && seconds <= 20 ? '1' : '0';
        livePhaseLabel.textContent = label;
        liveClock.textContent = formatClock(seconds);
        liveBarFill.style.width = (total > 0 ? Math.max(0, seconds) * 100 / total : 0) + '%';
    }

    function liveRunClock(phase, seconds, total, label, onDone) {
        liveStopClock();
        livePhase = phase;
        liveRemaining = seconds;
        liveTotal = total;
        liveShow(phase, label, liveRemaining, liveTotal);
        liveInterval = setInterval(function () {
            liveRemaining--;
            liveShow(phase, label, liveRemaining, liveTotal);
            if (liveRemaining <= 0) { liveStopClock(); onDone(); }
        }, 1000);
    }

    // Mỗi câu Thực chiến bắt đầu bằng đọc câu hỏi rồi đếm thời gian suy nghĩ.
    function liveBeginQuestion() {
        replaysUsed = 0;
        updateReplayButton();
        livePhase = 'reading';
        liveShow('reading', 'Đang đọc câu hỏi', THINK_SECONDS, THINK_SECONDS);
        setSpeakLabel('Trả lời ngay');
        const token = ++liveQuestionToken;
        const startIfSameQuestion = function () { if (token === liveQuestionToken) liveStartThinking(); };
        speakQuestion(startIfSameQuestion);
        setTimeout(startIfSameQuestion, TTS_SAFETY_MS);
        logEvent('live_question_begin');
    }

    function liveStartThinking() {
        if (livePhase !== 'reading') return;
        setStatus('Đang suy nghĩ...', 'is-waiting');
        liveRunClock('think', THINK_SECONDS, THINK_SECONDS, 'Thời gian suy nghĩ', liveStartAnswer);
        logEvent('live_think_start');
    }

    function liveStartAnswer() {
        if (busy || (livePhase !== 'think' && livePhase !== 'reading')) return;
        liveStopClock();
        stopSpeaking();
        startListening();
        setSpeakLabel('Xong, gửi câu trả lời');
        liveRunClock('answer', MAX_ANSWER_SECONDS, MAX_ANSWER_SECONDS, 'Thời gian trả lời', liveFinishAnswer);
        logEvent('live_answer_start');
    }

    function liveFinishAnswer() {
        if (livePhase !== 'answer') return;
        liveStopClock();
        livePhase = 'sending';
        if (isListening) stopListening();
        if (!currentAnswer()) {
            showMessage('Bạn chưa nói gì nên câu này được tính là bỏ qua (Sai).', 'error');
            submit(true);
        } else {
            submit(false);
        }
        logEvent('live_answer_end', liveRemaining + 's left');
    }

    function liveSpeakClick() {
        if (busy) return;
        if (livePhase === 'ready') liveBeginQuestion();
        else if (livePhase === 'reading' || livePhase === 'think') liveStartAnswer();
        else if (livePhase === 'answer') liveFinishAnswer();
    }

    // Khi tiếp tục Thực chiến, bảo toàn thời gian suy nghĩ còn lại; câu đang trả lời sẽ được gửi.

    function liveResume() {
        if (livePhase === 'reading' || livePhase === 'think') {
            livePhase = 'think';
            liveRunClock('think', Math.max(liveRemaining, 3), THINK_SECONDS, 'Thời gian suy nghĩ', liveStartAnswer);
        } else if (livePhase === 'answer') {
            liveFinishAnswer();
        }
    }

    // Khi quay lại giữa buổi, cần thao tác người dùng trước khi bật micro/giọng đọc.
    function liveReady() {
        livePhase = 'ready';
        liveShow('ready', 'Sẵn sàng?', THINK_SECONDS, THINK_SECONDS);
        setSpeakLabel('Bắt đầu câu này');
        setStatus('Chờ bạn bắt đầu', 'is-waiting');
    }

    if (isLive) {
        btnType.hidden = true;
        btnClear.hidden = true;
        btnSend.hidden = true;
        updateReplayButton();
        if (!micCheck || micCheck.style.display === 'none') liveReady();
        else setSpeakLabel('Trả lời ngay');
    }

    logEvent('question_shown', 'first');
})();
