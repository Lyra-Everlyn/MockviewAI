/* MockViewAI — xử lý mọi link đăng nhập (href bắt đầu bằng https://mockviewai.com/Auth/Login):
   1. Gắn ReturnUrl = trang đang đọc, để đăng nhập / đăng ký xong ứng dụng đưa người dùng quay lại đúng trang này.
      Ứng dụng chỉ quay về những địa chỉ có trong LandingPage:AllowedReturnOrigins (appsettings.json).
   2. Khi landing chạy trên máy (Live Server: 127.0.0.1 / localhost) → trỏ sang ứng dụng chạy trên máy
      (Visual Studio, profile "http": http://localhost:9090) để test. Đưa lên web thì vẫn là mockviewai.com. */
(() => {
  if (location.protocol !== 'https:' && location.protocol !== 'http:') return; // mở bằng file:// thì không có địa chỉ để quay về
  const PROD_APP = 'https://mockviewai.com';
  const LOCAL_APP = 'http://localhost:9090';
  const isLocal = location.hostname === '127.0.0.1' || location.hostname === 'localhost';
  const app = isLocal ? LOCAL_APP : PROD_APP;

  document.querySelectorAll('a[href^="' + PROD_APP + '/Auth/Login"]').forEach((link) => {
    const src = new URL(link.href);
    const url = new URL(src.pathname + src.search, app);
    url.searchParams.set('ReturnUrl', location.href);
    link.href = url.toString();
  });
})();
