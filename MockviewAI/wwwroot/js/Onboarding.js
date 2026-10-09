document.addEventListener("DOMContentLoaded", function () {
    let currentStep = 1;
    const totalSteps = 3;

    const btnBack = document.getElementById("btnBack");
    const btnNext = document.getElementById("btnNext");
    const onboardingForm = document.getElementById("onboardingForm");
    const progressBar = document.getElementById("progressBarFill");

    // Click chọn Option Card
    document.querySelectorAll(".option-card").forEach(card => {
        card.addEventListener("click", function () {
            const groupName = this.getAttribute("data-group");
            document.querySelectorAll(`.option-card[data-group="${groupName}"]`).forEach(c => c.classList.remove("selected"));
            this.classList.add("selected");

            const inputId = groupName === "graduation" ? "graduationStatusInput" : "experienceLevelInput";
            document.getElementById(inputId).value = this.getAttribute("data-value");
        });
    });

    btnNext.addEventListener("click", function () {
        if (!validateStep(currentStep)) return;

        if (currentStep < totalSteps) {
            currentStep++;
            updateStepUI();
        } else {
            submitForm();
        }
    });

    btnBack.addEventListener("click", function () {
        if (currentStep > 1) {
            currentStep--;
            updateStepUI();
        }
    });

    function updateStepUI() {
        // Toggle Steps
        document.querySelectorAll(".wizard-step").forEach(step => step.classList.remove("active"));
        document.getElementById(`step${currentStep}`).classList.add("active");

        // Update Tracker Left Panel
        document.querySelectorAll(".step-item").forEach((item, index) => {
            const stepNum = index + 1;
            item.classList.remove("active", "completed");
            if (stepNum === currentStep) {
                item.classList.add("active");
            } else if (stepNum < currentStep) {
                item.classList.add("completed");
            }
        });

        // Update Progress Bar
        const percentage = (currentStep / totalSteps) * 100;
        progressBar.style.width = `${percentage}%`;

        // Update Buttons Text
        btnBack.disabled = currentStep === 1;
        btnNext.textContent = currentStep === totalSteps ? "Complete Setup" : "Continue";
    }

    function validateStep(step) {
        if (step === 1) {
            const major = document.getElementById("majorInput").value.trim();
            const position = document.getElementById("targetPositionInput").value.trim();

            if (!major) {
                alert("Please enter your field of study or major.");
                return false;
            }
            if (!position) {
                alert("Please enter your target position.");
                return false;
            }
        } else if (step === 2) {
            const gradStatus = document.getElementById("graduationStatusInput").value.trim();
            if (!gradStatus) {
                alert("Please select your graduation status.");
                return false;
            }
        } else if (step === 3) {
            const expLevel = document.getElementById("experienceLevelInput").value.trim();
            if (!expLevel) {
                alert("Please select your experience level.");
                return false;
            }
        }
        return true;
    }

    function submitForm() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const payload = {
            Major: document.getElementById("majorInput").value.trim(),
            TargetPosition: document.getElementById("targetPositionInput").value.trim(),
            GraduationStatus: document.getElementById("graduationStatusInput").value.trim(),
            ExperienceLevel: document.getElementById("experienceLevelInput").value.trim()
        };

        btnNext.disabled = true;
        btnNext.textContent = "Saving...";

        fetch("/Onboarding/Complete", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify(payload)
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                window.location.href = data.redirectUrl;
            } else {
                alert(data.message || "An error occurred while saving profile.");
                btnNext.disabled = false;
                btnNext.textContent = "Complete Setup";
            }
        })
        .catch(err => {
            console.error(err);
            alert("Something went wrong. Please try again.");
            btnNext.disabled = false;
            btnNext.textContent = "Complete Setup";
        });
    }
});