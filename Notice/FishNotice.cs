using System;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using TShockAPI;
using Notice.Utils;

namespace Notice;

internal static class FishNotice
{
    private static DateTime _lastCheck = DateTime.MinValue;
    private static int _worldId = -1;
    private static bool _wasDay;
    private static bool _autoDoneToday;

    public static void Reset()
    {
        _lastCheck = DateTime.MinValue;
        _worldId = -1;
        _wasDay = false;
        _autoDoneToday = false;
    }

    public static void Tick()
    {
        if (Main.gameMenu)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastCheck).TotalSeconds < 1)
        {
            return;
        }

        _lastCheck = now;

        int worldId;
        try
        {
            worldId = Main.worldID;
        }
        catch
        {
            return;
        }

        if (worldId != _worldId)
        {
            _worldId = worldId;
            _wasDay = false;
            _autoDoneToday = false;
        }

        var day = Main.dayTime;
        if (day && !_wasDay)
        {
            _autoDoneToday = false;
        }

        _wasDay = day;

        if (!Settings.AutoBroadcastAnglerQuest || _autoDoneToday)
        {
            return;
        }

        if (TShock.Utils.GetActivePlayerCount() <= 0)
        {
            return;
        }

        if (!AnglerReady())
        {
            return;
        }

        Broadcast(null);
        _autoDoneToday = true;
    }

    public static void Broadcast(TSPlayer requester)
    {
        var itemId = GetQuestItemId();
        if (itemId <= 0)
        {
            requester?.SendErrorMessage("没有读到今天的任务鱼。");
            TShock.Log.ConsoleError("[Notice] Main.anglerQuest 越界，无法播报任务鱼。");
            return;
        }

        var item = new Item();
        item.SetDefaults(itemId);

        var title = $"今日任务鱼：{ChatText.ItemTag(item)}";
        if (Main.anglerQuestFinished)
        {
            title += "（今日任务已完成）";
        }
        else
        {
            var location = QuestLocation();
            if (!string.IsNullOrEmpty(location))
            {
                title += $"（{location}）";
            }
        }

        ChatText.SendTitle(title);
    }

    /// <summary>从渔夫对话原文末尾的括号里取「抓捕位置」那一段。</summary>
    private static string QuestLocation()
    {
        try
        {
            var questText = Lang.AnglerQuestChat();
            if (string.IsNullOrWhiteSpace(questText))
            {
                return null;
            }

            var match = Regex.Match(questText.TrimEnd(), @"[（(]([^（()）\r\n]+)[）)]\s*$");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Notice] 读取任务鱼文本失败：{ex.Message}");
            return null;
        }
    }

    private static int GetQuestItemId()
    {
        if (Main.anglerQuest < 0 || Main.anglerQuest >= Main.anglerQuestItemNetIDs.Length)
        {
            return 0;
        }

        return Main.anglerQuestItemNetIDs[Main.anglerQuest];
    }

    private static bool AnglerReady()
    {
        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc != null && npc.active && npc.type == NPCID.Angler && !npc.homeless)
            {
                return true;
            }
        }

        return false;
    }
}
