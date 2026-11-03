document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('onboardingForm');
    const nextButton = document.getElementById('btnNext');
    const backButton = document.getElementById('btnBack');
    const nextLabel = document.getElementById('btnNextLabel');
    const nextIcon = document.getElementById('btnNextIcon');
    const progress = document.getElementById('onboardingProgress');
    const progressFill = document.getElementById('progressBarFill');
    const counter = document.getElementById('stepCounter');
    const description = document.getElementById('stepDescription');
    const errorBox = document.getElementById('onboardingError');
    const overlay = document.getElementById('onboardingLoading');
    const dialog = overlay?.querySelector('.onboarding-loading__dialog');
    const tokenInput = form?.querySelector('input[name="__RequestVerificationToken"]');
    if (!form || !nextButton || !backButton || !tokenInput) return;

    const totalSteps = 3;
    let currentStep = 1;
    let submitting = false;
    const stepDescriptions = [
        'Bắt đầu với ngành học và vị trí mà bạn muốn ứng tuyển.',
        'Chọn tình trạng học tập hiện tại để cá nhân hóa câu hỏi.',
        'Cho MockviewAI biết mức kinh nghiệm của bạn để bắt đầu.'
    ];

    const get = id => document.getElementById(id);
    const value = id => get(id)?.value.trim() || '';

    function setError(message, field) {
        if (errorBox) {
            errorBox.textContent = message;
            errorBox.hidden = !message;
        }
        if (field) field.focus({ preventScroll: false });
    }

    function clearError() { setError(''); }

    function updateStepUI() {
        for (let step = 1; step <= totalSteps; step++) {
            const section = get(`step${step}`);
            const tracker = get(`trackerStep${step}`);
            if (section) {
                section.hidden = step !== currentStep;
                section.classList.toggle('active', step === currentStep);
            }
            if (tracker) {
                tracker.classList.toggle('active', step === currentStep);
                tracker.classList.toggle('completed', step < currentStep);
                if (step === currentStep) tracker.setAttribute('aria-current', 'step');
                else tracker.removeAttribute('aria-current');
                const number = tracker.querySelector('.step-number');
                if (number) number.textContent = step < currentStep ? '✓' : String(step);
            }
        }
        if (progressFill) progressFill.style.width = `${(currentStep / totalSteps) * 100}%`;
        if (progress) progress.setAttribute('aria-valuenow', String(currentStep));
        if (counter) counter.textContent = `Bước ${currentStep} / ${totalSteps}`;
        if (description) description.textContent = stepDescriptions[currentStep - 1];
        backButton.disabled = currentStep === 1 || submitting;
        nextLabel.textContent = currentStep === totalSteps ? 'Hoàn tất thiết lập' : 'Tiếp tục';
        if (nextIcon) nextIcon.className = currentStep === totalSteps ? 'fa-solid fa-check' : 'fa-solid fa-chevron-right';
        clearError();
    }

    // Use actual buttons for keyboard and screen-reader accessibility.
    document.querySelectorAll('.option-card').forEach(card => {
        card.addEventListener('click', () => {
            if (submitting) return;
            const group = card.dataset.group;
            const input = get(group === 'graduation' ? 'graduationStatusInput' : 'experienceLevelInput');
            if (!input) return;
            document.querySelectorAll(`.option-card[data-group="${group}"]`).forEach(item => {
                item.classList.toggle('selected', item === card);
                item.setAttribute('aria-pressed', String(item === card));
            });
            input.value = card.dataset.value || '';
            clearError();
        });
    });

    // Prepopulate the selected card when an OnboardingDto contains existing values.
    [['graduation', 'graduationStatusInput'], ['experience', 'experienceLevelInput']].forEach(([group, id]) => {
        const stored = value(id);
        if (!stored) return;
        document.querySelectorAll(`.option-card[data-group="${group}"]`).forEach(card => {
            const selected = card.dataset.value === stored;
            card.classList.toggle('selected', selected);
            card.setAttribute('aria-pressed', String(selected));
        });
    });

    function validateStep(step) {
        if (step === 1) {
            if (!value('majorInput')) {
                setError('Vui lòng nhập ngành học hoặc lĩnh vực chuyên môn.', get('majorInput'));
                return false;
            }
            if (!value('targetPositionInput')) {
                setError('Vui lòng nhập vị trí bạn muốn ứng tuyển.', get('targetPositionInput'));
                return false;
            }
        } else if (step === 2 && !value('graduationStatusInput')) {
            setError('Hãy chọn tình trạng tốt nghiệp của bạn.');
            return false;
        } else if (step === 3 && !value('experienceLevelInput')) {
            setError('Hãy chọn mức kinh nghiệm của bạn.');
            return false;
        }
        clearError();
        return true;
    }

    function setLoading(active) {
        submitting = active;
        nextButton.disabled = active;
        backButton.disabled = active || currentStep === 1;
        if (overlay) overlay.hidden = !active;
        if (active) {
            document.body.classList.add('is-loading');
            dialog?.focus({ preventScroll: true });
        } else {
            document.body.classList.remove('is-loading');
        }
    }

    async function submitForm() {
        if (submitting) return;
        const payload = {
            Major: value('majorInput'),
            TargetPosition: value('targetPositionInput'),
            GraduationStatus: value('graduationStatusInput'),
            ExperienceLevel: value('experienceLevelInput')
        };
        setLoading(true);
        try {
            const response = await fetch(form.action || '/Onboarding/Complete', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': tokenInput.value
                },
                body: JSON.stringify(payload)
            });
            const contentType = response.headers.get('content-type') || '';
            if (!contentType.includes('application/json')) {
                throw new Error(response.status === 400
                    ? 'Phiên xác thực đã hết hạn. Vui lòng tải lại trang và thử lại.'
                    : 'Máy chủ trả về dữ liệu không hợp lệ. Vui lòng thử lại.');
            }
            const data = await response.json();
            if (response.ok && data.success && typeof data.redirectUrl === 'string' && data.redirectUrl.startsWith('/') && !data.redirectUrl.startsWith('//')) {
                window.location.assign(data.redirectUrl);
                return;
            }
            throw new Error(data?.message || 'Không thể lưu hồ sơ. Vui lòng thử lại.');
        } catch (error) {
            setLoading(false);
            const message = error instanceof Error ? error.message : 'Đã xảy ra lỗi. Vui lòng thử lại.';
            setError(message);
            if (typeof window.Swal !== 'undefined') {
                window.Swal.fire({
                    icon: 'error', title: 'Không thể hoàn tất', text: message,
                    confirmButtonText: 'Đã hiểu', confirmButtonColor: '#5565ed',
                    position: 'center'
                });
            }
        }
    }

    nextButton.addEventListener('click', () => {
        if (submitting || !validateStep(currentStep)) return;
        if (currentStep < totalSteps) {
            currentStep += 1;
            updateStepUI();
        } else {
            submitForm();
        }
    });
    backButton.addEventListener('click', () => {
        if (submitting || currentStep <= 1) return;
        currentStep -= 1;
        updateStepUI();
    });
    // Prevent Enter from triggering an unintended native POST; this is a JSON-only endpoint.
    form.addEventListener('submit', event => {
        event.preventDefault();
        if (!submitting && validateStep(currentStep)) {
            if (currentStep < totalSteps) { currentStep += 1; updateStepUI(); }
            else submitForm();
        }
    });
    window.addEventListener('pageshow', event => {
        if (event.persisted) setLoading(false);
    });
    updateStepUI();
});
