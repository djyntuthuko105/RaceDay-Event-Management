document.querySelector('.menu-toggle')?.addEventListener('click', function () {
    const open = this.getAttribute('aria-expanded') !== 'true';
    this.setAttribute('aria-expanded', String(open));
    document.getElementById('main-nav').classList.toggle('is-open', open);
});
