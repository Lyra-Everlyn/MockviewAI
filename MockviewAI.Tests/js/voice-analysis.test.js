// Test Đợt 7 cho wwwroot/js/VoiceAnalysis.js (chạy: node MockviewAI.Tests/js/voice-analysis.test.js)
// Dùng sóng âm tổng hợp: biết trước độ to (dB) và cao độ (Hz) để kiểm tra phép đo.
const assert = require('assert');
const VA = require('../../MockviewAI/wwwroot/js/VoiceAnalysis.js');

const RATE = 48000, N = 2048;
function sine(freq, amp, n = N, rate = RATE) {
    const a = new Float32Array(n);
    for (let i = 0; i < n; i++) a[i] = amp * Math.sin(2 * Math.PI * freq * i / rate);
    return a;
}
function noise(amp, n = N) {
    const a = new Float32Array(n);
    let s = 7;
    for (let i = 0; i < n; i++) { s = (s * 16807) % 2147483647; a[i] = amp * ((s / 2147483647) * 2 - 1); }
    return a;
}
let passed = 0;
function test(name, fn) { fn(); passed++; console.log('  ✓ ' + name); }

test('rmsDb: sóng sin biên độ 1 ≈ -3 dB, biên độ 0.01 ≈ -43 dB, im lặng = -100', () => {
    assert.ok(Math.abs(VA.rmsDb(sine(200, 1)) - (-3.01)) < 0.2);
    assert.ok(Math.abs(VA.rmsDb(sine(200, 0.01)) - (-43.01)) < 0.2);
    assert.strictEqual(VA.rmsDb(new Float32Array(N)), -100);
});

test('estimatePitch: tìm đúng cao độ 120 Hz / 220 Hz (sai < 3%)', () => {
    for (const f of [120, 220]) {
        const p = VA.estimatePitch(sine(f, 0.3), RATE);
        assert.ok(p && Math.abs(p - f) / f < 0.03, `muốn ${f}, ra ${p}`);
    }
});

test('estimatePitch: giọng có hoạ âm (giống giọng người) không nhầm quãng tám', () => {
    for (const f of [95, 110, 180, 260]) {
        const a = new Float32Array(N);
        for (let h = 1; h <= 8; h++) {   // hoạ âm 2, 3... mạnh gần bằng âm gốc
            const amp = 0.25 / Math.pow(h, 0.7);
            for (let i = 0; i < N; i++) a[i] += amp * Math.sin(2 * Math.PI * f * h * i / RATE + h);
        }
        const p = VA.estimatePitch(a, RATE);
        assert.ok(p && Math.abs(p - f) / f < 0.03, `muốn ${f}, ra ${p}`);
    }
});

test('estimatePitch: tiếng ồn không có cao độ -> null', () => {
    assert.strictEqual(VA.estimatePitch(noise(0.3), RATE), null);
});

// Mô phỏng một câu trả lời: danh sách khung (mỗi khung ~100ms) {db, pitch}
function frames(list) { return list.map(([db, pitch]) => ({ db, pitch })); }

test('summarize: nói đủ to -> mức "Vừa", tỉ lệ nói nhỏ 0', () => {
    const f = frames(Array.from({ length: 40 }, (_, i) => [-26, 150 + (i % 8) * 12]).concat(Array(10).fill([-65, null])));
    const s = VA.summarize(f, 0.1);
    assert.strictEqual(VA.levelLabel(s.levelDb), 'Vừa');
    assert.strictEqual(s.quietRatio, 0);
    assert.ok(Math.abs(s.voicedSeconds - 4) < 0.01, 'chỉ tính khung có tiếng nói: 40 x 0.1s');
});

test('summarize: nói nhỏ phần lớn thời gian -> quietRatio cao, mức "Nhỏ"', () => {
    const f = frames(Array(30).fill([-44, 160]).concat(Array(10).fill([-30, 160])).concat(Array(20).fill([-70, null])));
    const s = VA.summarize(f, 0.1);
    assert.ok(s.quietRatio > 0.7 && s.quietRatio < 0.8, 'quietRatio=' + s.quietRatio);
    assert.strictEqual(VA.levelLabel(s.levelDb), 'Nhỏ');
});

test('summarize: giọng đều đều (cao độ gần như không đổi) -> độ lên xuống thấp', () => {
    const flat = VA.summarize(frames(Array.from({ length: 40 }, (_, i) => [-25, 150 + (i % 2)])), 0.1);
    const lively = VA.summarize(frames(Array.from({ length: 40 }, (_, i) => [-25, 120 + (i % 10) * 15])), 0.1);
    assert.ok(flat.pitchVariation < 1, 'đều đều: ' + flat.pitchVariation);
    assert.ok(lively.pitchVariation > 2, 'tự nhiên: ' + lively.pitchVariation);
    assert.strictEqual(VA.toneLabel(flat.pitchVariation), 'Đều đều');
    assert.strictEqual(VA.toneLabel(lively.pitchVariation), 'Tự nhiên');
});

test('summarize: quá ít tiếng nói -> không kết luận (null), tránh chấm oan', () => {
    const s = VA.summarize(frames(Array(5).fill([-25, 150]).concat(Array(50).fill([-70, null]))), 0.1);
    assert.strictEqual(s, null);
});

test('summarize: tiếng ồn nền to vẫn tách được tiếng nói (ngưỡng theo nền ồn)', () => {
    // nền ồn -45 dB (quạt, điều hoà), nói -22 dB
    const s = VA.summarize(frames(Array(30).fill([-45, null]).concat(Array(30).fill([-22, 140]))), 0.1);
    assert.ok(Math.abs(s.voicedSeconds - 3) < 0.01, 'voiced=' + s.voicedSeconds);
    assert.strictEqual(s.quietRatio, 0);
});

test('isQuietNow: 2 giây gần nhất phần lớn nhỏ -> nhắc; nói đủ to -> không nhắc', () => {
    const quiet = frames(Array(10).fill([-26, 150]).concat(Array(20).fill([-44, 150])));
    const loud = frames(Array(30).fill([-26, 150]));
    assert.strictEqual(VA.isQuietNow(quiet, 0.1), true);
    assert.strictEqual(VA.isQuietNow(loud, 0.1), false);
    assert.strictEqual(VA.isQuietNow(frames(Array(5).fill([-44, 150])), 0.1), false, 'mới nói 0.5s: chưa nhắc');
});

// Kiểm tra micro: câu nói ngắn (~1 giây có tiếng) vẫn ra mức âm lượng khi hạ ngưỡng
test('summarize: hạ ngưỡng thời lượng cho phần nói thử micro', () => {
    const frames = [];
    for (let i = 0; i < 5; i++) frames.push({ db: -75, pitch: null });
    for (let i = 0; i < 10; i++) frames.push({ db: -24, pitch: 150 });
    assert.strictEqual(VA.summarize(frames, 0.1), null);              // mặc định cần 1.5 giây
    const s = VA.summarize(frames, 0.1, 0.6);
    assert.ok(s && Math.abs(s.levelDb + 24) < 0.01, JSON.stringify(s));
    assert.strictEqual(VA.levelLabel(s.levelDb), 'Vừa');
});

console.log(`\n${passed} test đạt`);
