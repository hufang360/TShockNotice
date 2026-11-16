using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using TShockAPI;
using Notice.Utils;

namespace Notice.Boss;

/// <summary>
/// 一场 BOSS 战的战报快照。BOSS 数据（tracker）在会话结束后仍可读取，
/// 保留下来供 <c>/notice boss</c> 重播。
/// </summary>
internal sealed class BossReportSnapshot
{
    public string BossName { get; init; }
    public List<NPCDamageTracker> Trackers { get; init; } = new();
    public List<string> DeathLines { get; init; } = new();
}

internal static class BossReport
{
    /// <summary>最近一场 BOSS 战报，供 <c>/notice boss</c> 重播。</summary>
    public static BossReportSnapshot Latest { get; private set; }

    public static void Reset() => Latest = null;

    public static BossReportSnapshot Create(BossFightSession session)
    {
        return new BossReportSnapshot
        {
            BossName = session.BossName,
            Trackers = Settings.BossDamageReportEnabled ? session.NewTrackers() : new List<NPCDamageTracker>(),
            DeathLines = Settings.BossDeathReportEnabled ? BuildDeathLines(session) : new List<string>()
        };
    }

    public static void Broadcast(BossReportSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        Latest = snapshot;

        var online = TShock.Players
            .Where(p => p is { Active: true, RealPlayer: true })
            .ToList();

        var header = ChatText.ItemIcon(Settings.BossIconItemId) + $"【BOSS战报】{snapshot.BossName}";

        if (online.Count == 0)
        {
            TShock.Log.ConsoleInfo($"[Notice] {ChatText.NoticeIcon}{header}");
            foreach (var line in DamageLines(snapshot, null).Concat(snapshot.DeathLines))
            {
                TShock.Log.ConsoleInfo(line);
            }

            return;
        }

        ChatText.SendTitle(header);

        // 每人自己一份报告，才能高亮自己那一行。
        foreach (var player in online)
        {
            try
            {
                ChatText.SendLinesTo(player, DamageLines(snapshot, player.TPlayer).Concat(snapshot.DeathLines));
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[Notice] 发送 BOSS 战报给 {player.Name} 失败：{ex.Message}");
            }
        }
    }

    private static IEnumerable<string> DamageLines(BossReportSnapshot snapshot, Player forPlayer)
    {
        if (snapshot.Trackers.Count == 0)
        {
            yield return "未记录到有效伤害。";
            yield break;
        }

        foreach (var tracker in snapshot.Trackers)
        {
            string text;
            try
            {
                text = tracker.GetReport(forPlayer).ToString();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[Notice] 生成伤害报告失败：{ex.Message}");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static List<string> BuildDeathLines(BossFightSession session)
    {
        var entries = session.GetDeathEntries();
        var lines = new List<string>();

        if (entries.Count == 0)
        {
            lines.Add("本场无人死亡。");
            return lines;
        }

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

        return lines;
    }
}
