using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using TShockAPI;

namespace Notice.Boss;

internal sealed class DeathEntry
{
    public string Name { get; init; }
    public int Deaths { get; init; }
    public bool MidJoin { get; init; }
    public bool Left { get; init; }
    public bool Rejoined { get; init; }
}

internal sealed class BossFightSession
{
    private sealed class Participant
    {
        public string DisplayName = "";
        public TSPlayer Player;
        public int Baseline;
        public int CompletedDeaths;
        public bool SegmentActive;
        public bool MidJoin;
        public bool Left;
        public bool Rejoined;
    }

    private readonly Dictionary<string, Participant> _participants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _indexKeys = new();
    private readonly HashSet<NPCDamageTracker> _seenTrackers;

    public string BossName { get; private set; }
    public DateTime StartedUtc { get; }

    public string DurationText
    {
        get
        {
            var duration = DateTime.UtcNow - StartedUtc;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            return $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
        }
    }

    public BossFightSession(NPC npc)
    {
        BossName = GetNpcName(npc);
        StartedUtc = DateTime.UtcNow;
        _seenTrackers = new HashSet<NPCDamageTracker>(NPCDamageTracker.RecentAttempts());

        foreach (var player in TShock.Players)
        {
            if (player is { Active: true, RealPlayer: true })
            {
                AddParticipant(player, false);
            }
        }
    }

    public void NoteBoss(NPC npc)
    {
        if (string.IsNullOrWhiteSpace(BossName))
        {
            BossName = GetNpcName(npc);
        }
    }

    public void AddParticipant(TSPlayer player, bool midJoin)
    {
        if (player == null || !player.RealPlayer)
        {
            return;
        }

        var key = GetKey(player);
        _indexKeys[player.Index] = key;

        if (_participants.TryGetValue(key, out var existing))
        {
            if (!existing.SegmentActive)
            {
                existing.Player = player;
                existing.DisplayName = string.IsNullOrWhiteSpace(player.Name) ? existing.DisplayName : player.Name;
                existing.Baseline = SafeDeaths(player);
                existing.SegmentActive = true;
                existing.Left = false;
                existing.Rejoined = true;
            }

            return;
        }

        _participants[key] = new Participant
        {
            DisplayName = string.IsNullOrWhiteSpace(player.Name) ? player.Account?.Name ?? "未知玩家" : player.Name,
            Player = player,
            Baseline = SafeDeaths(player),
            SegmentActive = true,
            MidJoin = midJoin
        };
    }

    public void OnLeave(int playerIndex)
    {
        if (!_indexKeys.Remove(playerIndex, out var key))
        {
            return;
        }

        if (_participants.TryGetValue(key, out var participant))
        {
            CloseSegment(participant);
            participant.Left = true;
        }
    }

    public void CloseAllSegments()
    {
        foreach (var participant in _participants.Values)
        {
            if (!participant.SegmentActive)
            {
                continue;
            }

            CloseSegment(participant);
            if (participant.Player == null || !participant.Player.Active)
            {
                participant.Left = true;
            }
        }
    }

    public List<DeathEntry> GetDeathEntries()
    {
        var result = new List<DeathEntry>();
        foreach (var participant in _participants.Values)
        {
            var deaths = participant.CompletedDeaths;
            if (participant.SegmentActive)
            {
                deaths += Math.Max(0, SafeDeaths(participant.Player) - participant.Baseline);
            }

            if (deaths <= 0 && !Settings.IncludeZeroDeathPlayers)
            {
                continue;
            }

            result.Add(new DeathEntry
            {
                Name = participant.DisplayName,
                Deaths = deaths,
                MidJoin = participant.MidJoin,
                Left = participant.Left,
                Rejoined = participant.Rejoined
            });
        }

        return result
            .OrderByDescending(x => x.Deaths)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<NPCDamageTracker> NewTrackers()
    {
        return NPCDamageTracker.RecentAttempts()
            .Where(tracker => !_seenTrackers.Contains(tracker))
            .ToList();
    }

    public static bool IsBossType(NPC npc)
    {
        if (npc == null)
        {
            return false;
        }

        if (npc.boss)
        {
            return true;
        }

        var definitions = NPCDamageTracker.CustomBossDefinitions;
        return definitions != null
               && npc.type >= 0
               && npc.type < definitions.Length
               && definitions[npc.type] != null;
    }

    public static bool AnyBossActive()
    {
        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc != null && npc.active && IsBossType(npc))
            {
                return true;
            }
        }

        return false;
    }

    private void CloseSegment(Participant participant)
    {
        if (!participant.SegmentActive)
        {
            return;
        }

        var current = SafeDeaths(participant.Player);
        participant.CompletedDeaths += Math.Max(0, current - participant.Baseline);
        participant.SegmentActive = false;
    }

    private static int SafeDeaths(TSPlayer player)
    {
        try
        {
            return player?.DeathsPVE ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string GetKey(TSPlayer player)
    {
        var name = player.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = player.Account?.Name ?? $"player-{player.Index}";
        }

        return name.Trim();
    }

    private static string GetNpcName(NPC npc)
    {
        if (npc == null)
        {
            return "未知BOSS";
        }

        return string.IsNullOrWhiteSpace(npc.FullName) ? npc.TypeName : npc.FullName;
    }
}
