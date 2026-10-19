/* [AI-MODULE] Avatar người phỏng vấn (SVG 2D, miệng cử động theo âm lượng)
 *
 * Cách dùng (cho bạn làm phòng phỏng vấn):
 *   <link rel="stylesheet" href="/ai/avatar.css">
 *   <div id="face"></div>
 *   <script src="/ai/avatar.js"></script>
 *   <script>
 *     const face = MockviewAvatar.mount(document.getElementById('face'));
 *     face.setState('speaking');          // 'idle' | 'listening' | 'thinking' | 'speaking'
 *     face.attachAudio(audioElement);     // miệng theo âm thanh thật (giọng Google TTS phát bằng <audio>)
 *     face.speak('Xin chào');             // giọng có sẵn của trình duyệt, để thử khi chưa có TTS
 *     face.listenMic();                   // vòng sáng theo âm lượng micro khi sinh viên trả lời
 *   </script>
 */
(function (global) {
  'use strict';

  var uid = 0;

  function template(id) {
    return '' +
      '<svg viewBox="0 0 120 120" role="img" aria-label="Người phỏng vấn AI" xmlns="http://www.w3.org/2000/svg">' +
      '<defs><clipPath id="mvclip' + id + '"><circle cx="60" cy="60" r="58"/></clipPath></defs>' +
      '<g clip-path="url(#mvclip' + id + ')">' +
        '<circle cx="60" cy="60" r="58" fill="#FFE9C2"/>' +
        '<path d="M22 120c4-22 20-32 38-32s34 10 38 32z" fill="#2B4BD6"/>' +                         // áo
        '<path d="M33 70c-2-30 10-44 27-44s29 14 27 44l-6-8H39z" fill="#3B2A22"/>' +                  // tóc sau
        '<circle cx="60" cy="56" r="22" fill="#F2C29B"/>' +                                          // mặt
        '<path d="M38 52c2-14 12-20 22-20s20 6 22 20c-6-6-14-9-22-9s-16 3-22 9z" fill="#3B2A22"/>' +  // tóc mái
        '<g class="mv-eyes"><circle cx="52" cy="58" r="2.4" fill="#3B2A22"/><circle cx="68" cy="58" r="2.4" fill="#3B2A22"/></g>' +
        '<path class="mv-smile" d="M53 66c4 3.5 10 3.5 14 0" stroke="#3B2A22" stroke-width="2.2" fill="none" stroke-linecap="round"/>' +
        '<ellipse class="mv-mouth" cx="60" cy="67" rx="4" ry="1" fill="#7A2E12" opacity="0"/>' +
        '<g class="mv-dots" fill="#2B4BD6"><circle cx="81" cy="35" r="2.2"/><circle cx="88" cy="29" r="2.8"/><circle cx="95" cy="23" r="3.2"/></g>' +
      '</g>' +
      '<circle class="mv-ring" cx="60" cy="60" r="56.5" fill="none" stroke-width="3"/>' +
      '</svg>';
  }

  function clamp01(x) { return x < 0 ? 0 : x > 1 ? 1 : x; }

  function mount(container, options) {
    options = options || {};
    var id = ++uid;
    var root = document.createElement('div');
    root.className = 'mv-avatar mv-state-idle';
    root.innerHTML = template(id);
    container.appendChild(root);

    var smile = root.querySelector('.mv-smile');
    var mouth = root.querySelector('.mv-mouth');
    var ring = root.querySelector('.mv-ring');

    var state = 'idle';
    var target = 0, current = 0;        // mức mở miệng 0..1 (mượt dần về target)
    var ringLevel = 0;                  // 0..1, cho vòng sáng khi nghe micro
    var simulate = false;               // true: giả lập miệng nói (khi dùng giọng trình duyệt, không đo được âm lượng)
    var audioCtx = null;
    var audioSources = new WeakMap();   // mỗi <audio> chỉ nối vào Web Audio được 1 lần
    var analyser = null, buf = null, analyserForMic = false;
    var micStream = null;
    var raf = 0, blinkTimer = 0, destroyed = false;

    function setState(s) {
      state = s;
      root.className = root.className.replace(/mv-state-\w+/g, '').trim() + ' mv-state-' + s;
      if (s !== 'speaking') { simulate = false; if (!analyser || analyserForMic) target = 0; }
    }

    function setMouth(level) { target = clamp01(level); }

    function draw() {
      // mượt: tiến 45% mỗi khung hình về mức mục tiêu
      current += (target - current) * 0.45;
      var open = current > 0.06;
      smile.style.display = open ? 'none' : '';
      mouth.setAttribute('opacity', open ? '1' : '0');
      mouth.setAttribute('ry', (1.2 + current * 6.5).toFixed(2));
      mouth.setAttribute('rx', (4 + current * 2.5).toFixed(2));
    }

    function tick(t) {
      if (destroyed) return;
      raf = requestAnimationFrame(tick);

      if (analyser) {
        analyser.getByteTimeDomainData(buf);
        var sum = 0;
        for (var i = 0; i < buf.length; i++) { var v = (buf[i] - 128) / 128; sum += v * v; }
        var level = clamp01(Math.sqrt(sum / buf.length) * 5);
        if (analyserForMic) { ringLevel = level; } else { target = level; }
      } else if (simulate) {
        var s = t / 1000;
        target = clamp01(0.25 + 0.6 * Math.abs(Math.sin(s * 9)) * (0.6 + 0.4 * Math.sin(s * 2.3)));
      }

      if (analyserForMic || state === 'listening') {
        ring.style.transform = 'scale(' + (1 + ringLevel * 0.04).toFixed(3) + ')';
        ring.style.strokeWidth = (3 + ringLevel * 3).toFixed(2);
      } else {
        ring.style.transform = ''; ring.style.strokeWidth = '';
      }
      draw();
    }

    function scheduleBlink() {
      blinkTimer = setTimeout(function () {
        if (destroyed) return;
        root.classList.add('mv-blink');
        setTimeout(function () { root.classList.remove('mv-blink'); }, 140);
        scheduleBlink();
      }, 2500 + Math.random() * 3000);
    }

    function ensureContext() {
      var AC = global.AudioContext || global.webkitAudioContext;
      if (!AC) throw new Error('Trình duyệt không hỗ trợ Web Audio.');
      if (!audioCtx) audioCtx = new AC();
      if (audioCtx.state === 'suspended') audioCtx.resume();
      return audioCtx;
    }

    // Miệng cử động theo âm thanh thật của một thẻ <audio>/<video>
    function attachAudio(el) {
      var ctx = ensureContext();
      var src = audioSources.get(el);
      if (!src) { src = ctx.createMediaElementSource(el); audioSources.set(el, src); }
      var an = ctx.createAnalyser();
      an.fftSize = 1024;
      src.disconnect();
      src.connect(an);
      an.connect(ctx.destination);       // vẫn phải nghe được tiếng
      var b = new Uint8Array(an.fftSize);

      el.addEventListener('play', function () {
        ensureContext();
        analyser = an; buf = b; analyserForMic = false;
        setState('speaking');
      });
      var stop = function () { if (analyser === an) { analyser = null; } setState('idle'); target = 0; };
      el.addEventListener('pause', stop);
      el.addEventListener('ended', stop);
    }

    // Giọng có sẵn của trình duyệt (chỉ để thử): không đo được âm lượng nên miệng chuyển động giả lập
    function speak(text, opts) {
      if (!('speechSynthesis' in global)) return false;
      opts = opts || {};
      var u = new SpeechSynthesisUtterance(text);
      u.lang = opts.lang || 'vi-VN';
      var voices = global.speechSynthesis.getVoices();
      var v = voices.filter(function (x) { return x.lang && x.lang.toLowerCase().indexOf('vi') === 0; })[0];
      if (v) u.voice = v;
      u.onstart = function () { setState('speaking'); simulate = true; };
      u.onend = u.onerror = function () { simulate = false; setState('idle'); target = 0; };
      global.speechSynthesis.cancel();
      global.speechSynthesis.speak(u);
      return true;
    }

    function stopSpeaking() {
      if ('speechSynthesis' in global) global.speechSynthesis.cancel();
      simulate = false; setState('idle'); target = 0;
    }

    // Vòng sáng theo âm lượng micro khi sinh viên đang trả lời (cần HTTPS hoặc localhost)
    function listenMic() {
      return navigator.mediaDevices.getUserMedia({ audio: true }).then(function (stream) {
        var ctx = ensureContext();
        micStream = stream;
        var an = ctx.createAnalyser();
        an.fftSize = 1024;
        ctx.createMediaStreamSource(stream).connect(an);   // KHÔNG nối ra loa, tránh hú
        analyser = an; buf = new Uint8Array(an.fftSize); analyserForMic = true;
        setState('listening');
        return stream;
      });
    }

    function stopMic() {
      if (micStream) { micStream.getTracks().forEach(function (t) { t.stop(); }); micStream = null; }
      if (analyserForMic) { analyser = null; analyserForMic = false; }
      ringLevel = 0; setState('idle');
    }

    function destroy() {
      destroyed = true; cancelAnimationFrame(raf); clearTimeout(blinkTimer);
      stopMic(); stopSpeaking();
      if (root.parentNode) root.parentNode.removeChild(root);
    }

    raf = requestAnimationFrame(tick);
    scheduleBlink();

    return {
      element: root,
      setState: setState,
      setMouth: setMouth,
      attachAudio: attachAudio,
      speak: speak,
      stopSpeaking: stopSpeaking,
      listenMic: listenMic,
      stopMic: stopMic,
      destroy: destroy,
      get state() { return state; }
    };
  }

  global.MockviewAvatar = { mount: mount };
})(window);
