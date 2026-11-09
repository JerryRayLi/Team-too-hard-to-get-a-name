(function () {
  "use strict";

  function isFullscreen() {
    return Boolean(document.fullscreenElement || document.webkitFullscreenElement);
  }

  function requestFullscreen() {
    if (isFullscreen()) return Promise.resolve();
    var root = document.documentElement;
    var request = root.requestFullscreen || root.webkitRequestFullscreen;
    if (!request) return Promise.reject(new Error("Fullscreen is not supported."));

    return new Promise(function (resolve, reject) {
      var timeout;
      function cleanup() {
        clearTimeout(timeout);
        document.removeEventListener("fullscreenchange", changed);
        document.removeEventListener("webkitfullscreenchange", changed);
        document.removeEventListener("fullscreenerror", failed);
        document.removeEventListener("webkitfullscreenerror", failed);
      }
      function changed() {
        if (isFullscreen()) { cleanup(); resolve(); }
      }
      function failed() {
        cleanup();
        reject(new Error("Fullscreen was declined."));
      }
      document.addEventListener("fullscreenchange", changed);
      document.addEventListener("webkitfullscreenchange", changed);
      document.addEventListener("fullscreenerror", failed);
      document.addEventListener("webkitfullscreenerror", failed);
      timeout = setTimeout(failed, 5000);
      try {
        // Must happen directly in the click handler, before loading Unity:
        // downloading the build first would lose browser user activation.
        var result = request.call(root);
        if (result && typeof result.then === "function") result.then(changed, failed);
        changed();
      } catch (error) { failed(); }
    });
  }

  window.UnityFullscreenEntry = {
    start: function (options) {
      var canvas = options.canvas;
      var container = document.getElementById("unity-container");
      var aspect = canvas.width / canvas.height;
      var originalWidth = canvas.style.width;
      var originalHeight = canvas.style.height;
      var started = false;
      var ready = false;
      container.inert = true;
      container.setAttribute("aria-hidden", "true");
      document.body.classList.add("fullscreen-entry-pending");

      var entry = document.createElement("section");
      entry.id = "fullscreen-entry";
      entry.setAttribute("role", "dialog");
      entry.setAttribute("aria-modal", "true");
      entry.setAttribute("aria-labelledby", "entry-title");
      entry.innerHTML = '<div class="entry-card">' +
        '<p class="entry-eyebrow">COLOR BLIND TROLL</p>' +
        '<div class="entry-scene" aria-hidden="true"><i class="entry-platform entry-platform-blue"></i>' +
        '<i class="entry-player"></i><i class="entry-platform entry-platform-red"></i><i class="entry-door"></i></div>' +
        '<h1 id="entry-title">See the whole picture.</h1>' +
        '<p class="entry-description">Play in fullscreen so every platform, switch, and hint stays in view.</p>' +
        '<button id="entry-play" type="button">Enter Fullscreen &amp; Play <span aria-hidden="true">↗</span></button>' +
        '<p class="entry-note">Your main menu is one click away.</p>' +
        '<p id="entry-status" role="status" aria-live="polite"></p>' +
        '<button id="entry-windowed" type="button" hidden>Continue in window</button></div>';
      document.body.appendChild(entry);
      var play = document.getElementById("entry-play");
      var status = document.getElementById("entry-status");
      var windowed = document.getElementById("entry-windowed");

      var returnButton = document.createElement("button");
      returnButton.id = "entry-return-fullscreen";
      returnButton.type = "button";
      returnButton.textContent = "Fullscreen ↗";
      returnButton.hidden = true;
      document.body.appendChild(returnButton);

      function resize() {
        var fullscreen = isFullscreen();
        document.body.classList.toggle("fullscreen-entry-active", fullscreen);
        if (fullscreen) {
          var width = Math.min(window.innerWidth, window.innerHeight * aspect);
          canvas.style.width = Math.floor(width) + "px";
          canvas.style.height = Math.floor(width / aspect) + "px";
        } else {
          canvas.style.width = originalWidth;
          canvas.style.height = originalHeight;
        }
        returnButton.hidden = !ready || fullscreen;
      }

      function failedToLoad() {
        document.body.classList.add("fullscreen-entry-pending");
        container.inert = true;
        container.setAttribute("aria-hidden", "true");
        entry.hidden = false;
        play.disabled = false;
        play.textContent = "Reload Game";
        play.onclick = function () { window.location.reload(); };
        windowed.hidden = true;
        returnButton.hidden = true;
        status.textContent = "The game could not load. Refresh the page to try again.";
        play.focus();
      }

      function begin() {
        if (started) return;
        started = true;
        entry.hidden = true;
        document.body.classList.remove("fullscreen-entry-pending");
        container.inert = false;
        container.removeAttribute("aria-hidden");
        resize();
        try {
          Promise.resolve(options.onStart()).then(function () {
            ready = true;
            resize();
            var footer = document.getElementById("unity-fullscreen-button");
            if (footer) footer.onclick = tryReturn;
            canvas.focus();
          }).catch(failedToLoad);
        } catch (error) { failedToLoad(); }
      }

      function tryReturn() {
        requestFullscreen().then(function () { resize(); canvas.focus(); }).catch(function () {
          returnButton.textContent = "Fullscreen unavailable — retry ↗";
          returnButton.hidden = false;
        });
      }

      play.onclick = function () {
        if (play.disabled || started) return;
        play.disabled = true;
        status.textContent = "Opening fullscreen…";
        requestFullscreen().then(begin).catch(function () {
          play.disabled = false;
          status.textContent = "Fullscreen could not open. Try again, or continue in a window.";
          windowed.hidden = false;
          play.focus();
        });
      };
      windowed.onclick = begin;
      returnButton.onclick = tryReturn;
      document.addEventListener("fullscreenchange", resize);
      document.addEventListener("webkitfullscreenchange", resize);
      window.addEventListener("resize", resize);
      resize();
      play.focus();
    }
  };
}());
