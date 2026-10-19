# Avatar người phỏng vấn (khuôn mặt)

Tìm nhanh trong VS Code: `Ctrl+Shift+F` rồi gõ `[AI-MODULE]`.

Thư mục `MockviewAI/wwwroot/ai/`. Trang thử: chạy app rồi mở `/ai/avatar.html` (có thể xóa trước khi deploy thật).

```html
<link rel="stylesheet" href="/ai/avatar.css">
<div id="face"></div>
<script src="/ai/avatar.js"></script>
<script>
  const face = MockviewAvatar.mount(document.getElementById('face'));
  face.attachAudio(audioElement);   // miệng cử động theo giọng Google TTS đang phát
  face.setState('thinking');        // 'idle' | 'listening' | 'thinking' | 'speaking'
  face.listenMic();                 // vòng sáng theo âm lượng micro khi sinh viên trả lời
</script>
```

- Âm thanh chỉ phát sau khi người dùng chạm vào trang (trình duyệt chặn tự phát), nên người phỏng vấn nên bắt đầu nói sau khi bấm "Bắt đầu".
- Micro chỉ dùng được trên HTTPS hoặc localhost.
- Giọng đọc có sẵn của trình duyệt (`face.speak`) chỉ để thử; sản phẩm dùng Google Cloud Text-to-Speech theo bộ công cụ đã chốt.

