//#region Feedback
/// <summary>
/// [FEEDBACK] 
/// Trang Góp ý: nhãn và tô sao theo mức đánh giá, đếm số ký tự góp ý. Giao diện nằm trong Feedback.css, màu trong các file theme.
/// </summary>
//#endregion
(() => {
    const labels = {
        1: 'Khó sử dụng',
        2: 'Hơi khó sử dụng',
        3: 'Tạm ổn',
        4: 'Khá dễ sử dụng',
        5: 'Rất dễ sử dụng'
    };

    //#region Đánh giá sao
    const stars = document.querySelectorAll('.feedback-star input[name="easeRating"]');
    const ratingLabel = document.getElementById('rating-label');
    const ratingValue = document.getElementById('rating-value');
    const updateRating = () => {
        const selected = document.querySelector('.feedback-star input[name="easeRating"]:checked');
        if (!selected) return;
        const n = Number(selected.value);
        if (ratingLabel) ratingLabel.textContent = labels[n];
        if (ratingValue) ratingValue.textContent = n + '/5';
        stars.forEach(input => input.closest('.feedback-star').classList.toggle('is-selected', Number(input.value) <= n));
    };
    stars.forEach(input => input.addEventListener('change', updateRating));
    updateRating();
    //#endregion

    //#region Đếm ký tự
    const comment = document.getElementById('feedback-comment');
    const counter = document.getElementById('char-count');
    if (comment && counter) {
        const updateCount = () => { counter.textContent = String(comment.value.length); };
        comment.addEventListener('input', updateCount);
        updateCount();
    }
    //#endregion
})();
