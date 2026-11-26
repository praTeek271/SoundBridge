/* ---- Scroll-Driven Feature Scenes, Hero Live Neon Parallax Waves & Marquee ---- */
(() => {
  const $ = (q, r = document) => r.querySelector(q);
  const calm = matchMedia("(prefers-reduced-motion:reduce)");

  /* ---- Scroll-driven feature scenes ---- */
  const tw = (el, to, sfx = "") => {
    const f = parseFloat(el.textContent) || 0, t0 = performance.now();
    (function s(t) {
      const k = Math.min(1, (t - t0) / 800);
      el.textContent = Math.round(f + (to - f) * k) + sfx;
      if (k < 1) requestAnimationFrame(s);
    })(t0);
  };

  const bar = (n, v, anim) => {
    $("#v" + n).style.width = v + "%";
    anim ? tw($("#vt" + n), v, "%") : $("#vt" + n).textContent = v + "%";
  };

  const wv = () => {
    let d = "";
    for (let x = 0; x <= 400; x += 5) d += (x ? "L" : "M") + x + " " + (40 + Math.sin(x / 16) * 18).toFixed(1);
    return d;
  };
  document.querySelectorAll(".wv").forEach(p => p.setAttribute("d", wv()));

  function scene(i) {
    bar(1, i == 1 ? 30 : 80, i == 1);
    bar(2, i == 1 ? 90 : 80, i == 1);
    if (i == 2) tw($("#dv"), 120, " ms");
    else $("#dv").textContent = "0 ms";
  }

  const sts = [...document.querySelectorAll(".st")], pane = $("#pane");
  const io = new IntersectionObserver(es => es.forEach(e => {
    if (e.isIntersecting) {
      const i = +e.target.dataset.i;
      sts.forEach(s => s.classList.toggle("on", s === e.target));
      pane.dataset.a = i;
      scene(i);
    }
  }), { rootMargin: "-45% 0px -45% 0px" });
  sts.forEach(s => io.observe(s));

  /* ---- Hero live neon waves + parallax ---- */
  const shades = ["#5BC8F5", "#2FB3C9", "#8FDDF8", "#3A93C2"], lanesEl = $("#lanes");

  const buildWavePath = (phase = 0, ampBoost = 1, laneOffset = 0) => {
    const pts = [];
    for (let x = 0; x <= 1200; x += 12) {
      const carrier = Math.sin(x / 38 + phase + laneOffset * 0.7);
      const envelope = (15 + 11 * Math.sin(x / 190 - phase * 0.45 + laneOffset)) * ampBoost;
      const harmonic = Math.sin(x / 19 - phase * 1.35) * 3.2 * ampBoost;
      const y = (40 + carrier * envelope + harmonic).toFixed(1);
      pts.push((x ? "L" : "M") + x + " " + y);
    }
    return pts.join(" ");
  };

  const initialD = buildWavePath(0, 1, 0);
  const lanes = shades.map((c, idx) => {
    const d = document.createElement("div");
    d.className = "lane";
    d.style.setProperty("--lc", c);
    d.style.setProperty("--w-delay", `${(idx * 0.16).toFixed(2)}s`);
    d.style.setProperty("--p-dur", `${(3.6 + idx * 0.55).toFixed(2)}s`);
    d.innerHTML =
      `<svg viewBox="0 0 1200 80" preserveAspectRatio="none">` +
      `<path class="base" d="${initialD}"/>` +
      `<path class="pulse" d="${initialD}"/>` +
      `</svg>`;
    lanesEl.appendChild(d);

    const basePath = d.querySelector("path.base");
    const pulsePath = d.querySelector("path.pulse");
    const len = Math.ceil(basePath.getTotalLength()) + 40;
    basePath.style.setProperty("--len", `${len}px`);
    return { el: d, basePath, pulsePath, offset: idx * 1.1 };
  });

  const startNeonWaves = () => {
    requestAnimationFrame(() => lanesEl.classList.add("is-drawn"));
  };

  const preloaderEl = $("#preloader");
  if (!preloaderEl || preloaderEl.classList.contains("loaded")) {
    startNeonWaves();
  } else {
    const preObs = new MutationObserver(() => {
      if (preloaderEl.classList.contains("loaded")) {
        preObs.disconnect();
        setTimeout(startNeonWaves, 550);
      }
    });
    preObs.observe(preloaderEl, { attributes: true, attributeFilter: ["class"] });
    setTimeout(startNeonWaves, 4200);
  }

  /* Continuous wave undulation + pointer energy + scroll parallax */
  const pars = [...document.querySelectorAll("[data-speed]")];
  let phase = 0, targetEnergy = 1, currentEnergy = 1, lastMouseX = 0, lastMouseT = 0;

  const heroEl = $(".hero");
  if (heroEl) {
    heroEl.addEventListener("pointermove", e => {
      const now = performance.now();
      const dt = Math.max(16, now - lastMouseT);
      const speed = Math.abs(e.clientX - lastMouseX) / dt;
      targetEnergy = Math.min(1.55, 1.08 + speed * 0.35);
      lastMouseX = e.clientX;
      lastMouseT = now;
    }, { passive: true });
  }

  function animateHero() {
    if (calm.matches) return;

    const vh = innerHeight;
    const p = Math.min(1, 0.2 + scrollY / (vh * 0.9));

    targetEnergy += (1 - targetEnergy) * 0.03;
    currentEnergy += (targetEnergy - currentEnergy) * 0.08;
    phase += 0.028 * currentEnergy;

    if (scrollY < vh * 1.4) {
      lanes.forEach((lane, i) => {
        lane.el.style.transform = `translate(${(i * p * 110).toFixed(1)}px,${((i - 1.5) * p * 80).toFixed(1)}px)`;
        const d = buildWavePath(phase * (1 + i * 0.12), currentEnergy, lane.offset);
        lane.basePath.setAttribute("d", d);
        lane.pulsePath.setAttribute("d", d);
      });
    }

    pars.forEach(el => {
      const r = el.parentElement.getBoundingClientRect();
      el.style.transform = `translate3d(0,${((r.top + r.height / 2 - vh / 2) * parseFloat(el.dataset.speed)).toFixed(1)}px,0)`;
    });

    requestAnimationFrame(animateHero);
  }

  if (calm.matches) {
    lanes.forEach((l, i) => l.el.style.transform = `translateY(${(i - 1.5) * 45}px)`);
  } else {
    requestAnimationFrame(animateHero);
  }

  /* ---- Features timeline ---- */
  const txt = $(".txt");
  function tl() {
    const r = txt.getBoundingClientRect(), p = Math.max(0, Math.min(r.height, innerHeight * .5 - r.top));
    txt.style.setProperty("--p", p + "px");
    sts.forEach(s => { s.classList.toggle("done", s.querySelector("h3").getBoundingClientRect().top - r.top + 7 <= p); });
  }
  addEventListener("scroll", tl, { passive: true });
  addEventListener("resize", tl);
  tl();

  /* ---- Under the hood rows ---- */
  const L = ["C#", ".NET 8", "WPF", "C++17", "WASAPI", "WiX installer", "MSI package", "Virtual Audio Driver", "Multi-output routing", "Per-output volume", "Drift audio", "Saved setups", "System tray", "JSON settings", "GitHub Releases"];
  const W = [120, 170, 230, 300, 150, 260, 200];
  $("#rows").setAttribute("aria-label", "Built with: " + L.join(", "));
  $("#rows").innerHTML = [0, 1, 2, 3, 4].map(r => {
    let k = r, h = "";
    const lab = L.slice(r * 3, r * 3 + 3), dim = () => `<i class="pl" style="width:${W[k++ % W.length]}px"></i>`;
    lab.forEach(t => { h += dim() + dim() + `<span class="pl lit">${t}</span>`; });
    h += dim();
    return `<div class="rw2${r % 2 ? " r" : ""}" aria-hidden="true" style="--t:${[70, 55, 85, 62, 95][r]}s;animation-delay:-${r * 17}s">${h}${h}</div>`;
  }).join("");
})();