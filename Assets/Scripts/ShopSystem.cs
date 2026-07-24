using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// Handles purchases and crop sales made from the web shop. The wallet
    /// remains owned by TopDownPlayer; this component validates prices and
    /// stores both supplies and harvested crops.
    /// </summary>
    public sealed class ShopSystem : MonoBehaviour
    {
        private readonly Dictionary<string, int> itemCounts =
            new Dictionary<string, int>();

        private TopDownPlayer player;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NotifyShopItem(string itemId, int count);

        [DllImport("__Internal", EntryPoint = "NotifyHarvest")]
        private static extern void NotifyHarvestNative(string itemId, int coins);
#endif

        public void Initialize(TopDownPlayer controlledPlayer)
        {
            player = controlledPlayer;
            itemCounts["wheat_seed"] = 0;
            itemCounts["carrot_seed"] = 0;
            itemCounts["fertilizer"] = 0;
            itemCounts["wood"] = 0;
            itemCounts["wheat_crop"] = 0;
            itemCounts["carrot_crop"] = 0;
        }

        // Called by the HTML shop through Unity's SendMessage API.
        public void BuyItem(string itemId)
        {
            int price = GetPrice(itemId);
            if (price <= 0 || player == null || !player.TrySpendCoins(price))
            {
                return;
            }

            int count = GetCount(itemId) + 1;
            itemCounts[itemId] = count;
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyShopItem(itemId, count);
#endif
        }

        public bool TryConsumeItem(string itemId)
        {
            int count = GetCount(itemId);
            if (count <= 0)
            {
                return false;
            }

            itemCounts[itemId] = count - 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyShopItem(itemId, count - 1);
#endif
            return true;
        }

        public void AddHarvest(string seedId)
        {
            string harvestId = seedId == "carrot_seed" ? "carrot_crop" : "wheat_crop";
            int count = GetCount(harvestId) + 1;
            itemCounts[harvestId] = count;
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyShopItem(harvestId, count);
            NotifyHarvestNative(harvestId, count);
#endif
        }

        // Called by the web shop through Unity's SendMessage API.
        public void SellItem(string itemId)
        {
            int price = GetSellPrice(itemId);
            if (price <= 0 || player == null || !TryConsumeItem(itemId))
            {
                return;
            }

            player.AddCoins(price);
            ShowFarmingFeedback(
                itemId == "carrot_crop"
                    ? "胡萝卜已售出，获得 " + price + " 金币"
                    : "小麦已售出，获得 " + price + " 金币");
        }

        public void ShowFarmingFeedback(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyFarmingFeedback(message);
#else
            Debug.Log(message);
#endif
        }

        public void NotifyHarvest(string itemId, int coins)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyHarvestNative(itemId, coins);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NotifyFarmingFeedback(string message);
#endif

        private int GetCount(string itemId)
        {
            return itemCounts.TryGetValue(itemId, out int count) ? count : 0;
        }

        private static int GetPrice(string itemId)
        {
            switch (itemId)
            {
                case "wheat_seed":
                    return 5;
                case "carrot_seed":
                    return 8;
                case "fertilizer":
                    return 12;
                case "wood":
                    return 15;
                default:
                    return 0;
            }
        }

        private static int GetSellPrice(string itemId)
        {
            switch (itemId)
            {
                case "wheat_crop":
                    return 10;
                case "carrot_crop":
                    return 16;
                default:
                    return 0;
            }
        }
    }
}
