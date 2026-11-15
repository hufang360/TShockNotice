using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace Notice;

internal static class Commands
{
    private const string UsePermission = "notice.use";

    private static readonly Dictionary<string, DateTime> ShopCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private static Command _command;

    public static void Register()
    {
        _command = new Command(
            new List<string> { UsePermission },
            OnCommand,
            "notice",
            "n")
        {
            HelpText = "播报：/notice shop、/notice fish、/notice 1~10（快捷栏物品）",
            AllowServer = true
        };

        TShockAPI.Commands.ChatCommands.Add(_command);
    }

    public static void Unregister()
    {
        if (_command != null)
        {
            TShockAPI.Commands.ChatCommands.Remove(_command);
            _command = null;
        }
    }

    public static void OnPlayerLeave(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            ShopCooldowns.Remove(name);
        }
    }

    private static void OnCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            HandleShop(args);
            return;
        }

        if (int.TryParse(args.Parameters[0], out var slot) && slot >= 1 && slot <= 10)
        {
            HotbarNotice.Broadcast(args.Player, slot);
            return;
        }

        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "shop":
                HandleShop(args);
                break;
            case "fish":
                FishNotice.Broadcast(args.Player);
                break;
            default:
                SendHelp(args.Player);
                break;
        }
    }

    private static void HandleShop(CommandArgs args)
    {
        var player = args.Player;
        if (player == null || !player.RealPlayer)
        {
            player?.SendErrorMessage("该指令只能由玩家执行。");
            return;
        }

        var range = Math.Max(1, Settings.ShopCommandRangeTiles);
        var target = MerchantNotice.FindNearestShopNpc(player, range);
        if (target == null)
        {
            player.SendWarningMessage($"附近 {range} 格内没有会卖东西的 NPC。");
            return;
        }

        var cooldown = CheckCooldown(player);
        if (cooldown > 0)
        {
            player.SendWarningMessage($"请等待 {cooldown} 秒后再使用 /wn shop。");
            return;
        }

        var items = MerchantNotice.ReadShopItems(target.ShopIndex, player.Index);
        if (items.Count == 0)
        {
            player.SendWarningMessage($"{target.Name} 没有可播报的商品。");
            return;
        }

        MerchantNotice.BroadcastShop(target, items);
        SetCooldown(player);
    }

    private static void SendHelp(TSPlayer player)
    {
        player.SendInfoMessage("用法：");
        player.SendInfoMessage("/notice shop, 播报附近最近 NPC商店 的库存");
        player.SendInfoMessage("/notice fish, 播报今天的渔夫 任务鱼");
        player.SendInfoMessage("/notice 1~10, 播报自己快捷栏第 1~10 格的物品");
    }

    private static int CheckCooldown(TSPlayer player)
    {
        var seconds = Settings.ShopCommandCooldownSeconds;
        if (seconds <= 0)
        {
            return 0;
        }

        var key = GetKey(player);
        if (!ShopCooldowns.TryGetValue(key, out var last))
        {
            return 0;
        }

        var remain = seconds - (DateTime.UtcNow - last).TotalSeconds;
        return remain > 0 ? (int)Math.Ceiling(remain) : 0;
    }

    private static void SetCooldown(TSPlayer player)
    {
        if (Settings.ShopCommandCooldownSeconds > 0)
        {
            ShopCooldowns[GetKey(player)] = DateTime.UtcNow;
        }
    }

    private static string GetKey(TSPlayer player)
    {
        return player.Account?.Name ?? player.Name ?? "";
    }
}
