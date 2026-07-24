mergeInto(LibraryManager.library, {
  SetCoinDisplay: function (coins) {
    if (typeof window.setCoinDisplay === "function") {
      window.setCoinDisplay(coins);
    }
  }
});
