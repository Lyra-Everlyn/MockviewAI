// MockViewAI — Buổi 6: Edge Function "doi-check"
// Luồng: Trình duyệt → Edge Function → Crossref API (kho dữ liệu DOI chính thức) → trả kết quả gọn cho trang web.
// Không cần secret: Crossref là API công khai. Function chỉ ĐỌC, không ghi gì vào database.

const CORS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
  'Access-Control-Allow-Methods': 'POST, OPTIONS',
};
// Định dạng DOI chuẩn: 10.<mã nhà xuất bản>/<mã bài>
const DOI_PATTERN = /^10\.\d{4,9}\/[-._;()/:A-Za-z0-9]+$/;

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { ...CORS, 'Content-Type': 'application/json; charset=utf-8' } });
}

Deno.serve(async (req: Request) => {
  if (req.method === 'OPTIONS') return new Response('ok', { headers: CORS });
  if (req.method !== 'POST') return json({ ok: false, error: 'Chỉ nhận yêu cầu POST.' }, 405);

  let doi = '';
  try {
    const body = await req.json();
    doi = String(body?.doi ?? '').trim().replace(/^https?:\/\/(dx\.)?doi\.org\//i, '');
  } catch {
    return json({ ok: false, error: 'Dữ liệu gửi lên không đúng định dạng JSON.' }, 400);
  }
  if (!doi || doi.length > 200 || !DOI_PATTERN.test(doi)) {
    return json({ ok: false, error: 'DOI không đúng định dạng (ví dụ đúng: 10.1111/ijsa.12365).' }, 400);
  }

  // Crossref khuyến khích ghi email liên hệ (không phải thông tin bí mật) để vào "polite pool".
  const mailto = Deno.env.get('CROSSREF_MAILTO');
  const path = doi.split('/').map(encodeURIComponent).join('/');
  const url = `https://api.crossref.org/works/${path}${mailto ? `?mailto=${encodeURIComponent(mailto)}` : ''}`;

  try {
    const res = await fetch(url, {
      headers: { 'User-Agent': 'MockViewAI/1.0 (student project; MindX Vibecode)' },
      signal: AbortSignal.timeout(8000),
    });
    if (res.status === 404) return json({ ok: true, found: false, doi });
    if (!res.ok) return json({ ok: false, error: `Crossref đang trả về lỗi ${res.status}. Thử lại sau.` }, 502);
    const m = (await res.json()).message ?? {};
    const parts = m.issued?.['date-parts']?.[0] ?? m.published?.['date-parts']?.[0] ?? [];
    const authors = (m.author ?? []) as Array<{ family?: string; given?: string; name?: string }>;
    return json({
      ok: true,
      found: true,
      doi: m.DOI ?? doi,
      title: Array.isArray(m.title) ? m.title[0] : null,
      journal: Array.isArray(m['container-title']) ? m['container-title'][0] : null,
      year: parts[0] ?? null,
      authors: authors.slice(0, 3).map((a) => a.family ? `${a.family}${a.given ? ', ' + a.given : ''}` : (a.name ?? '')),
      authorCount: authors.length,
      publisher: m.publisher ?? null,
      type: m.type ?? null,
    });
  } catch (_err) {
    return json({ ok: false, error: 'Không kết nối được Crossref (quá thời gian chờ). Thử lại sau.' }, 504);
  }
});
