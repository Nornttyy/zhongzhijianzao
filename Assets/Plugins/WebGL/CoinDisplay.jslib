mergeInto(LibraryManager.library, {
  SetCoinDisplay: function (coins) {
    if (typeof window.setCoinDisplay === "function") {
      window.setCoinDisplay(coins);
    }
  },
  NotifyBuildingPlaced: function (buildingIdPtr) {
      if (typeof window.notifyBuildingPlaced === "function") {
        window.notifyBuildingPlaced(UTF8ToString(buildingIdPtr));
      }
  },
  NotifyShopItem: function (itemIdPtr, count) {
    if (typeof window.setShopItemCount === "function") {
      window.setShopItemCount(UTF8ToString(itemIdPtr), count);
    }
  },
  NotifyFarmingFeedback: function (messagePtr) {
    if (typeof window.notifyFarmingFeedback === "function") {
      window.notifyFarmingFeedback(UTF8ToString(messagePtr));
    }
  },
  NotifyHarvest: function (itemIdPtr, coins) {
    if (typeof window.notifyHarvest === "function") {
      window.notifyHarvest(UTF8ToString(itemIdPtr), coins);
    }
  }
});
