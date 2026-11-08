document.querySelector('.menu-toggle')?.addEventListener('click', function () {
    const open = this.getAttribute('aria-expanded') !== 'true';
    this.setAttribute('aria-expanded', String(open));
    document.getElementById('main-nav').classList.toggle('is-open', open);
});
document.querySelectorAll('form[data-confirm]').forEach(form => {
    form.addEventListener('submit', event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});
document.querySelectorAll('input[type="file"]').forEach(input => {
    input.addEventListener('change', () => {
        const file = input.files[0];
        input.setCustomValidity(file && file.size > 5 * 1024 * 1024 ? 'Choose an image no larger than 5 MB.' : '');
        input.reportValidity();
    });
});
