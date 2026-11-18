using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Notice.Boss;

namespace Notice;

[ApiVersion(2, 1)]
public class NoticePlugin : TerrariaPlugin
{
    private readonly List<(DateTime Due, Action Action)> _delayed = new();

    private BossFightSession _bossSession;
    private bool _bossFinishScheduled;
    private DateTime _lastTravelBroadcast = DateTime.MinValue;
    private DateTime _noBossSince = DateTime.MinValue;

    public override string Name => "Notice";
    public override string Author => "hufang360";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);
    public override string Description => "旅商 / 商店 / 渔夫任务 / BOSS 战报。";

    public NoticePlugin(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
        Commands.Register();

        ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        ServerApi.Hooks.NetGreetPlayer.Register(this, OnNetGreetPlayer);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
        ServerApi.Hooks.GamePostUpdate.Register(this, OnGamePostUpdate);
        ServerApi.Hooks.GameWorldDisconnect.Register(this, OnWorldDisconnect);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnNetGreetPlayer);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            ServerApi.Hooks.GamePostUpdate.Deregister(this, OnGamePostUpdate);
            ServerApi.Hooks.GameWorldDisconnect.Deregister(this, OnWorldDisconnect);

            Commands.Unregister();
            _delayed.Clear();
            _bossSession = null;
            _bossFinishScheduled = false;
        }

        base.Dispose(disposing);
    }

    private void OnNpcSpawn(NpcSpawnEventArgs args)
    {
        if (args.NpcId < 0 || args.NpcId >= Main.npc.Length || Main.gameMenu)
        {
            return;
        }

        var npc = Main.npc[args.NpcId];
        if (npc == null || !npc.active)
        {
            return;
        }

        if (npc.type == NPCID.TravellingMerchant && Settings.AutoBroadcastTravelingMerchant)
        {
            var cooldown = Math.Max(0, Settings.MerchantBroadcastCooldownSeconds);
            var now = DateTime.UtcNow;
            if ((now - _lastTravelBroadcast).TotalSeconds >= cooldown)
            {
                _lastTravelBroadcast = now;
                MerchantNotice.BroadcastTravelMerchant();
            }
        }

        if (!Settings.BossReportEnabled || !BossFightSession.IsBossType(npc))
        {
            return;
        }

        if (_bossSession == null)
        {
            _bossSession = new BossFightSession(npc);
            _noBossSince = DateTime.MinValue;
            TShock.Log.ConsoleInfo($"[Notice] BOSS 战开始：{_bossSession.BossName}");
        }
        else if (_noBossSince != DateTime.MinValue && !_bossFinishScheduled)
        {
            _bossSession = new BossFightSession(npc);
            _noBossSince = DateTime.MinValue;
            TShock.Log.ConsoleInfo($"[Notice] BOSS 战重新开始：{_bossSession.BossName}");
        }
        else
        {
            _bossSession.NoteBoss(npc);
        }
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        var npc = args.npc;
        if (npc == null || !Settings.BossReportEnabled || _bossSession == null)
        {
            return;
        }

        if (!BossFightSession.IsBossType(npc) || _bossFinishScheduled)
        {
            return;
        }

        _bossFinishScheduled = true;
        Schedule(Settings.BossFinishDelayMilliseconds, FinishBossIfDone);
    }

    private void FinishBossIfDone()
    {
        _bossFinishScheduled = false;

        var session = _bossSession;
        if (session == null)
        {
            return;
        }

        if (BossFightSession.AnyBossActive())
        {
            return;
        }

        _bossSession = null;
        _noBossSince = DateTime.MinValue;

        try
        {
            session.CloseAllSegments();
            BossReport.Broadcast(BossReport.Create(session));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Notice] 输出 BOSS 战报失败：{ex.Message}");
        }
    }

    private void OnNetGreetPlayer(GreetPlayerEventArgs args)
    {
        var player = TShock.Players[args.Who];
        if (player is not { Active: true, RealPlayer: true })
        {
            return;
        }

        if (_bossSession != null)
        {
            _bossSession.AddParticipant(player, true);
        }
    }

    private void OnServerLeave(LeaveEventArgs args)
    {
        var player = TShock.Players[args.Who];
        Commands.OnPlayerLeave(player?.Name ?? "");
        _bossSession?.OnLeave(args.Who);
    }

    private void OnGamePostUpdate(EventArgs args)
    {
        ProcessDelayed();

        if (Main.gameMenu)
        {
            return;
        }

        TickBossCleanup();
        FishNotice.Tick();
    }

    private void TickBossCleanup()
    {
        if (_bossSession == null)
        {
            _noBossSince = DateTime.MinValue;
            return;
        }

        if (_bossFinishScheduled)
        {
            return;
        }

        if (BossFightSession.AnyBossActive())
        {
            _noBossSince = DateTime.MinValue;
            return;
        }

        if (_noBossSince == DateTime.MinValue)
        {
            _noBossSince = DateTime.UtcNow;
            return;
        }

        if ((DateTime.UtcNow - _noBossSince).TotalSeconds < 5)
        {
            return;
        }

        _bossSession = null;
        _noBossSince = DateTime.MinValue;
        TShock.Log.ConsoleInfo("[Notice] BOSS 战会话结束（无击杀）。");
    }

    private void OnWorldDisconnect(EventArgs args)
    {
        _delayed.Clear();
        _bossSession = null;
        _bossFinishScheduled = false;
        _noBossSince = DateTime.MinValue;
        _lastTravelBroadcast = DateTime.MinValue;
        FishNotice.Reset();
        BossReport.Reset();
    }

    private void Schedule(int delayMilliseconds, Action action)
    {
        if (action == null)
        {
            return;
        }

        var due = DateTime.UtcNow.AddMilliseconds(Math.Max(0, delayMilliseconds));
        _delayed.Add((due, action));
    }

    private void ProcessDelayed()
    {
        if (_delayed.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        for (var i = _delayed.Count - 1; i >= 0; i--)
        {
            if (_delayed[i].Due > now)
            {
                continue;
            }

            var action = _delayed[i].Action;
            _delayed.RemoveAt(i);
            try
            {
                action();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[Notice] 延迟任务执行失败：{ex.Message}");
            }
        }
    }
}
