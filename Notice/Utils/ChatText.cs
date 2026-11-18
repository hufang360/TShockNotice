using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Chat;
using TShockAPI;

namespace Notice.Utils;

internal static class ChatText
{
    private static readonly Color TitleColor = new(255, 210, 90);

    /// <summary>正文颜色；BOSS 战报等逐个玩家发送的正文也用它。</summary>
    public static readonly Color BodyColor = new(220, 220, 220);

    /// <summary>播报图标，用在每条播报的第一行开头。</summary>
    public static string NoticeIcon => ItemIcon(Settings.NoticeIconItemId);

    public static string ItemIcon(int itemId) => Tag(itemId, 1);

    /// <summary>物品标签：用原版生成器，自动带词缀（/p）与数量（/s），悬停可见完整名称。</summary>
    public static string ItemTag(Item item) => ItemTagHandler.GenerateTag(item);

    /// <summary>按每行最多 <see cref="Settings.ItemsPerLine"/> 个物品拼行，物品之间只隔一个空格。</summary>
    public static IEnumerable<string> ItemLines(IEnumerable<Item> items)
    {
        var tokens = items.Select(ItemTag).ToList();
        return WrapLines(tokens);
    }

    /// <summary>把物品标签切成每行最多 <see cref="Settings.ItemsPerLine"/> 个。</summary>
    public static IEnumerable<string> WrapLines(IReadOnlyList<string> tokens)
    {
        for (var i = 0; i < tokens.Count; i += Settings.ItemsPerLine)
        {
            yield return string.Join(" ", tokens.Skip(i).Take(Settings.ItemsPerLine));
        }
    }

    /// <summary>发标题行（自动加播报图标）。</summary>
    public static void SendTitle(string title)
    {
        var text = NoticeIcon + title;
        TSPlayer.All.SendMessage(text, TitleColor);
        Log(text);
    }

    /// <summary>标题发一条，正文按 <see cref="Settings.MaxLinesPerMessage"/> 行分条发。</summary>
    public static void Broadcast(string title, IEnumerable<string> lines)
    {
        var list = Normalize(lines);

        SendTitle(title);

        foreach (var chunk in Chunks(list))
        {
            TSPlayer.All.SendMessage(chunk, BodyColor);
            Log(chunk);
        }
    }

    /// <summary>正文按 <see cref="Settings.MaxLinesPerMessage"/> 行分条发给一个玩家。</summary>
    public static void SendLinesTo(TSPlayer player, IEnumerable<string> lines)
    {
        if (player is not { Active: true })
        {
            return;
        }

        foreach (var chunk in Chunks(Normalize(lines)))
        {
            player.SendMessage(chunk, BodyColor);
        }
    }

    private static List<string> Normalize(IEnumerable<string> lines)
    {
        return lines?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
    }

    private static IEnumerable<string> Chunks(List<string> lines)
    {
        var max = Math.Max(1, Settings.MaxLinesPerMessage);
        for (var i = 0; i < lines.Count; i += max)
        {
            yield return string.Join("\n", lines.Skip(i).Take(max));
        }
    }

    private static void Log(string text) => TShock.Log.ConsoleInfo($"[Notice] {text}");

    private static string Tag(int itemId, int stack)
    {
        return stack > 1 ? $"[i/s{stack}:{itemId}]" : $"[i:{itemId}]";
    }
}
