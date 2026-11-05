mergeInto(LibraryManager.library, {
  ExitBrowserFullscreen: function () {
    if (!document.fullscreenElement && !document.webkitFullscreenElement) return;
    var exit = document.exitFullscreen || document.webkitExitFullscreen;
    if (!exit) return;
    try {
      var result = exit.call(document);
      if (result && typeof result.catch === "function") {
        result.catch(function (error) { console.warn("Fullscreen exit failed:", error); });
      }
    } catch (error) {
      console.warn("Fullscreen exit failed:", error);
    }
  }
});
