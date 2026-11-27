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
    const slots = document.getElementById('cvSlots');
    if (!CV || !list) return;

    const DAY_MS = 24 * 60 * 60 * 1000;

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

    /// <summary>Số ngày còn lại trước khi trình duyệt tự xoá CV.</summary>
    function daysLeftText(item) {
        const days = Math.ceil((CV.expiresAt(item).getTime() - Date.now()) / DAY_MS);
        return days > 0 ? 'Còn ' + days + ' ngày' : 'Hết hạn hôm nay';
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
    /// <summary>Một phần có tiêu đề ◆ (dùng chung cho cột chính và cột phụ).</summary>
    function section(parent, label) {
        const wrap = el('section', 'cv-detail__section');
        wrap.append(el('h3', 'cv-detail__heading section-header__title--marked', label));
        parent.append(wrap);
        return wrap;
    }

    function chips(values, className) {
        const ul = el('ul', 'cv-detail__chips' + (className ? ' ' + className : ''));
        values.forEach(value => ul.append(el('li', 'cv-detail__chip', value)));
        return ul;
    }

    /// <summary>Gạch đầu dòng chi tiết được gập sau nút "Xem N ý chính".</summary>
    function moreList(lines) {
        const label = 'Xem ' + lines.length + ' ý chính';
        const more = el('details', 'cv-detail__more');
        const summary = el('summary', null, label);
        const ul = el('ul', 'cv-detail__bullets');
        lines.forEach(line => ul.append(el('li', null, line)));
        more.append(summary, ul);
        more.addEventListener('toggle', () => { summary.textContent = more.open ? 'Ẩn bớt' : label; });
        return more;
    }

    /// <summary>Một mục trong luồng: tên, dòng phụ, mô tả, công nghệ, ý chính.</summary>
    function flowItem(title, meta, description, tech, bullets) {
        const li = el('li');
        li.append(el('strong', 'cv-flow__title', title || 'Chưa có tên'));
        if (meta) li.append(el('span', 'cv-flow__meta', meta));
        if (description) li.append(el('p', 'cv-detail__desc', description));
        if (tech.length) li.append(chips(tech, 'cv-detail__chips--small'));
        if (bullets.length) li.append(moreList(bullets));
        return li;
    }

    function flowGroup(kind, label, items) {
        const group = el('li', 'cv-flow__group cv-flow__group--' + kind);
        const ul = el('ul', 'cv-flow__items');
        items.forEach(item => ul.append(item));
        group.append(el('span', 'cv-flow__label', label + ' · ' + items.length), ul);
        return group;
    }

    /// <summary>Tên đậm + một dòng phụ (học vấn, chứng chỉ ở cột phụ).</summary>
    function sideEntry(parent, title, meta) {
        const p = el('p', 'cv-detail__side-entry');
        p.append(el('strong', null, title));
        if (meta) p.append(el('span', null, meta));
        parent.append(p);
    }

    function buildDetails(data) {
        const box = el('div', 'cv-library__details');
        box.hidden = true;

        const jobs = asList(data.workExperiences);
        const projects = asList(data.projects);
        const schools = asList(data.educations);
        const certs = asList(data.certificationsAndAwards);
        const hard = asList(data.hardSkills);
        const soft = asList(data.softSkills);

        //#region Hàng tóm tắt
        const facts = el('div', 'cv-detail__facts');
        [
            ['Họ tên', data.fullName],
            ['Kinh nghiệm', data.totalYearsOfExperience ? 'Khoảng ' + data.totalYearsOfExperience + ' năm' : ''],
            ['Học vấn', schools.length ? [schools[0].schoolName, schools[0].major].filter(Boolean).join(' · ') : '']
        ].filter(([, value]) => value).forEach(([label, value]) => {
            const fact = el('div', 'cv-detail__fact');
            fact.append(el('span', null, label), el('strong', null, value));
            facts.append(fact);
        });
        if (facts.childElementCount) box.append(facts);
        //#endregion

        const cols = el('div', 'cv-detail__cols');
        const main = el('div', 'cv-detail__main');
        const side = el('aside', 'cv-detail__side');

        //#region Cột chính
        if (data.careerObjective) {
            section(main, 'Mục tiêu nghề nghiệp').append(el('p', 'cv-detail__objective', data.careerObjective));
        }

        if (jobs.length || projects.length) {
            const flow = el('ol', 'cv-flow');
            if (jobs.length) {
                flow.append(flowGroup('job', 'Kinh nghiệm', jobs.map(j => flowItem(
                    j.jobTitle,
                    [j.companyName, j.duration].filter(Boolean).join(' · '),
                    j.mainResponsibilities,
                    [],
                    asList(j.achievements)))));
            }
            if (projects.length) {
                flow.append(flowGroup('project', 'Dự án', projects.map(p => flowItem(
                    p.name,
                    [p.context, p.role, p.duration].filter(Boolean).join(' · '),
                    p.description,
                    asList(p.technologies),
                    asList(p.responsibilities)))));
            }
            section(main, 'Kinh nghiệm & dự án').append(flow);
        }
        //#endregion

        //#region Cột phụ
        if (hard.length) section(side, 'Kỹ năng chuyên môn').append(chips(hard));
        if (soft.length) section(side, 'Kỹ năng mềm').append(chips(soft, 'cv-detail__chips--soft'));
        if (schools.length) {
            const s = section(side, 'Học vấn');
            schools.forEach(e => sideEntry(s, e.schoolName,
                [e.major, e.degree, e.gpa ? 'GPA ' + e.gpa : ''].filter(Boolean).join(' · ')));
        }
        if (certs.length) {
            const s = section(side, 'Chứng chỉ & giải thưởng');
            certs.forEach(c => sideEntry(s, c.title, [c.issuer, c.year].filter(Boolean).join(' · ')));
        }
        //#endregion

        if (main.childElementCount) cols.append(main);
        if (side.childElementCount) cols.append(side);
        if (cols.childElementCount) box.append(cols);
        return box;
    }
    //#endregion

    //#region Card
    function buildCard(item) {
        const data = item.data || {};
        const title = CV.titleOf(item);
        const card = el('article', 'cv-library__item' + (item.isDefault ? ' is-default' : ''));

        const head = el('div', 'cv-library__head');
        const iconBox = el('span', 'cv-library__icon');
        iconBox.append(icon('fa-regular fa-file-lines'));

        const text = el('div', 'cv-library__info');
        const h2 = el('h2', 'cv-library__title', title);
        if (item.isDefault) {
            const badge = el('span', 'cv-library__badge');
            badge.append(icon('fa-solid fa-star'), document.createTextNode(' Mặc định'));
            h2.append(badge);
        }

        const meta = el('div', 'cv-library__meta');
        [data.fullName, asList(data.hardSkills).length + ' kỹ năng',
            'Lưu ngày ' + formatDate(new Date(item.savedAt)), daysLeftText(item)]
            .filter(Boolean)
            .forEach(value => meta.append(el('span', 'cv-library__pill', value)));

        text.append(h2, meta);
        head.append(iconBox, text);

        const details = buildDetails(data);
        const actions = el('div', 'cv-library__actions');

        const viewBtn = button('Xem chi tiết', 'fa-regular fa-eye', 'button--info button--small');
        viewBtn.setAttribute('aria-expanded', 'false');
        viewBtn.addEventListener('click', () => {
            details.hidden = !details.hidden;
            viewBtn.setAttribute('aria-expanded', String(!details.hidden));
            viewBtn.lastChild.textContent = details.hidden ? ' Xem chi tiết' : ' Thu gọn';
        });
        actions.append(viewBtn);

        if (!item.isDefault) {
            const defaultBtn = button('Dùng CV này', 'fa-regular fa-star', 'button--secondary button--small');
            defaultBtn.addEventListener('click', () => {
                CV.setDefault(item.id);
                render();
                toast('Đã chọn CV mặc định');
            });
            actions.append(defaultBtn);
        }

        const deleteBtn = button('Xoá', 'fa-regular fa-trash-can', 'button--small cv-library__delete');
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
    /// <summary>Tô các ô lưu trữ trên banner theo số CV đã lưu.</summary>
    function renderSlots(used) {
        if (!slots) return;
        const cells = [];
        for (let i = 0; i < CV.max; i++) {
            const cell = el('span', 'cv-slot' + (i < used ? ' is-filled' : ''));
            if (i < used) cell.append(icon('fa-regular fa-file-lines'));
            cells.push(cell);
        }
        slots.replaceChildren(...cells);
    }

    function render() {
        const usable = CV.hasOwner && CV.isAvailable();
        storageError.hidden = usable;

        const items = usable ? CV.load() : [];
        list.replaceChildren(...items.map(buildCard));
        empty.hidden = !usable || items.length > 0;
        count.textContent = items.length + '/' + CV.max + ' CV';
        renderSlots(items.length);

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
