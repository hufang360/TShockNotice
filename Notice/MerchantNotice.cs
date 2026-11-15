using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using Notice.Utils;

namespace Notice;

internal sealed class ShopTarget
{
    public NPC Npc { get; init; }
    public int ShopIndex { get; init; }
    public float DistanceTiles { get; init; }

    public string Name => string.IsNullOrWhiteSpace(Npc.FullName) ? Npc.TypeName : Npc.FullName;
}

internal static class MerchantNotice
{
    public static void BroadcastTravelMerchant()
    {
        var items = ReadTravelShopItems();
        if (items.Count == 0)
        {
            TShock.Log.ConsoleInfo("[Notice] 旅商出现，但 Main.travelShop 为空，跳过播报。");
            return;
        }

        ChatText.Broadcast(
            $"旅商带来了{items.Count}件商品：",
            ChatText.ShopLines(items));
    }

    public static ShopTarget FindNearestShopNpc(TSPlayer player, int rangeTiles)
    {
        if (player?.TPlayer == null)
        {
            return null;
        }

        var range = Math.Max(1, rangeTiles);
        var maxDistance = range * 16f;
        var playerCenter = player.TPlayer.Center;
        ShopTarget best = null;
        var bestDistance = float.MaxValue;

        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc == null || !npc.active || !NpcShopLookup.TryGetShopIndex(npc.type, out var shopIndex))
            {
                continue;
            }

            var distance = Vector2.Distance(npc.Center, playerCenter);
            if (distance > maxDistance || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = new ShopTarget
            {
                Npc = npc,
                ShopIndex = shopIndex,
                DistanceTiles = distance / 16f
            };
        }

        return best;
    }

    public static List<Item> ReadShopItems(int shopIndex, int playerIndex)
    {
        var result = new List<Item>();
        try
        {
            if (Main.instance == null || shopIndex < 0 || shopIndex >= Main.instance.shop.Length)
            {
                return result;
            }

            var shop = Main.instance.shop[shopIndex];
            if (shop == null)
            {
                return result;
            }

            var oldMyPlayer = Main.myPlayer;
            try
            {
                Main.myPlayer = playerIndex;
                shop.SetupShop(shopIndex);
            }
            finally
            {
                Main.myPlayer = oldMyPlayer;
            }

            if (shop.item == null)
            {
                return result;
            }

            foreach (var item in shop.item)
            {
                if (item != null && !item.IsAir && item.stack > 0)
                {
                    result.Add(item.Clone());
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Notice] 读取商店 {shopIndex} 失败：{ex.Message}");
        }

        return result;
    }

    public static List<Item> ReadTravelShopItems()
    {
        var result = new List<Item>();
        if (Main.travelShop == null)
        {
            return result;
        }

        foreach (var id in Main.travelShop)
        {
            if (id <= 0)
            {
                continue;
            }

            var item = new Item();
            item.SetDefaults(id);
            result.Add(item);
        }

        return result;
    }

    public static void BroadcastShop(ShopTarget target, IReadOnlyList<Item> items)
    {
        ChatText.Broadcast(
            $"{target.Name}卖这{items.Count}件商品：",
            ChatText.ShopLines(items));
    }
}
