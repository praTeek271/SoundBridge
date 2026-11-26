/* ---- Built-By Panel, Scroll Grow/Shrink & Handwritten "soundbridge" ---- */
(() => {
  const $ = (q, r = document) => r.querySelector(q);

  /* 1. Social links, draggable stickers & credits (original index_2.html behavior) */
  const LINKS = { github: "https://github.com/praTeek271", linkedin: "", email: "" };
  const IC = {
    github: '<path d="M8 7l-5 5 5 5M16 7l5 5-5 5"/>',
    linkedin: '<path d="M4 9v11M4 4.5v.5M9 20v-7a4 4 0 0 1 8 0v7M9 9v11"/>',
    email: '<path d="M3 6h18v12H3zM3 7l9 7 9-7"/>'
  };

  $("#soc").innerHTML = Object.keys(LINKS)
    .filter(k => LINKS[k])
    .map(k => `<a href="${k == "email" ? "mailto:" + LINKS[k] : LINKS[k]}" aria-label="${k}"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${IC[k]}</svg></a>`)
    .join("");

  document.querySelectorAll(".stk").forEach(s => {
    let ox = 0, oy = 0, sx, sy;
    s.addEventListener("pointerdown", e => {
      s.setPointerCapture(e.pointerId);
      sx = e.clientX - ox;
      sy = e.clientY - oy;
      s.style.zIndex = 4;
      const mv = e => {
        ox = e.clientX - sx;
        oy = e.clientY - sy;
        s.style.translate = `${ox}px ${oy}px`;
      };
      const up = () => {
        s.removeEventListener("pointermove", mv);
        s.removeEventListener("pointerup", up);
      };
      s.addEventListener("pointermove", mv);
      s.addEventListener("pointerup", up);
    });
  });

const cb = $("#crb"), cp = $("#crp");
  if (cb && cp) {
    cb.addEventListener("click", () => cb.setAttribute("aria-expanded", cp.toggleAttribute("data-open")));
  }
  /* 2. Handwritten "soundbridge" SVG generator (non-clickable) */
  const SCRIPT = {
    b: [9.4, "M0 -1C2.8 -3 6.2 -12 6 -16.4C5.8 -19.8 1.8 -20 1.8 -16.4L1.8 -1L1.8 -6C2.4 -9 3.4 -10 4.8 -10C6.8 -10 8.2 -8 8.2 -5C8.2 -2 6.6 0 4.8 0C3.4 0 2.4 -0.6 1.8 -1.6C2.4 -0.6 3.4 0 4.8 0C6.6 0 7.8 -0.8 9.4 -1"],
    d: [10.2, "M0 -1C1.4 -1.4 1.4 -9.6 4.3 -10C2.5 -10 1 -7.8 1 -5C1 -2.2 2.5 0 4.3 0C6.1 0 7.6 -2.2 7.6 -5L7.8 -19L7.8 -3C7.8 -0.8 8.8 -1 10.2 -1"],
    e: [9.4, "M0 -1C3.4 -2.4 8 -4.4 8 -7.6C8 -9.4 6.6 -10.4 4.8 -10.4C2.2 -10.4 0.8 -7.8 0.8 -5C0.8 -1.8 2.6 0 5 0C6.8 0 8 -1 9.4 -1"],
    g: [9.6, "M0 -1C1.4 -1.4 1.4 -9.6 4.3 -10C2.5 -10 1 -7.8 1 -5C1 -2.2 2.5 0 4.3 0C6.1 0 7.6 -2.2 7.6 -5L7.8 -10L7.8 4C7.8 8.6 5.8 9.6 4.2 9.6C2 9.6 1.4 7.6 2.8 6C4.6 4 7.4 1.4 9.6 -1"],
    i: [5, "M0 -1C1.2 -1.4 2 -6 2.2 -10L2.2 -3C2.2 -0.8 3.4 -1 5 -1", "M2.4 -15.2L2.5 -15.3"],
    n: [9.6, "M0 -1C1.2 -1.6 1.6 -6 1.8 -9.6L1.8 -1L1.8 -5.6C2.4 -8.6 3.4 -10 4.8 -10C6.4 -10 7.2 -9 7.2 -7L7.2 -3C7.2 -0.8 8.2 -1 9.6 -1"],
    o: [9, "M0 -1C1.4 -1.4 1.4 -9.6 4.3 -10C2.5 -10 1 -7.8 1 -5C1 -2.2 2.5 0 4.3 0C6.1 0 7.6 -2.2 7.6 -5C7.6 -7.8 6.1 -10 4.3 -10C6 -10 7.4 -9.2 9 -9", "", 1],
    r: [8.6, "M0 -1C1.2 -1.6 1.6 -6 1.8 -9.6L1.8 -1L1.8 -5.6C2.6 -8.6 4.2 -10 5.8 -10C6.8 -10 7.6 -9.6 8.2 -8.8", "", 1],
    s: [8.4, "M0 -1C1.6 -2 3 -7 4.4 -10.2C5.6 -8 7.4 -6 7.4 -3.6C7.4 -1.2 5.6 0 3.6 0C2 0 1 -0.6 0.6 -1.6C1 -0.6 2 0 3.6 0C5.6 0 6.8 -0.8 8.4 -1"],
    u: [10, "M0 -1C1.2 -1.6 1.6 -6 1.8 -9.6L1.8 -3.6C1.8 -1 3 0 4.6 0C6.2 0 7.4 -1.4 7.6 -4L7.6 -9.8L7.6 -3C7.6 -0.8 8.6 -1 10 -1"]
  };

  const SX = 1.18, TRACK = 0.95, CUT = -1.2, STROKE = 3.45, SLANT = 0.1584;
  const round = v => Math.round(v * 100) / 100;
  const place = (d, dx) => {
    let k = 0;
    return d.replace(/-?\d*\.?\d+/g, n => (k++ % 2 === 0 ? String(round(+n * SX + dx)) : n));
  };
  const extent = d => (d.match(/-?\d*\.?\d+/g) || []).map(Number).filter((_, i) => i % 2 === 1);

  const svg = $("#sbWordmark");
  const footer = $(".pf");

  if (svg && footer) {
    let x = 0, main = "", extra = [], lift = true, top = 0, bottom = 0;
    for (const ch of "soundbridge") {
      const g = SCRIPT[ch];
      if (!g) continue;
      const d = place(g[1], x);
      main += lift ? d : "L" + d.slice(1);
      if (g[2]) extra.push(place(g[2], x));
      const ys = extent(g[1] + (g[2] || ""));
      top = Math.min(top, ...ys);
      bottom = Math.max(bottom, ...ys);
      lift = !!g[3];
      x += g[0] * SX + TRACK;
    }

    const width = round(x - TRACK);
    const pad = STROKE / 2 + 0.6;
    const vbX = -1.4 - pad;
    const vbY = top - pad;
    const vbW = width + 2.8 + pad * 2 - top * SLANT;
    const vbH = bottom + pad - vbY;
    const cutH = CUT - vbY;

    svg.setAttribute("viewBox", [vbX, vbY, vbW, vbH].map(round).join(" "));
    // svg.parentElement.style.marginBottom = round(-((vbH - cutH) / vbW) * 100) + "%";

    const drawDur = 1.9, writeAt = 0.25;
    svg.innerHTML =
      `<g transform="skewX(-9)">` +
      `<path d="${main}" data-draw style="--dur:${drawDur}s;--at:${writeAt}s"/>` +
      extra.map((d, k) => `<path d="${d}" data-draw style="--dur:0.3s;--at:${round(writeAt + drawDur + k * 0.18)}s"/>`).join("") +
      `</g>`;

    svg.querySelectorAll("path[data-draw]").forEach(p => {
      p.style.setProperty("--l", Math.ceil(p.getTotalLength()) + 1 + "px");
    });

    /* Trigger handwriting once when scrolled into view */
    const writeObserver = new IntersectionObserver(entries => {
      if (entries.some(e => e.isIntersecting)) {
        footer.classList.add("is-written");
        writeObserver.disconnect();
      }
    }, { threshold: 0.35 });
    writeObserver.observe(footer);

    /* 3. Continuous scroll-linked grow (scroll down) & shrink back (scroll up) */
    let rafId = 0;
    const updateFooterScale = () => {
      rafId = 0;
      const rect = footer.getBoundingClientRect();
      const vh = window.innerHeight || document.documentElement.clientHeight;
      const travel = Math.min(rect.height * 0.85, vh * 0.75) || 1;
      const raw = Math.min(1, Math.max(0, (vh - rect.top) / travel));
      const eased = 1 - Math.pow(1 - raw, 2);
      footer.style.setProperty("--foot-p", eased.toFixed(4));
    };

    const onScroll = () => {
      if (!rafId) rafId = requestAnimationFrame(updateFooterScale);
    };

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll);
    updateFooterScale();
  }
})();