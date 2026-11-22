//#region VoiceAnalysis
/// <summary>
/// [VOICE] 
// Đo ÂM LƯỢNG và GIỌNG ĐIỆU từ micro bằng Web Audio (không dùng AI, chạy trên trình duyệt).
/// Chỉ gồm phép tính thuần, không đụng giao diện, nên test được bằng Node: node MockviewAI.Tests/js/voice-analysis.test.js
/// Ngưỡng phải khớp với Models/VoiceMetrics.cs.
/// Giới hạn: dB là tương đối theo micro từng máy (không phải decibel đo ngoài đời), và là mức SAU khi trình duyệt
/// tự khuếch đại (InterviewRoom.js bật autoGainControl để Whisper nghe rõ hơn).
/// </summary>
//#endregion
(function (root, factory) {
    if (typeof module === 'object' && module.exports) module.exports = factory();
    else root.VoiceAnalysis = factory();
})(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    //#region Ngưỡng
    const QUIET_DB = -38;        // nói nhỏ hơn mức này (dBFS) = khó nghe
    const LOUD_DB = -10;         // to hơn mức này = dễ rè / vỡ tiếng
    const MONOTONE_ST = 1.5;     // độ lên xuống giọng < 1.5 nửa cung = đều đều
    const LIVELY_ST = 6;         // > 6 nửa cung = nhấn nhá rất nhiều
    const MIN_VOICED_SECONDS = 1.5;
    const MIN_PITCH_FRAMES = 10;
    const MIN_HZ = 75, MAX_HZ = 400;   // khoảng cao độ giọng nói người
    //#endregion

    //#region Đo một khung âm thanh
    /// <summary>Mức to nhỏ của một khung (dBFS: 0 = lớn nhất micro thu được; im lặng = -100).</summary>
    function rmsDb(samples) {
        let sum = 0;
        for (let i = 0; i < samples.length; i++) sum += samples[i] * samples[i];
        const rms = Math.sqrt(sum / samples.length);
        return rms < 1e-5 ? -100 : 20 * Math.log10(rms);
    }

    /// <summary>Cao độ (Hz) bằng tự tương quan: tìm chu kỳ lặp lại của sóng. Không rõ chu kỳ (tiếng ồn, phụ âm) -> null.</summary>
    function estimatePitch(samples, sampleRate) {
        const n = samples.length;
        const minLag = Math.floor(sampleRate / MAX_HZ);
        const maxLag = Math.min(Math.floor(sampleRate / MIN_HZ), n - 1);
        const corr = new Float32Array(maxLag + 2);
        let best = -1, bestLag = -1;
        for (let lag = minLag; lag <= maxLag; lag++) {
            let sxy = 0, sxx = 0, syy = 0;
            for (let i = 0; i + lag < n; i++) {
                const x = samples[i], y = samples[i + lag];
                sxy += x * y; sxx += x * x; syy += y * y;
            }
            const r = sxx > 0 && syy > 0 ? sxy / Math.sqrt(sxx * syy) : 0;
            corr[lag] = r;
            if (r > best) { best = r; bestLag = lag; }
        }
        if (best < 0.6 || bestLag < 0) return null;
        // Tránh nhầm quãng tám: lấy đỉnh đầu tiên đạt >= 90% đỉnh cao nhất
        for (let lag = minLag + 1; lag < bestLag; lag++) {
            if (corr[lag] >= 0.9 * best && corr[lag] >= corr[lag - 1] && corr[lag] >= corr[lag + 1]) { bestLag = lag; break; }
        }
        // Nội suy parabol quanh đỉnh cho chính xác hơn
        let lag = bestLag;
        if (bestLag > minLag && bestLag < maxLag) {
            const a = corr[bestLag - 1], b = corr[bestLag], c = corr[bestLag + 1];
            const d = a - 2 * b + c;
            if (d !== 0) lag = bestLag + 0.5 * (a - c) / d;
        }
        return sampleRate / lag;
    }
    //#endregion

    //#region Tổng hợp nhiều khung
    function percentile(sorted, p) {
        if (sorted.length === 0) return null;
        const idx = (sorted.length - 1) * p;
        const lo = Math.floor(idx), hi = Math.ceil(idx);
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
    }

    /// <summary>Ngưỡng "có tiếng nói": cao hơn nền ồn 10 dB. Nói liền không có lúc im thì coi nền ồn là -70 dB.</summary>
    function speechThreshold(frames) {
        const dbs = frames.map(f => f.db).sort((a, b) => a - b);
        const p10 = percentile(dbs, 0.1), median = percentile(dbs, 0.5);
        const noiseFloor = p10 !== null && p10 < median - 8 ? p10 : -70;
        return noiseFloor + 10;
    }

    function round(x, digits) { const k = Math.pow(10, digits); return Math.round(x * k) / k; }

    /// <summary>
    /// Tóm tắt một câu trả lời từ các khung { db, pitch }. Quá ít tiếng nói -> null (không kết luận, tránh chấm oan).
    /// minVoicedSeconds mặc định 1,5 giây; phần "Kiểm tra micro" chỉ nói 1 câu ngắn nên truyền số nhỏ hơn.
    /// </summary>
    /// <returns>{ levelDb, quietRatio, pitchVariation, voicedSeconds } hoặc null.</returns>
    function summarize(frames, frameSeconds, minVoicedSeconds) {
        if (!frames || frames.length === 0) return null;
        const minVoiced = typeof minVoicedSeconds === 'number' ? minVoicedSeconds : MIN_VOICED_SECONDS;
        const threshold = speechThreshold(frames);
        const voiced = frames.filter(f => f.db >= threshold);
        const voicedSeconds = voiced.length * frameSeconds;
        if (voicedSeconds < minVoiced - 1e-9) return null;

        const dbs = voiced.map(f => f.db).sort((a, b) => a - b);
        const quiet = voiced.filter(f => f.db < QUIET_DB).length;

        let pitchVariation = null;
        const pitches = voiced.map(f => f.pitch).filter(p => p && p >= MIN_HZ && p <= MAX_HZ);
        if (pitches.length >= MIN_PITCH_FRAMES) {
            const med = percentile(pitches.slice().sort((a, b) => a - b), 0.5);
            const semis = pitches.map(p => 12 * Math.log2(p / med));
            const mean = semis.reduce((s, x) => s + x, 0) / semis.length;
            pitchVariation = round(Math.sqrt(semis.reduce((s, x) => s + (x - mean) * (x - mean), 0) / semis.length), 1);
        }
        return {
            levelDb: round(percentile(dbs, 0.5), 1),
            quietRatio: round(quiet / voiced.length, 2),
            pitchVariation: pitchVariation,
            voicedSeconds: round(voicedSeconds, 1)
        };
    }

    /// <summary>Đang nói nhỏ ngay lúc này? Xét khoảng 2 giây gần nhất, cần ít nhất 1,5 giây có tiếng nói.</summary>
    function isQuietNow(frames, frameSeconds, windowSeconds) {
        const win = Math.round((windowSeconds || 2) / frameSeconds);
        const recent = frames.slice(-win);
        const threshold = speechThreshold(frames);
        const voiced = recent.filter(f => f.db >= threshold);
        if (voiced.length * frameSeconds < MIN_VOICED_SECONDS) return false;
        return voiced.filter(f => f.db < QUIET_DB).length / voiced.length >= 0.6;
    }
    //#endregion

    //#region Nhãn hiển thị
    function levelLabel(db) {
        if (db === null || db === undefined) return '—';
        if (db < QUIET_DB) return 'Nhỏ';
        if (db > LOUD_DB) return 'Quá to';
        return 'Vừa';
    }

    function toneLabel(semitones) {
        if (semitones === null || semitones === undefined) return '—';
        if (semitones < MONOTONE_ST) return 'Đều đều';
        if (semitones > LIVELY_ST) return 'Nhấn nhá nhiều';
        return 'Tự nhiên';
    }
    //#endregion

    return {
        QUIET_DB, LOUD_DB, MONOTONE_ST, LIVELY_ST,
        rmsDb, estimatePitch, summarize, isQuietNow, levelLabel, toneLabel
    };
});
