// [AI-INTERVIEW] Phòng phỏng vấn: nói hoặc gõ câu trả lời -> gửi lên server -> sang câu tiếp -> chấm điểm.
// Giọng nói dùng SpeechRecognition của trình duyệt (Chrome/Edge). Trình duyệt không hỗ trợ thì dùng nút "Gõ câu trả lời".

(function () {
    const room = document.getElementById('interview-room');
    const sessionId = parseInt(room.dataset.sessionId, 10);
    const totalQuestions = parseInt(room.dataset.total, 10);
    const token = room.querySelector('input[name="__RequestVerificationToken"]').value;

    // Các phần tử trên trang
    const questionText = document.getElementById('question-text');
    const transcriptText = document.getElementById('transcript-text');
    const answerInput = document.getElementById('answer-input');
    const btnSpeak = document.getElementById('btn-speak');
    const btnType = document.getElementById('btn-type');
    const btnSkip = document.getElementById('btn-skip');
    const btnSend = document.getElementById('btn-send');
    const btnStop = document.getElementById('btn-stop-session');
    const btnResume = document.getElementById('btn-resume');
    const btnFinish = document.getElementById('btn-finish');
    const message = document.getElementById('room-message');
    const modal = document.getElementById('stop-modal');
    const answeredCount = document.getElementById('answered-count');

    const PLACEHOLDER = 'Câu trả lời của bạn sẽ hiện ở đây.';
    let spokenText = '';          // chữ nhận từ giọng nói
    let isTyping = false;         // đang gõ tay hay không
    let speakStartedAt = null;    // thời điểm bắt đầu nói
    let speakSeconds = null;      // tổng thời gian nói (giây)
    let recognition = null;
    let isListening = false;
    let busy = false;             // đang chờ server trả lời

    // ---------- Hàm gọi server ----------
    async function postJson(url, data) {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(data)
        });
        const body = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(body.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
        return body;
    }

    function showMessage(text) {
        message.textContent = text;
        message.hidden = !text;
    }

    function setBusy(value) {
        busy = value;
        [btnSpeak, btnType, btnSkip, btnSend, btnStop, btnFinish].forEach(b => b.disabled = value);
    }

    // ---------- Nói (speech to text) ----------
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    function setupRecognition() {
        recognition = new SpeechRecognition();
        recognition.lang = 'vi-VN';
        recognition.continuous = true;
        recognition.interimResults = true;

        recognition.onresult = function (event) {
            let text = '';
            for (let i = 0; i < event.results.length; i++) text += event.results[i][0].transcript + ' ';
            spokenText = text.trim();
            transcriptText.textContent = spokenText || PLACEHOLDER;
        };
        recognition.onerror = function () {
            showMessage('Không nghe được giọng nói. Hãy kiểm tra micro hoặc bấm "Gõ câu trả lời".');
        };
        recognition.onend = function () { stopListening(); };
    }

    function startListening() {
        if (!SpeechRecognition) {
            showMessage('Trình duyệt này chưa hỗ trợ nhận giọng nói. Hãy dùng Chrome hoặc bấm "Gõ câu trả lời".');
            return;
        }
        if (!recognition) setupRecognition();
        showMessage('');
        spokenText = '';
        speakSeconds = null;
        speakStartedAt = Date.now();
        isListening = true;
        btnSpeak.lastChild.textContent = ' Dừng nói';
        recognition.start();
    }

    function stopListening() {
        if (!isListening) return;
        isListening = false;
        speakSeconds = (Date.now() - speakStartedAt) / 1000;
        btnSpeak.lastChild.textContent = ' Bấm để nói';
        try { recognition.stop(); } catch (e) { }
        btnSend.hidden = spokenText === '';
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

    async function submit(skipped) {
        if (busy) return;
        if (isListening) stopListening();

        const answer = currentAnswer();
        if (!skipped && !answer) {
            showMessage('Vui lòng nói hoặc gõ câu trả lời trước khi gửi.');
            return;
        }

        setBusy(true);
        showMessage('');
        try {
            const result = await postJson('/Interview/Answer', {
                sessionId: sessionId,
                answer: skipped ? null : answer,
                durationSeconds: isTyping ? null : speakSeconds,
                skipped: skipped
            });
            answeredCount.textContent = result.answeredCount;

            if (result.done) {
                await finishSession();   // hết câu hỏi: chấm điểm luôn
            } else {
                showNextQuestion(result.nextNumber, result.nextQuestion);
                setBusy(false);
            }
        } catch (error) {
            showMessage(error.message);
            setBusy(false);
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

        spokenText = '';
        speakSeconds = null;
        answerInput.value = '';
        transcriptText.textContent = PLACEHOLDER;
        toggleTyping(false);
        btnSend.hidden = true;
    }

    // ---------- Kết thúc và chấm điểm ----------
    async function finishSession() {
        showMessage('Đang chấm điểm, vui lòng đợi vài giây...');
        modal.style.display = 'none';
        setBusy(true);
        try {
            const result = await postJson('/Interview/Finish', { sessionId: sessionId, anxietyAfter: null });
            window.location.href = result.redirectUrl;
        } catch (error) {
            showMessage(error.message);
            setBusy(false);
        }
    }

    btnStop.addEventListener('click', function () {
        if (isListening) stopListening();
        modal.style.display = 'flex';
    });
    btnResume.addEventListener('click', function () { modal.style.display = 'none'; });
    btnFinish.addEventListener('click', finishSession);
})();
