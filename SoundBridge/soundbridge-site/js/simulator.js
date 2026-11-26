/* ---- GitHub Release Fetch, Video Setup & 3-Window Interactive Simulator ---- */
(() => {
  /* ---- Edit these ---- */
  const REPO = "praTeek271/SoundBridge";
  const VIDEO_SRC = ""; // e.g. "assets/demo.mp4"
  const VIDEO_POSTER = ""; // e.g. "assets/poster.jpg"
  /* -------------------- */

  const $ = (q, r = document) => r.querySelector(q);

  (async () => {
    try {
      const r = await fetch(
        `https://api.github.com/repos/${REPO}/releases/latest`,
      );
      if (!r.ok) return;
      const j = await r.json();
      const a =
        (j.assets || []).find((x) => /\.msi$/i.test(x.name)) ||
        (j.assets || [])[0];
      if (!a) return;
      $("#dl").href = $("#dl2").href = a.browser_download_url;
      $("#dlMeta").textContent =
        `${j.tag_name} · ${(a.size / 1048576).toFixed(1)} MB`;
    } catch (e) {}
  })();

  if (VIDEO_SRC) {
    const v = document.createElement("video");
    v.controls = true;
    v.preload = "metadata";
    v.playsInline = true;
    v.src = VIDEO_SRC;
    if (VIDEO_POSTER) v.poster = VIDEO_POSTER;
    $("#videoEmpty").remove();
    $("#video").appendChild(v);
  }

  /* ---- Interactive app windows ---- */
  const devs = [
    { n: "Headphones (GOBOULT Buds)", s: "Headphones", on: 1, v: 80, d: 0 },
    { n: "Speaker (Realtek(R) Audio)", s: "Speaker", on: 1, v: 80, d: 0 },
    { n: "FxSound Speakers", s: "FxSound", on: 0, v: 66, d: 0 },
  ];
  let live = true;
  const on = () => devs.filter((d) => d.on);
  const spec = (d) => `${d.s} / 48 kHz / 2 ch / 32-bit / ${d.v}% vol`;
  const sb = (a) =>
    `<div class="sb"><div class="it${a == 0 ? " on" : ""}">Devices<b style="margin-left:auto;font-weight:400">+</b></div><div class="it sub"><span class="g">multi-output device 3</span><small class="cnt"></small></div><div class="it${a == 1 ? " on" : ""}">Calibration</div><div class="it">Settings</div><div class="ab">About</div></div>`;
  const win = (h, a, body) =>
    `<div class="app" style="height:${h}px"><div class="tb"><img src="assets/logo.png" alt="">SoundBridge<i>─ □ ×</i></div><div class="bd">${sb(a)}<div class="ct">${body}</div></div></div>`;

  $("#fA").innerHTML = win(
    542,
    0,
    `<div class="t">Devices</div><div class="card mc"><div><small>Master device</small><b>FxSound Speakers (FxSound Audio Enhancer)</b></div><div><small>Sample rate</small><b>48 kHz</b></div></div><div class="cap">Audio Routing from Master Device to Multi Peripherals</div><div class="card dg"><svg id="diag" viewBox="0 0 880 150"></svg></div><div class="card cs"><div class="ch"><span>› Routing Console</span><button class="pill" id="pill"></button></div><div class="cards" id="cons"></div></div>`,
  );
  $("#fB").innerHTML = win(
    607,
    0,
    `<div class="t">multi-output device 3</div><div class="cap" style="margin:0 0 12px">Multi-output device workspace</div><div class="card" style="width:68%"><div class="rw"><span>Master device:</span><span class="sel">FxSound Speakers (FxSound Audio Enhancer) ⌄</span></div><div class="rw"><span>Sample rate:</span><span class="sel">48 kHz</span></div></div><div class="th"><span>Use</span><span>Output device</span><span>Drift audio</span></div><div id="srows"></div><div class="act"><button class="b2" id="scan">Scan</button><button class="b2 pri" id="apply">Apply Configuration…</button></div><div class="toast" id="st"></div>`,
  );
  $("#fC").innerHTML = win(
    606,
    1,
    `<div class="t">Calibration</div><div class="cap" style="margin:0 0 14px">Review the active multi-output device, run a quick test, then press OK to start routing.</div><button class="b2" id="play">Play Test</button><div class="card info" style="margin-top:16px"><div><small>Active multi-output device</small><b>multi-output device 3</b></div><div><small>Sample rate</small><b>48 kHz</b></div><div><small>Master device</small><b>FxSound Speakers</b></div></div><div class="th"><span>Device</span><span style="text-align:right">Volume</span></div><div id="crows"></div><div class="act"><button class="b2 pri" id="ok">OK</button></div><div class="toast" id="ct2"></div>`,
  );

  function rMain() {
    const o = on(),
      ys = o.length == 1 ? [75] : o.length == 2 ? [42, 108] : [25, 75, 125];
    let s =
      `<g class="${live ? "live" : ""}"><path class="ln" d="M268 75H410"/>` +
      ys
        .map((y) => `<path class="ln" d="M470 75C540 75 570 ${y} 640 ${y}"/>`)
        .join("") +
      `</g>`;
    s += `<g transform="translate(240 75)"><rect x="-28" y="-28" width="56" height="56" rx="10" class="nd"/><circle r="9" class="ic"/><circle r="3" class="ic f"/><text y="50">FxSound Speakers</text></g>`;
    s += `<g transform="translate(440 75)"><circle r="30" class="nd"/><circle r="10" class="ic"/><text y="52">SoundBridge</text></g>`;
    s += ys
      .map(
        (y, i) =>
          `<g class="o" transform="translate(660 ${y})"><circle r="20" class="nd"/><path class="ic f" d="M-8-4h4l6-5v18l-6-5h-4z"/><text x="32" y="4" class="rt">${o[i].s}</text></g>`,
      )
      .join("");
    if (!o.length)
      s += `<text x="560" y="78" class="rt">Tick an output in the setup window</text>`;
    $("#diag").innerHTML = s;
    $("#cons").innerHTML = o
      .map(
        (d) =>
          `<div class="dc" style="opacity:${live ? 1 : 0.5}"><b>${d.n}</b><div class="bar"><i style="width:${d.v}%"></i></div><small>${d.v} % vol / ${d.d} ms</small></div>`,
      )
      .join("");
    const p = $("#pill");
    p.className = "pill" + (live ? " on" : "");
    p.innerHTML = `<i class="dot"></i>${live ? "Active" : "In-Active"}`;
    p.setAttribute("aria-pressed", live);
    document
      .querySelectorAll(".cnt")
      .forEach((e) => (e.textContent = `1 ins / ${o.length} outs`));
  }

  function rLists() {
    $("#srows").innerHTML = devs
      .map(
        (d, i) =>
          `<div class="r3"><span class="chk${d.on ? " on" : ""}" role="checkbox" tabindex="0" aria-checked="${!!d.on}" aria-label="Use ${d.s}" data-i="${i}">✓</span><div><b>${d.n}</b><small id="sp${i}">${spec(d)}</small></div><div class="dr"><button aria-label="Less drift for ${d.s}" data-i="${i}" data-k="-10">−</button><span class="num" id="dn${i}">${d.d}</span><button aria-label="More drift for ${d.s}" data-i="${i}" data-k="10">+</button></div></div>`,
      )
      .join("");
    $("#crows").innerHTML = on()
      .map((d) => {
        const i = devs.indexOf(d);
        return `<div class="r4"><span>${d.n}</span><input type="range" min="0" max="100" value="${d.v}" data-i="${i}" aria-label="Volume for ${d.s}"><span class="pc">${d.v}%</span></div>`;
      })
      .join("");
  }

  const toast = (sel, m) => {
    const e = $(sel);
    e.textContent = m;
    e.classList.add("show");
    clearTimeout(e._t);
    e._t = setTimeout(() => e.classList.remove("show"), 2600);
  };
  const tog = (i) => {
    devs[i].on = devs[i].on ? 0 : 1;
    rLists();
    rMain();
  };

  document.addEventListener("click", (e) => {
    const t = e.target,
      c = t.closest(".chk"),
      b = t.closest(".dr button");
    if (c) tog(+c.dataset.i);
    else if (b) {
      const d = devs[+b.dataset.i];
      d.d = Math.max(0, Math.min(500, d.d + +b.dataset.k));
      $("#dn" + b.dataset.i).textContent = d.d;
      rMain();
    } else if (t.closest("#pill")) {
      live = !live;
      rMain();
    } else if (t.closest("#ok")) {
      live = true;
      rMain();
      toast(
        "#ct2",
        `multi-output device 3 routing started -> ${on().length} output(s)`,
      );
    } else if (t.closest("#play"))
      toast("#ct2", `Playing test sound on ${on().length} output(s)`);
    else if (t.closest("#apply"))
      toast("#st", `multi-output device 3 applied: ${on().length} output(s)`);
    else if (t.closest("#scan"))
      toast("#st", "Scan complete: 3 output devices found");
  });

  document.addEventListener("keydown", (e) => {
    if (
      (e.key === " " || e.key === "Enter") &&
      e.target.classList.contains("chk")
    ) {
      e.preventDefault();
      tog(+e.target.dataset.i);
    }
  });

  document.addEventListener("input", (e) => {
    if (e.target.matches(".r4 input")) {
      const i = +e.target.dataset.i;
      devs[i].v = +e.target.value;
      e.target.nextElementSibling.textContent = devs[i].v + "%";
      $("#sp" + i).textContent = spec(devs[i]);
      rMain();
    }
  });

  rLists();
  rMain();

  function fit() {
    document.querySelectorAll(".fit").forEach((f) => {
      const a = f.firstElementChild,
        s = f.clientWidth / 960;
      a.style.transform = `scale(${s})`;
      f.style.height = a.offsetHeight * s + "px";
    });
  }
  new ResizeObserver(fit).observe($(".stage"));
  addEventListener("resize", fit);
  fit();
})();
