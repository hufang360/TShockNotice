using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TShockAPI;

namespace Notice.Utils;

internal static class NpcMood
{
    /// <summary>
    /// 商人的「心情」。城镇 NPC 用原版幸福度（即商店价格修正，和商店界面右下角显示的一致）；
    /// 旅商 / 骷髅商人原版不参与幸福度系统（幸福度文本为空），改用它们各自的价格倍率。
    /// 取不到时返回 null。
    /// </summary>
    public static string Describe(NPC npc, Player player)
    {
        if (npc == null || player == null)
        {
            return null;
        }

        try
        {
            var settings = Main.ShopHelper.GetShoppingSettings(player, npc);
            if (!string.IsNullOrEmpty(settings.HappinessReport))
            {
                return $"幸福度 {Percent(settings.PriceAdjustment)}";
            }

            if (npc.type == NPCID.TravellingMerchant)
            {
                return $"价格 {Percent(TravelPriceFactor(npc))}";
            }

            if (npc.type == NPCID.SkeletonMerchant)
            {
                return $"价格 {Percent(SkeletonPriceFactor())}";
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Notice] 读取商人幸福度失败：{ex.Message}");
        }

        return null;
    }

    private static string Percent(float factor) => $"{Math.Round(factor * 100)}%";

    /// <summary>原版 <c>ShopHelper.GetTravelingMerchantPrices</c>：离世界出生点越远越贵。</summary>
    private static float TravelPriceFactor(NPC npc)
    {
        var position = npc.Center / 16f;
        var spawn = new Vector2(Main.spawnTileX, Main.spawnTileY);
        var distance = Vector2.Distance(position, spawn) / (Main.maxTilesX / 2f);
        var factor = 1.5f - distance;
        return (2f + factor) / 3f;
    }

    /// <summary>原版 <c>ShopHelper.GetSkeletonMerchantPrices</c>：月相影响，白天再加一点。</summary>
    private static float SkeletonPriceFactor()
    {
        var factor = 1f;
        switch (Main.moonPhase)
        {
            case 1:
            case 7:
                factor = 1.1f;
                break;
            case 2:
            case 6:
                factor = 1.2f;
                break;
            case 3:
            case 5:
                factor = 1.3f;
                break;
            case 4:
                factor = 1.4f;
                break;
        }

        if (Main.dayTime)
        {
            factor += 0.1f;
        }

        return factor;
    }
}
