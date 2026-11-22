//#region CvLibrary
/// <summary>
/// [USER CV] 
/// Trang "CV của tôi": hiển thị, chọn mặc định, xoá CV lưu trên trình duyệt.
/// Dữ liệu CV chỉ được đưa ra bằng textContent.
/// </summary>
//#endregion
document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    const CV = window.MockviewCv;
    const list = document.getElementById('cvList');
    const empty = document.getElementById('cvEmpty');
    const count = document.getElementById('cvCount');
    const fullNotice = document.getElementById('cvFullNotice');
    const storageError = document.getElementById('cvStorageError');
    const uploadLink = document.getElementById('cvUploadLink');
    if (!CV || !list) return;

    //#region DOM helpers
    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = String(text);
        return node;
    }

    function icon(classes) {
        const i = el('i', classes);
        i.setAttribute('aria-hidden', 'true');
        return i;
    }

    function button(label, iconClass, className) {
        const b = el('button', 'button ' + className);
        b.type = 'button';
        b.append(icon(iconClass), document.createTextNode(' ' + label));
        return b;
    }

    function formatDate(date) {
        return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    }

    function asList(value) {
        return Array.isArray(value) ? value.filter(v => v !== null && v !== undefined && String(v).trim() !== '') : [];
    }
    //#endregion

    //#region Dialogs
    async function confirmDelete(title) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            const result = await window.Swal.fire({
                icon: 'warning',
                title: 'Xoá CV này?',
                text: '"' + title + '" sẽ bị xoá khỏi trình duyệt và không khôi phục được.',
                showCancelButton: true,
                confirmButtonText: 'Xoá CV',
                cancelButtonText: 'Huỷ',
                focusCancel: true
            });
            return result.isConfirmed;
        }
        return window.confirm('Xoá CV "' + title + '"? Thao tác này không khôi phục được.');
    }

    function toast(message) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            window.Swal.fire({ icon: 'success', title: message, timer: 1800, showConfirmButton: false });
        }
    }
    //#endregion

    //#region Details
    function section(parent, label, iconClass) {
        const wrap = el('div', 'cv-library__section');
        const heading = el('div', 'cv-section-label');
        heading.append(icon(iconClass), document.createTextNode(' ' + label));
        wrap.append(heading);
        parent.append(wrap);
        return wrap;
    }

    function bulletList(parent, lines) {
        const ul = el('ul', 'cv-review__tips');
        lines.forEach(line => ul.append(el('li', null, line)));
        parent.append(ul);
    }

    /// <summary>Một mục có tiêu đề, dòng phụ, mô tả và danh sách gạch đầu dòng (dự án, kinh nghiệm).</summary>
    function entry(parent, title, meta, description, bullets) {
        const box = el('div', 'cv-library__entry');
        box.append(el('strong', null, title));
        if (meta) box.append(el('small', null, meta));
        if (description) box.append(el('p', null, description));
        const lines = asList(bullets);
        if (lines.length) {
            const ul = el('ul');
            lines.forEach(line => ul.append(el('li', null, line)));
            box.append(ul);
        }
        parent.append(box);
    }

    function buildDetails(data) {
        const box = el('div', 'cv-library__details');
        box.hidden = true;

        if (data.fullName) box.append(el('p', null, 'Họ tên: ' + data.fullName));
        if (data.totalYearsOfExperience) box.append(el('p', null, 'Kinh nghiệm: khoảng ' + data.totalYearsOfExperience + ' năm'));
        if (data.careerObjective) {
            section(box, 'Mục tiêu nghề nghiệp', 'fa-regular fa-flag').append(el('p', null, data.careerObjective));
        }

        const hard = asList(data.hardSkills);
        const soft = asList(data.softSkills);
        if (hard.length || soft.length) {
            const s = section(box, 'Kỹ năng', 'fa-solid fa-code');
            if (hard.length) s.append(el('p', null, 'Chuyên môn: ' + hard.join(', ')));
            if (soft.length) s.append(el('p', null, 'Kỹ năng mềm: ' + soft.join(', ')));
        }

        const jobs = asList(data.workExperiences);
        if (jobs.length) {
            const s = section(box, 'Kinh nghiệm làm việc', 'fa-solid fa-briefcase');
            jobs.forEach(j => entry(s,
                [j.jobTitle, j.companyName].filter(Boolean).join(' · '),
                j.duration,
                j.mainResponsibilities,
                j.achievements));
        }

        const projects = asList(data.projects);
        if (projects.length) {
            const s = section(box, 'Dự án', 'fa-solid fa-diagram-project');
            projects.forEach(p => {
                const tech = asList(p.technologies);
                entry(s,
                    p.name,
                    [p.context, p.role, p.duration].filter(Boolean).join(' · '),
                    p.description,
                    asList(p.responsibilities).concat(tech.length ? ['Công nghệ: ' + tech.join(', ')] : []));
            });
        }

        const schools = asList(data.educations);
        if (schools.length) {
            bulletList(section(box, 'Học vấn', 'fa-solid fa-graduation-cap'), schools.map(e =>
                [e.schoolName, e.major, e.degree].filter(Boolean).join(' · ') + (e.gpa ? ' (GPA ' + e.gpa + ')' : '')));
        }

        const certs = asList(data.certificationsAndAwards);
        if (certs.length) {
            bulletList(section(box, 'Chứng chỉ & giải thưởng', 'fa-solid fa-award'), certs.map(c =>
                [c.title, c.issuer].filter(Boolean).join(' · ') + (c.year ? ' (' + c.year + ')' : '')));
        }
        return box;
    }
    //#endregion

    //#region Card
    function buildCard(item) {
        const data = item.data || {};
        const title = CV.titleOf(item);
        const card = el('article', 'cv-card cv-library__item' + (item.isDefault ? ' is-default' : ''));

        const head = el('div', 'cv-card__heading');
        const iconBox = el('span', 'cv-card__icon');
        iconBox.append(icon('fa-regular fa-file-lines'));

        const text = el('div');
        const h2 = el('h2', null, title);
        if (item.isDefault) {
            const badge = el('span', 'cv-phase cv-library__badge');
            badge.append(icon('fa-solid fa-star'), document.createTextNode(' Mặc định'));
            h2.append(document.createTextNode(' '), badge);
        }
        const meta = [data.fullName, asList(data.hardSkills).length + ' kỹ năng',
        'Lưu ngày ' + formatDate(new Date(item.savedAt)), 'Hết hạn ' + formatDate(CV.expiresAt(item))]
            .filter(Boolean).join(' · ');
        text.append(h2, el('p', null, meta));
        head.append(iconBox, text);

        const details = buildDetails(data);
        const actions = el('div', 'cv-review__actions cv-library__actions');

        const viewBtn = button('Xem chi tiết', 'fa-regular fa-eye', 'button--secondary cv-button-secondary');
        viewBtn.setAttribute('aria-expanded', 'false');
        viewBtn.addEventListener('click', () => {
            details.hidden = !details.hidden;
            viewBtn.setAttribute('aria-expanded', String(!details.hidden));
        });
        actions.append(viewBtn);

        if (!item.isDefault) {
            const defaultBtn = button('Dùng CV này', 'fa-regular fa-star', 'button--secondary cv-button-secondary');
            defaultBtn.addEventListener('click', () => {
                CV.setDefault(item.id);
                render();
                toast('Đã chọn CV mặc định');
            });
            actions.append(defaultBtn);
        }

        const deleteBtn = button('Xoá', 'fa-regular fa-trash-can', 'button--secondary cv-button-secondary');
        deleteBtn.addEventListener('click', async () => {
            if (!(await confirmDelete(title))) return;
            CV.remove(item.id);
            render();
            toast('Đã xoá CV');
        });
        actions.append(deleteBtn);

        card.append(head, actions, details);
        return card;
    }
    //#endregion

    //#region Render
    function render() {
        const usable = CV.hasOwner && CV.isAvailable();
        storageError.hidden = usable;

        const items = usable ? CV.load() : [];
        list.replaceChildren(...items.map(buildCard));
        empty.hidden = !usable || items.length > 0;
        count.textContent = items.length + '/' + CV.max + ' CV';

        const blocked = !usable || items.length >= CV.max;
        fullNotice.hidden = !usable || items.length < CV.max;
        uploadLink.classList.toggle('is-disabled', blocked);
        uploadLink.setAttribute('aria-disabled', String(blocked));
        if (blocked) uploadLink.setAttribute('tabindex', '-1'); else uploadLink.removeAttribute('tabindex');
    }

    uploadLink.addEventListener('click', event => {
        if (uploadLink.getAttribute('aria-disabled') === 'true') event.preventDefault();
    });

    // Đồng bộ khi CV thay đổi ở tab khác
    window.addEventListener('storage', event => {
        if (!event.key || event.key.indexOf('mockview.cv.') === 0) render();
    });

    render();

    try {
        if (sessionStorage.getItem('mockview.cv.flash') === 'saved') {
            sessionStorage.removeItem('mockview.cv.flash');
            toast('Đã lưu CV vào trình duyệt');
        }
    } catch (e) { }
    //#endregion
});
