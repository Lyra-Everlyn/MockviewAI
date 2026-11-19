//#region CvStorage
/// <summary>
/// [USER CV] Lưu CV trên trình duyệt (localStorage), tách theo tài khoản.
/// Cấu hình lấy từ thẻ meta cv-owner / cv-max / cv-keep-days.
/// </summary>
//#endregion
(function (window) {
    'use strict';

    //#region Config
    const PREFIX = 'mockview.cv.v1:';
    const DAY_MS = 24 * 60 * 60 * 1000;

    function meta(name) {
        const el = document.querySelector('meta[name="' + name + '"]');
        return el ? el.getAttribute('content') || '' : '';
    }

    const owner = meta('cv-owner');
    const max = Math.max(1, parseInt(meta('cv-max'), 10) || 3);
    const keepDays = Math.max(1, parseInt(meta('cv-keep-days'), 10) || 30);
    const storageKey = owner ? PREFIX + owner : '';
    //#endregion

    //#region Storage
    function storageAvailable() {
        try {
            const probe = PREFIX + '__probe';
            window.localStorage.setItem(probe, '1');
            window.localStorage.removeItem(probe);
            return true;
        } catch (e) {
            return false;
        }
    }

    function readRaw() {
        if (!storageKey) return [];
        try {
            const parsed = JSON.parse(window.localStorage.getItem(storageKey) || '[]');
            return Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            return [];
        }
    }

    function writeRaw(items) {
        if (!storageKey) throw new Error('no_owner');
        window.localStorage.setItem(storageKey, JSON.stringify(items));
    }
    //#endregion

    //#region Helpers
    function newId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') return window.crypto.randomUUID();
        return 'cv-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 10);
    }

    function isExpired(item, now) {
        const savedAt = Date.parse(item && item.savedAt);
        return !savedAt || now - savedAt > keepDays * DAY_MS;
    }

    /// <summary>Bỏ mục hỏng / hết hạn, giữ tối đa `max` mục, luôn có đúng 1 CV mặc định.</summary>
    function normalize(items) {
        const now = Date.now();
        let list = items
            .filter(i => i && typeof i === 'object' && i.id && i.data && typeof i.data === 'object' && !isExpired(i, now))
            .sort((a, b) => Date.parse(b.savedAt) - Date.parse(a.savedAt))
            .slice(0, max);

        const defaults = list.filter(i => i.isDefault);
        if (list.length > 0 && defaults.length !== 1) {
            const keep = defaults.length > 1 ? defaults[0].id : list[0].id;
            list = list.map(i => Object.assign({}, i, { isDefault: i.id === keep }));
        }
        return list;
    }

    function load() {
        const raw = readRaw();
        const list = normalize(raw);
        if (storageKey && JSON.stringify(list) !== JSON.stringify(raw)) {
            try { writeRaw(list); } catch (e) { }
        }
        return list;
    }

    function titleOf(item) {
        const d = (item && item.data) || {};
        return (d.targetJobTitle || '').trim() || (d.fullName || '').trim() || 'CV chưa đặt tên';
    }

    function expiresAt(item) {
        return new Date(Date.parse(item.savedAt) + keepDays * DAY_MS);
    }
    //#endregion

    //#region Public API
    /// <summary>Thêm CV (thành mặc định). reason khi lỗi: 'full' | 'no_owner' | 'storage'.</summary>
    function add(data) {
        if (!storageKey) return { ok: false, reason: 'no_owner' };
        if (!storageAvailable()) return { ok: false, reason: 'storage' };

        const list = load();
        if (list.length >= max) return { ok: false, reason: 'full' };

        const item = { id: newId(), savedAt: new Date().toISOString(), isDefault: true, data: data };
        try {
            writeRaw([item].concat(list.map(i => Object.assign({}, i, { isDefault: false }))));
        } catch (e) {
            return { ok: false, reason: 'storage' };
        }
        return { ok: true, item: item };
    }

    function remove(id) {
        writeRaw(normalize(load().filter(i => i.id !== id)));
    }

    function setDefault(id) {
        writeRaw(load().map(i => Object.assign({}, i, { isDefault: i.id === id })));
    }

    function getDefault() {
        return load().find(i => i.isDefault) || null;
    }

    function clearAllAccounts() {
        try {
            const keys = [];
            for (let i = 0; i < window.localStorage.length; i++) {
                const k = window.localStorage.key(i);
                if (k && k.indexOf(PREFIX) === 0) keys.push(k);
            }
            keys.forEach(k => window.localStorage.removeItem(k));
        } catch (e) { }
    }

    window.MockviewCv = {
        max: max,
        keepDays: keepDays,
        hasOwner: !!storageKey,
        isAvailable: storageAvailable,
        load: load,
        count: () => load().length,
        isFull: () => load().length >= max,
        add: add,
        remove: remove,
        setDefault: setDefault,
        getDefault: getDefault,
        titleOf: titleOf,
        expiresAt: expiresAt,
        clearAllAccounts: clearAllAccounts
    };
    //#endregion
})(window);
