/* Unity WebGL bootstrap + admin-panel collapse toggle + canvas re-fit.
   All canvas/layout concerns live here so the data managers stay clean. */
export function initUnity() {
  /* =====================================================================
     Unity bootstrap (original template logic, canvas sizing adapted so it
     measures the Unity container instead of the full window).
     ===================================================================== */
  var appLayout   = document.querySelector("#app-layout");
  var container   = document.querySelector("#unity-container");
  var canvas      = document.querySelector("#unity-canvas");
  var loadingBar  = document.querySelector("#unity-loading-bar");
  var progressBarFull = document.querySelector("#unity-progress-bar-full");
  var warningBanner   = document.querySelector("#unity-warning");

  function unityShowBanner(msg, type) {
    function updateBannerVisibility() {
      warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
    }
    var div = document.createElement('div');
    div.innerHTML = msg;
    warningBanner.appendChild(div);
    if (type == 'error') div.style = 'background: red; padding: 10px;';
    else {
      if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
      setTimeout(function() {
        warningBanner.removeChild(div);
        updateBannerVisibility();
      }, 5000);
    }
    updateBannerVisibility();
  }

  var buildUrl = "Build";
  var loaderUrl = buildUrl + "/Template6DOF.loader.js";
  var config = {
    dataUrl: buildUrl + "/Template6DOF.data",
    frameworkUrl: buildUrl + "/Template6DOF.framework.js",
    codeUrl: buildUrl + "/Template6DOF.wasm",
    streamingAssetsUrl: "StreamingAssets",
    companyName: "Your VR Experience",
    productName: "MuseumTechDemo",
    productVersion: "0.1.0",
    showBanner: unityShowBanner,
  };

  // Portrait design reference 1488 x 2266 (9:16). Fit the largest box of
  // that aspect ratio inside the Unity CONTAINER (not the window), so the
  // canvas stays sharp and undistorted while the panel takes its space.
  var DESIGN_ASPECT = 1488 / 2266;

  function resizeCanvas() {
    var availW = container.clientWidth;
    var availH = container.clientHeight;
    if (availW <= 0 || availH <= 0) return;

    var h = availH;
    var w = h * DESIGN_ASPECT;
    if (w > availW) { w = availW; h = w / DESIGN_ASPECT; }

    canvas.style.width  = Math.round(w) + "px";
    canvas.style.height = Math.round(h) + "px";
  }

  if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
    var meta = document.createElement('meta');
    meta.name = 'viewport';
    meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
    document.getElementsByTagName('head')[0].appendChild(meta);
    container.className = "unity-mobile";
    canvas.className = "unity-mobile";
  } else {
    container.className = "unity-desktop";
    canvas.className = "unity-desktop";
  }

  resizeCanvas();
  window.addEventListener('resize', resizeCanvas);
  window.addEventListener('orientationchange', resizeCanvas);

  loadingBar.style.display = "block";

  var script = document.createElement("script");
  script.src = loaderUrl;
  script.onload = () => {
    window.createUnityInstance(canvas, config, (progress) => {
      progressBarFull.style.width = 100 * progress + "%";
    }).then((unityInstance) => {
      loadingBar.style.display = "none";
      window.unityInstance = unityInstance;   // handy for SendMessage()
      resizeCanvas();
    }).catch((message) => {
      alert(message);
    });
  };
  document.body.appendChild(script);

  /* ---- Admin panel collapse toggle + canvas re-fit ---- */
  var toggle = document.querySelector("#panel-toggle");
  toggle.addEventListener("click", function () {
    var collapsed = appLayout.classList.toggle("panel-collapsed");
    toggle.setAttribute("aria-expanded", String(!collapsed));
    setTimeout(resizeCanvas, 300);                 // re-fit after the transition
  });
  container.addEventListener("transitionend", function (e) {
    if (e.propertyName === "right") resizeCanvas();
  });
  if (window.ResizeObserver) {
    new ResizeObserver(function () { resizeCanvas(); }).observe(container);
  }
}
