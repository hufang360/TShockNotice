using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent;
using Terraria.ID;
using TShockAPI;

namespace Notice.Utils;

internal static class NpcShopLookup
{
    private static readonly Dictionary<int, int> Map = new();
    private static bool _loaded;

    public static bool TryGetShopIndex(int npcType, out int shopIndex)
    {
        EnsureLoaded();
        return Map.TryGetValue(npcType, out shopIndex);
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        try
        {
            foreach (var interaction in NPCInteractions.All)
            {
                if (interaction is not NPCInteractions.Actions.OpenShop shop)
                {
                    continue;
                }

                var type = shop.GetType();
                var npcField = type.GetField("_npcType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var shopField = type.GetField("_shopIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (npcField?.GetValue(shop) is not int npcType || shopField?.GetValue(shop) is not int shopIndex)
                {
                    continue;
                }

                if (npcType > 0 && shopIndex > 0 && !Map.ContainsKey(npcType))
                {
                    Map[npcType] = shopIndex;
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Notice] 读取 NPC 商店表失败：{ex.Message}");
        }

        // 反射失败时的最低保障
        Map.TryAdd(NPCID.TravellingMerchant, 19);
        Map.TryAdd(NPCID.SkeletonMerchant, 20);

        TShock.Log.ConsoleInfo($"[Notice] 已识别 {Map.Count} 个商店 NPC。");
    }
}
