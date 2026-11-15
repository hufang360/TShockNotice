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
    private static readonly Color BodyColor = new(220, 220, 220);

    /// <summary>播报图标，用在每条播报的第一行开头。</summary>
    public static string NoticeIcon => Tag(Settings.NoticeIconItemId, 1);

    /// <summary>物品标签：用原版生成器，自动带词缀（/p）与数量（/s），悬停可见完整名称。</summary>
    public static string ItemTag(Item item) => ItemTagHandler.GenerateTag(item);

    /// <summary>按每行最多 <see cref="Settings.ItemsPerLine"/> 个商品拼行。</summary>
    public static IEnumerable<string> ShopLines(IEnumerable<Item> items)
    {
        var tokens = items.Select(ItemTag).ToList();
        for (var i = 0; i < tokens.Count; i += Settings.ItemsPerLine)
        {
            yield return string.Join(", ", tokens.Skip(i).Take(Settings.ItemsPerLine));
        }
    }

    /// <summary>发标题行（自动加播报图标）。</summary>
    public static void SendTitle(string title)
    {
        var text = NoticeIcon + title;
        TSPlayer.All.SendMessage(text, TitleColor);
        TShock.Log.ConsoleInfo($"[Notice] {text}");
    }

    public static void Broadcast(string title, IEnumerable<string> lines)
    {
        var list = lines?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();

        SendTitle(title);

        var max = Math.Max(1, Settings.MaxLinesPerMessage);
        for (var i = 0; i < list.Count; i += max)
        {
            var text = string.Join("\n", list.Skip(i).Take(max));
            TSPlayer.All.SendMessage(text, BodyColor);
            TShock.Log.ConsoleInfo(text);
        }
    }

    private static string Tag(int itemId, int stack)
    {
        return stack > 1 ? $"[i/s{stack}:{itemId}]" : $"[i:{itemId}]";
    }
}
