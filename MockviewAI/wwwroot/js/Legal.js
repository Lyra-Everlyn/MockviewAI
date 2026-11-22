//#region Legal
/// <summary>
/// Trang pháp lý dùng chung cho Views/Home/Terms.cshtml và Privacy.cshtml:
/// thời gian đọc, thanh tiến độ, mục lục tự đánh dấu (scrollspy), nút lên đầu trang, in / lưu PDF.
/// </summary>
//#endregion
(function () {
    'use strict';

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const scrollBehavior = prefersReducedMotion ? 'auto' : 'smooth';

    const progressBar = document.querySelector('[data-legal-progress]');
    const content = document.querySelector('[data-legal-content]');
    const sections = Array.from(document.querySelectorAll('[data-legal-section]'));
    const tocLinks = Array.from(document.querySelectorAll('[data-legal-toc-link]'));
    const toc = document.querySelector('.legal-toc');
    const tocToggle = document.querySelector('[data-legal-toc-toggle]');
    const topButton = document.querySelector('[data-legal-top]');
    const printButton = document.querySelector('[data-legal-print]');
    const readTime = document.querySelector('[data-legal-read-time]');

    //#region Thời gian đọc (~200 từ/phút)
    if (content && readTime) {
        const words = content.textContent.trim().split(/\s+/).length;
        readTime.textContent = '~' + Math.max(1, Math.round(words / 200)) + ' phút đọc';
    }
    //#endregion

    //#region Tiến độ đọc + hiện nút lên đầu trang
    let ticking = false;

    function updateOnScroll() {
        const doc = document.documentElement;
        const max = doc.scrollHeight - doc.clientHeight;
        const ratio = max > 0 ? Math.min(1, window.scrollY / max) : 0;

        if (progressBar) progressBar.style.transform = 'scaleX(' + ratio + ')';
        if (topButton) topButton.hidden = window.scrollY < 480;

        ticking = false;
    }

    window.addEventListener('scroll', function () {
        if (!ticking) {
            window.requestAnimationFrame(updateOnScroll);
            ticking = true;
        }
    }, { passive: true });

    updateOnScroll();
    //#endregion

    //#region Scrollspy
    function setActive(id) {
        tocLinks.forEach(function (link) {
            const isActive = link.getAttribute('href') === '#' + id;
            link.classList.toggle('is-active', isActive);
            if (isActive) {
                link.setAttribute('aria-current', 'true');
            } else {
                link.removeAttribute('aria-current');
            }
        });
    }

    if ('IntersectionObserver' in window && sections.length) {
        const visible = new Map();

        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    visible.set(entry.target.id, entry.boundingClientRect.top);
                } else {
                    visible.delete(entry.target.id);
                }
            });

            // Chọn mục gần đỉnh màn hình nhất trong số đang hiển thị
            if (visible.size) {
                const topMost = Array.from(visible.entries())
                    .sort(function (a, b) { return a[1] - b[1]; })[0][0];
                setActive(topMost);
            }
        }, {
            rootMargin: '-90px 0px -55% 0px',
            threshold: 0
        });

        sections.forEach(function (section) { observer.observe(section); });
    }
    //#endregion

    //#region Mục lục: cuộn mượt + đóng trên mobile
    function setTocOpen(open) {
        if (!toc || !tocToggle) return;
        toc.classList.toggle('is-open', open);
        tocToggle.setAttribute('aria-expanded', String(open));
    }

    if (tocToggle) {
        tocToggle.addEventListener('click', function () {
            setTocOpen(!toc.classList.contains('is-open'));
        });
    }

    tocLinks.forEach(function (link) {
        link.addEventListener('click', function (event) {
            const id = link.getAttribute('href').slice(1);
            const target = document.getElementById(id);
            if (!target) return;

            event.preventDefault();
            target.scrollIntoView({ behavior: scrollBehavior, block: 'start' });
            history.replaceState(null, '', '#' + id);
            setActive(id);
            setTocOpen(false);

            // Chuyển focus cho người dùng bàn phím / trình đọc màn hình
            target.setAttribute('tabindex', '-1');
            target.focus({ preventScroll: true });
        });
    });

    // Mở trang với #anchor có sẵn
    if (location.hash) {
        const initial = document.getElementById(location.hash.slice(1));
        if (initial) setActive(initial.id);
    }

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && toc && toc.classList.contains('is-open')) {
            setTocOpen(false);
            tocToggle.focus();
        }
    });
    //#endregion

    //#region Nút lên đầu trang và in
    if (topButton) {
        topButton.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: scrollBehavior });
        });
    }

    if (printButton) {
        printButton.addEventListener('click', function () {
            window.print();
        });
    }
    //#endregion
})();
