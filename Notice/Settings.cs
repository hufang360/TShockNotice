namespace Notice;

/// <summary>
/// 可调参数集中在这里：没有配置文件，改完重新编译、覆盖 dll 生效。
/// </summary>
internal static class Settings
{
    // 旅商出现自动播报
    public const bool AutoBroadcastTravelingMerchant = true;
    public const int MerchantBroadcastCooldownSeconds = 3;

    // /notice shop
    public const int ShopCommandRangeTiles = 20;
    public const int ShopCommandCooldownSeconds = 30;

    // 渔夫任务
    public const bool AutoBroadcastAnglerQuest = true;

    // BOSS 战报
    public const bool BossReportEnabled = true;
    public const bool BossDamageReportEnabled = true;
    public const bool BossDeathReportEnabled = true;
    public const bool IncludeZeroDeathPlayers = false;
    public const int BossFinishDelayMilliseconds = 1000;

    // 输出
    /// <summary>播报图标：仙灵铃铛（ItemID.FairyBell = 425），每条播报开头。</summary>
    public const int NoticeIconItemId = 425;

    /// <summary>旅商播报的第二个图标：旅商草帽（ItemID.PeddlersHat = 2222）。</summary>
    public const int TravelMerchantIconItemId = 2222;

    /// <summary>BOSS 战报的第二个图标：史莱姆王纪念章（ItemID.KingSlimeTrophy = 2489）。</summary>
    public const int BossIconItemId = 2489;

    /// <summary>商店一行最多放几个商品。</summary>
    public const int ItemsPerLine = 10;

    /// <summary>一条消息里最多几行。</summary>
    public const int MaxLinesPerMessage = 10;
}
