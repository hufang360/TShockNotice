using System;
using System.Text.RegularExpressions;
using Terraria;
using TShockAPI;
using Notice.Utils;

namespace Notice;

internal static class FishNotice
{
    private static DateTime _lastCheck = DateTime.MinValue;
    private static int _worldId = -1;

    /// <summary>上一帧是否白天；null 表示刚载入世界，只记基准不做判断。</summary>
    private static bool? _wasDay;

    /// <summary>夜晚→黎明后，等待任务鱼数据刷新再播报的时刻；MinValue 表示本次白天已处理完。</summary>
    private static DateTime _pendingAt = DateTime.MinValue;

    public static void Reset()
    {
        _lastCheck = DateTime.MinValue;
        _worldId = -1;
        _wasDay = null;
        _pendingAt = DateTime.MinValue;
    }

    /// <summary>只在夜晚转黎明（任务鱼更换）时播报一次；无人上线就跳过，之后上线的玩家不补发。</summary>
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
            _wasDay = null;
            _pendingAt = DateTime.MinValue;
        }

        var day = Main.dayTime;
        var wasDay = _wasDay;
        _wasDay = day;

        if (wasDay == null || !Settings.AutoBroadcastAnglerQuest)
        {
            return;
        }

        if (!day)
        {
            _pendingAt = DateTime.MinValue;
            return;
        }

        if (wasDay == false)
        {
            // 任务鱼在开始新一天时轮换，等它刷新完再读
            _pendingAt = now.AddSeconds(1);
            return;
        }

        if (_pendingAt == DateTime.MinValue || now < _pendingAt)
        {
            return;
        }

        _pendingAt = DateTime.MinValue;

        if (TShock.Utils.GetActivePlayerCount() <= 0)
        {
            return;
        }

        var title = BuildTitle(requester: null);
        if (title == null)
        {
            return;
        }

        ChatText.SendTitle(title);
    }

    public static void Broadcast(TSPlayer requester)
    {
        var title = BuildTitle(requester);
        if (title == null)
        {
            return;
        }

        ChatText.SendTitle(title);
    }

    /// <summary>拼标题；取不到任务鱼时已给执行者发错误，返回 null。</summary>
    private static string BuildTitle(TSPlayer requester)
    {
        var itemId = GetQuestItemId();
        if (itemId <= 0)
        {
            requester?.SendErrorMessage("没有读到今天的任务鱼。");
            TShock.Log.ConsoleError("[Notice] Main.anglerQuest 越界，无法播报任务鱼。");
            return null;
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

        return title;
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
}
