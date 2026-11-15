using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using TShockAPI;
using Notice.Utils;

namespace Notice.Boss;

internal static class BossReport
{
    private static readonly Color BodyColor = new(220, 220, 220);

    public static void Broadcast(BossFightSession session)
    {
        var online = TShock.Players
            .Where(p => p is { Active: true, RealPlayer: true })
            .ToList();

        if (Settings.BossDamageReportEnabled)
        {
            BroadcastDamage(session, online);
        }

        if (Settings.BossDeathReportEnabled)
        {
            session.CloseAllSegments();
            BroadcastDeaths(session);
        }
    }

    private static void BroadcastDamage(BossFightSession session, List<TSPlayer> online)
    {
        var trackers = session.NewTrackers();
        var header = $"【BOSS战报】{session.BossName} 已击败（用时 {session.DurationText}）";

        if (trackers.Count == 0)
        {
            ChatText.Broadcast(header, new[] { "未记录到有效伤害。" });
            return;
        }

        ChatText.SendTitle(header);

        foreach (var tracker in trackers)
        {
            if (online.Count == 0)
            {
                TShock.Log.ConsoleInfo(tracker.GetReport().ToString());
                continue;
            }

            foreach (var player in online)
            {
                try
                {
                    player.SendMessage(tracker.GetReport(player.TPlayer).ToString(), BodyColor);
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"[Notice] 发送伤害报告给 {player.Name} 失败：{ex.Message}");
                }
            }
        }
    }

    private static void BroadcastDeaths(BossFightSession session)
    {
        var entries = session.GetDeathEntries();
        var lines = new List<string>();

        if (entries.Count == 0)
        {
            lines.Add("本场无人死亡。");
        }
        else
        {
            foreach (var entry in entries)
            {
                var line = $"- {entry.Name} {entry.Deaths} 次";
                var tags = new List<string>();
                if (entry.MidJoin) tags.Add("中途加入");
                if (entry.Rejoined) tags.Add("重连");
                if (entry.Left) tags.Add("已离服");
                if (tags.Count > 0)
                {
                    line += $"（{string.Join("，", tags)}）";
                }

                lines.Add(line);
            }
        }

        ChatText.Broadcast($"【BOSS战报】{session.BossName} 死亡统计：", lines);
    }
}
