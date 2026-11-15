# AGENTS.md

本文件给在本仓库工作的 AI 代理看。保持精简。

## 聊天物品标签格式

语法来自原版 `Terraria.UI.Chat.ChatManager/Regexes.Format`：

```text
[<tag>[/<options>]:<text>]
```

匹配 `(?<!\\)\[(?<tag>[a-zA-Z]{1,10})(\/(?<options>[^:]+))?:(?<text>.+?)(?<!\\)\]`，
`options` 是 `[^:]+`，可含 `/` 与 `,`。

物品标签（`tag` = `i`，`text` = 物品 netID）：

```text
[i:507]                普通物品
[i/s5:73]              数量 5（不写数量时默认 1）
[i/p59:4956]           词缀 59
[i/p59,s2:4956]        词缀 + 数量
```

- 生成用原版 `Terraria.GameContent.UI.Chat.ItemTagHandler.GenerateTag(Item)`，不要手写。
  它先写 `[i`，有词缀补 `/p<词缀>`（后续段用 `,p<词缀>`），数量 ≠ 1 补 `/s<数量>`（后续段用 `,s<数量>`），
  最后拼 `:<物品ID>]`。
- 解析在 `ItemTagHandler.Parse`：`options` 按 `,` 切成若干段，按首字母识别
  `p` 词缀、`s` 数量、`x` 数量（钳到 `maxStack`）；数量不写在标签里就是 1。
- 客户端悬停图标即显示（带词缀的）名称与数量，所以播报里不用再写物品名。

## 构建

- 目标框架 net9.0，依赖 NuGet `TShock` 6.2.1（不要引用本地 `lib/`）。
- `dotnet build Notice/Notice.csproj -c Release`
- 产物放仓库根目录 `Publish/`（已 ignore），命名 `Notice-v<版本>-<MMDD>.dll`。
- 版本号取 `Notice.csproj` 的 `<Version>`。
- 部署给本地运行时服务器：拷 `bin/Release/net9.0/Notice.dll` 到 `/Volumes/970/Games/tr/s/ts/plugins/`，
  再 `docker restart tshock` 生效。

## changelog

- 文件根目录 `changelog.md`；只记相对「上一个已发布版本」的对外净变化。
- 未发布放 `## 未发布`，发版改成 `## YYYYMMDD (vX.Y.Z)`。
- 一条一事，通俗说明「做了什么、影响是什么」。

## 注意

- 除非用户明确要求，否则不要 `git commit` / `git push`。
