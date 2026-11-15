using Terraria;
using TShockAPI;
using Notice.Utils;

namespace Notice;

/// <summary>`/notice 1`~`/notice 10`：播报玩家快捷栏某格的物品。</summary>
internal static class HotbarNotice
{
    public static void Broadcast(TSPlayer player, int slot)
    {
        if (player == null || !player.RealPlayer)
        {
            player?.SendErrorMessage("该指令只能由玩家执行。");
            return;
        }

        var inventory = player.TPlayer.inventory;
        var index = slot - 1;
        if (index < 0 || index >= 10 || index >= inventory.Length)
        {
            return;
        }

        var item = inventory[index];
        if (item == null || item.IsAir || item.stack <= 0)
        {
            player.SendWarningMessage($"快捷栏第 {slot} 格是空的。");
            return;
        }

        ChatText.SendTitle($"{player.Name}快捷栏第{slot}格：{ChatText.ItemTag(item)}");
    }
}
