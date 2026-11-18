/* =========================================================
   MockviewAI — Terms page interactions
   ========================================================= */

(() => {
    "use strict";

    const progressBar = document.getElementById("termsProgressBar");
    const backToTop = document.getElementById("backToTop");
    const printButton = document.getElementById("printTerms");
    const toc = document.getElementById("termsToc");

    if (!progressBar || !backToTop || !toc) return;

    const links = Array.from(toc.querySelectorAll(".terms-toc__link"));
    const sections = links
        .map((link) => document.querySelector(link.getAttribute("href")))
        .filter(Boolean);

    let ticking = false;

    const updatePageState = () => {
        const scrollableHeight =
            document.documentElement.scrollHeight - window.innerHeight;

        const progress = scrollableHeight > 0
            ? (window.scrollY / scrollableHeight) * 100
            : 0;

        progressBar.style.width = `${Math.min(100, Math.max(0, progress))}%`;
        backToTop.classList.toggle("is-visible", window.scrollY > 420);

        let currentSection = sections[0];

        sections.forEach((section) => {
            if (section.getBoundingClientRect().top <= 150) {
                currentSection = section;
            }
        });

        if (currentSection) {
            links.forEach((link) => {
                const isActive = link.getAttribute("href") === `#${currentSection.id}`;
                link.classList.toggle("is-active", isActive);

                if (isActive) {
                    link.setAttribute("aria-current", "location");
                } else {
                    link.removeAttribute("aria-current");
                }
            });
        }

        ticking = false;
    };

    window.addEventListener("scroll", () => {
        if (!ticking) {
            window.requestAnimationFrame(updatePageState);
            ticking = true;
        }
    }, { passive: true });

    window.addEventListener("resize", updatePageState);

    links.forEach((link) => {
        link.addEventListener("click", (event) => {
            const target = document.querySelector(link.getAttribute("href"));
            if (!target) return;

            event.preventDefault();
            target.scrollIntoView({ behavior: "smooth", block: "start" });
            history.replaceState(null, "", link.getAttribute("href"));
        });
    });

    backToTop.addEventListener("click", () => {
        window.scrollTo({ top: 0, behavior: "smooth" });
    });

    printButton?.addEventListener("click", () => window.print());

    updatePageState();
})();
