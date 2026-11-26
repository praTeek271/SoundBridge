/* ---- Cinematic Preloader Simulation & 4-Card Forge Merge ---- */
(() => {
  function initPreloader() {
    const preloader = document.getElementById("preloader");
    if (!preloader) return;

    const loadStatus = document.getElementById("loadStatus");
    const metalOrbit = document.getElementById("metalOrbit");
    const metalCore = document.getElementById("metalCore");
    const tiles = [
      document.getElementById("tile0"),
      document.getElementById("tile1"),
      document.getElementById("tile2"),
      document.getElementById("tile3"),
    ].filter(Boolean);

    let progress = 0;
    let finished = false;

    const finishLoad = () => {
      if (finished) return;
      finished = true;
      clearInterval(interval);

      if (loadStatus) {
        loadStatus.textContent = "SoundBridge Ready";
        loadStatus.classList.add("ready");
      }

      if (metalOrbit) metalOrbit.classList.add("paused");
      tiles.forEach((tile) => {
        tile.classList.add("active", "merging");
      });
      if (metalCore) metalCore.classList.add("forged");

      setTimeout(() => {
        preloader.classList.add("loaded");
      }, 720);
    };

    const interval = setInterval(() => {
      progress = Math.min(100, progress + Math.floor(Math.random() * 10) + 7);

      if (loadStatus && progress < 100) {
        loadStatus.textContent = `Loading SoundBridge · ${progress}%`;
      }

      if (tiles.length) {
        const activeCount = Math.min(
          tiles.length,
          Math.ceil((progress / 100) * tiles.length),
        );
        tiles.forEach((tile, idx) => {
          tile.classList.toggle("active", idx < activeCount);
        });
      }

      if (progress >= 100) {
        finishLoad();
      }
    }, 90);

    // Safety fallback so the preloader never hangs
    setTimeout(finishLoad, 3500);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initPreloader, {
      once: true,
    });
  } else {
    initPreloader();
  }
})();
