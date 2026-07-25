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
  },
  SaveGameData: function (jsonPtr) {
    if (typeof window.SaveGameData === "function") {
      window.SaveGameData(UTF8ToString(jsonPtr));
    }
  },
  LoadGameData: function () {
    var json = "";
    if (typeof window.LoadGameData === "function") {
      json = window.LoadGameData() || "";
    }
    return stringToNewUTF8(json);
  },
  GetSelectedWorldSeed: function () {
    if (typeof window.GetSelectedWorldSeed === "function") {
      return window.GetSelectedWorldSeed() | 0;
    }
    return 271828;
  }
});
