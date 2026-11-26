/* ---- Smooth Inertial Page Scroll (Lenis) ---- */
(() => {
  const calm = matchMedia("(prefers-reduced-motion: reduce)");
  if (calm.matches || typeof Lenis === "undefined") return;

  // Disable native CSS smooth scroll so it doesn't fight Lenis on anchor jumps
  document.documentElement.style.scrollBehavior = "auto";

  const lenis = new Lenis({
    duration: 1.15,
    easing: (t) => Math.min(1, 1.001 - Math.pow(2, -10 * t)),
    smoothWheel: true,
    wheelMultiplier: 0.95,
    touchMultiplier: 1.5
  });

  // Lock scroll until preloader finishes fading out
  const preloader = document.getElementById("preloader");
  if (preloader && !preloader.classList.contains("loaded")) {
    lenis.stop();
    const obs = new MutationObserver(() => {
      if (preloader.classList.contains("loaded")) {
        obs.disconnect();
        lenis.start();
      }
    });
    obs.observe(preloader, { attributes: true, attributeFilter: ["class"] });
    setTimeout(() => lenis.start(), 3800);
  }

  // Smooth anchor navigation for header & hero CTA links
  document.addEventListener("click", (e) => {
    const link = e.target.closest('a[href^="#"]');
    if (!link) return;
    const id = link.getAttribute("href");
    if (id.length <= 1) return;
    const target = document.querySelector(id);
    if (!target) return;

    e.preventDefault();
    lenis.scrollTo(target, {
      offset: -72,
      duration: 1.25
    });
  });

  function raf(time) {
    lenis.raf(time);
    requestAnimationFrame(raf);
  }
  requestAnimationFrame(raf);
})();