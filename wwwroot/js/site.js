(() => {
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const progress = document.querySelector('.scroll-progress span');
  const nav = document.querySelector('.nav');

  const updateScroll = () => {
    const max = document.documentElement.scrollHeight - window.innerHeight;
    if (progress) progress.style.width = (max > 0 ? (window.scrollY / max) * 100 : 0) + '%';
    if (nav) nav.classList.toggle('nav-scrolled', window.scrollY > 18);
  };

  updateScroll();
  window.addEventListener('scroll', updateScroll, { passive: true });

  const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('in-view');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.1, rootMargin: '0px 0px -4% 0px' });

  document.querySelectorAll('.reveal,.reveal-item').forEach(el => observer.observe(el));

  // Discourage casual downloading/dragging of displayed portfolio media.
  document.addEventListener('contextmenu', e => {
    if (e.target instanceof HTMLImageElement || e.target instanceof HTMLVideoElement) {
      e.preventDefault();
    }
  });

  document.querySelectorAll('img,video').forEach(el => {
    el.setAttribute('draggable', 'false');
    el.addEventListener('dragstart', e => e.preventDefault());
  });

  if (reduceMotion) {
    document.documentElement.classList.add('reduce-motion');
  }
})();