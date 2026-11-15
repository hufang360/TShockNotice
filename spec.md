# Notice 规格说明

插件名 `Notice`；指令入口 `/notice`，简写 `/n`。

## 1. 目标

把服务器里「值得让所有人知道」的事件播报出来：

1. 旅商出现时，自动向全服播报旅商当前库存。
2. `/notice shop`：播报玩家 20 格内最近的、会卖东西的 NPC 的库存（旅商、骷髅商人、普通城镇 NPC 都走这条指令）。
3. `/notice fish`：播报今天的渔夫任务鱼；默认每天自动播报一次。
4. `/notice 1`~`/notice 10`：播报自己快捷栏对应格的物品（含数量与词缀）。
5. BOSS 被击败时，播报本场伤害贡献（参考 `/bossdamage`）和本场玩家死亡次数（参考 `/alldeath`）。

## 2. 非目标

- 不替代 TShock 原生 `/bossdamage`、`/alldeath`，只做事件触发时的自动播报。
- 不修改商人库存、不刷新旅商背包、不改玩家死亡计数。
- 不记录跨重启数据，不做排行榜、数据库、Web 面板。
- 不处理入侵、事件进度、掉落物等其他播报。

## 3. 术语与判定

| 名称 | 判定 |
| --- | --- |
| 旅商 | `NPCID.TravellingMerchant`（368） |
| 骷髅商人 | `NPCID.SkeletonMerchant`（453） |
| 渔夫 | `NPCID.Angler`（369） |
| 会卖东西的 NPC | 在 `Terraria.GameContent.NPCInteractions.All` 中存在 `OpenShop` 交互的 NPC 类型 |
| BOSS NPC | `npc.boss == true`，或命中 `NPCDamageTracker.CustomBossDefinitions` / `BossTypeForMob` |
| 一场 BOSS 战 | 从场上出现第一个 BOSS NPC 开始，到最后一个 BOSS NPC 消失为止；复合 BOSS 算一场 |
| 20 格 | 玩家中心点与 NPC 中心点的像素距离 ≤ `20 * 16` |

## 4. 功能 A：旅商出现自动播报

### 4.1 触发

- 监听 `NpcSpawn`；NPC 为旅商且 `active` 时触发。
- 世界加载期间（`Main.gameMenu` 为真）忽略。
- 短时间内重复生成做去重，默认 3 秒内只播一次。
- 库存直接读 `Main.travelShop`，不调用 `Chest.SetupTravelShop()`，避免把玩家看到的库存重新随机掉。

### 4.2 输出

```text
[i:507]旅商带来了5件商品：
[i:3118], [i:3118], [i/s30:167], ...
```

- 每条播报的第一行以 `[i:507]`（铃铛图标）开头。
- 只发物品图标：不写物品名，也不写价格，鼠标悬停即可看到名称、数量与售价。
- 数量 > 1 的图标带数量。
- 每行最多 10 个商品，多余的另起一行；行内用 `, ` 分隔。
- 行数过多时按 `MaxLinesPerMessage` 分条发送。
- 没有有效商品时只写日志，不向玩家发空消息。

### 4.3 手动补播

旅商已经在场、但玩家没看到自动播报时，走近旅商执行 `/notice shop` 即可重播。

## 5. 功能 B：`/notice shop`

### 5.1 语法

```text
/notice shop
```

- 只有玩家能执行；控制台执行时提示「只能由玩家执行」。
- 每名玩家有指令冷却（默认 30 秒），防止刷屏。

### 5.2 选择目标

1. 遍历所有 `active` 的 NPC，只保留「会卖东西」的类型。
2. 计算玩家与 NPC 中心点距离，筛选 ≤ `ShopCommandRangeTiles`（默认 20 格）。
3. 取最近的一个。
4. 附近没有可卖东西的 NPC 时，只给执行者发提示，不广播。

### 5.3 商店来源

- 扫描 `NPCInteractions.All` 中的 `OpenShop` 交互，建立「NPC 类型 → 商店索引」缓存。
- 同一 NPC 类型有多个商店时取第一个（默认商店）。
- 识别失败时至少回退：旅商 368 → 19、骷髅商人 453 → 20。
- 不要求 NPC 非无家可归；商店内容按游戏原版逻辑计算。

### 5.4 读取库存

- 临时把 `Main.myPlayer` 设为执行玩家，调用 `Main.instance.shop[shopIndex].SetupShop(shopIndex)`，
  读完后恢复。旅商（索引 19）同样经过 `SetupShop(19)`，与玩家实际打开商店一致。
- 不修改 NPC、不给玩家发物品、不打开商店。

### 5.5 输出

```text
[i:507]骷髅商人卖这9件商品：
[i:3118], [i:3118], ...
```

- 标题带 NPC 名称（`npc.FullName`）；不显示距离。
- 商品行与旅商自动播报一致（只有图标，每行最多 10 个）。

## 6. 功能 C：`/notice fish`

### 6.1 语法

```text
/notice fish
```

- 玩家、控制台都可以执行。
- 手动执行不受「渔夫在场 / 非无家可归」限制，也不影响当天的自动播报标记。

### 6.2 播报内容

- 任务鱼：`Main.anglerQuestItemNetIDs[Main.anglerQuest]`。
- 「抓捕位置」：取 `Lang.AnglerQuestChat()` 末尾括号里的内容。
- `Main.anglerQuestFinished == true` 时，括号内显示「今日任务已完成」。
- 取不到任务文本时不阻塞播报，只显示鱼图标。

### 6.3 自动播报

默认开启，条件：

- `AutoBroadcastAnglerQuest = true`；
- 渔夫在场（任意 `active` 的 `NPCID.Angler`）；
- 渔夫非无家可归（`!npc.homeless`）；
- 至少 1 名玩家在线（避免无人时消费掉当天播报）；
- 当天还没有自动播报过。

触发时机：

1. 世界加载后的第一个有效 tick：条件满足就立即播报（不论白天还是夜晚）。
2. 每天天亮（`Main.dayTime` 由 false 变 true，原版已刷新任务）后，重置当天标记并尝试播报。
3. 条件不满足时每 1 秒重试一次，直到当天播报成功。
4. 当天播报成功后不再重复，直到下一天天亮。

### 6.4 输出

```text
[i:507]今日任务鱼：[i:2472]（抓捕位置：地表苔原）
```

- 只有一行：鱼图标 + 「抓捕位置」。

## 7. 功能 D：BOSS 击败战报

### 7.1 会话开始

- 监听 `NpcSpawn`，遇到 BOSS NPC 时：没有会话就新建（记录 BOSS 名称、开始时间、玩家死亡数基线）；
  已有会话则视为同一场（复合 BOSS、同时出现多个 BOSS）。
- 创建会话时，为每个在线玩家记录 `DeathsPVE` 基线。
- 旧会话还在但场上已无 BOSS NPC 时，丢弃旧会话，避免 BOSS 逃跑 / 自然消失后卡住。

### 7.2 玩家进出

- 战期间加入（`NetGreetPlayer`，此时死亡数已同步）：记录基线，标记「中途加入」。
- 战期间离开：记录离开时的 `DeathsPVE`，结算这一段，标记「已离服」。
- 同一玩家重连：按角色名合并为同一参与者，每段分别记基线再相加，避免重连后死亡数归零出现负数。
- 参与者按角色名合并；角色名为空时退回账号名。

### 7.3 结束判定

- 监听 `NpcKilled`，非 BOSS NPC 直接忽略。
- BOSS 死亡后延迟约 1 秒再检查：场上仍有 BOSS NPC 说明不是最终击杀；已无 BOSS NPC 则结束本场会话。
- 只处理「被击杀」；逃跑 / 时间结束自然消失不播报，只清理会话。
- 同一会话只结算一次，防止多部位 BOSS 重复播报。

### 7.4 伤害报告

- 数据源是 Terraria 内置 `NPCDamageTracker`（与 TShock `/bossdamage` 一致）。
- 会话开始时记录一次 `RecentAttempts()` 引用集合，结算时找出本场新完成的 tracker。
- 对每个在线玩家调用 `tracker.GetReport(player.TPlayer)` 后逐个发送，让各自那一行高亮。
- 找不到新 tracker（例如 BOSS 没受到玩家伤害）时显示「未记录到有效伤害」。
- 报告包含 BOSS 名称、击杀用时、各玩家伤害 / 百分比 / 命中数（以游戏原生文本为准）。

### 7.5 死亡报告

- 每个参与者按「结束快照 − 开始快照」计算，并累加各分段；只统计本场 BOSS 战期间的死亡。
- 按死亡次数从高到低排序，次数相同按名字排序。
- 默认只显示死亡次数 > 0 的玩家；`IncludeZeroDeathPlayers = true` 时显示所有人。
- 无死亡时显示「本场无人死亡」。
- 离服玩家保留并标记 `（已离服）`；中途加入者标记 `（中途加入）`。
- 已知限制：`DeathsPVE` 在玩家复活时才更新，BOSS 被击杀瞬间「刚死还没复活」的死亡可能来不及计入。

### 7.6 输出格式

```text
[i:507]【BOSS战报】月亮领主 已击败（用时 3:24）
输出：
...

[i:507]【BOSS战报】月亮领主 死亡统计：
- 玩家A 3 次
- 玩家B 1 次（中途加入）
- 玩家C 2 次（已离服）
```

伤害与死亡拆成两条消息发送，各自第一行以铃铛图标开头。

## 8. 功能 E：`/notice 1` ~ `/notice 10`

### 8.1 语法

```text
/notice 3
```

- 数字 1~10 对应快捷栏第 1~10 格（背包索引 0~9）。
- 只能由玩家执行。

### 8.2 输出

```text
[i:507]玩家A快捷栏第3格：[i/p59,s2:4956]
```

- 单行播报。
- 物品标签由原版 `ItemTagHandler.GenerateTag` 生成，词缀（`/p`）与数量（`/s`）自动带上，
  悬停图标即可看到带词缀的完整名称。
- 空格子只提示执行者，不广播。

## 9. 指令与权限

| 指令 | 权限 | 说明 |
| --- | --- | --- |
| `/notice shop` | `notice.use` | 播报 20 格内最近的商店 NPC 库存 |
| `/notice fish` | `notice.use` | 播报今天的渔夫任务鱼 |
| `/notice 1`~`/notice 10` | `notice.use` | 播报自己快捷栏第 1~10 格的物品 |

- 主指令 `/notice`，简写 `/n`。
- 自动播报（旅商、渔夫任务、BOSS 战报）不需要权限。

## 10. 参数

没有配置文件；参数集中在 `Notice/Settings.cs`，改完重新编译、覆盖 dll 生效。

```csharp
internal static class Settings
{
    public const bool AutoBroadcastTravelingMerchant = true;
    public const int MerchantBroadcastCooldownSeconds = 3;
    public const int ShopCommandRangeTiles = 20;
    public const int ShopCommandCooldownSeconds = 30;
    public const bool AutoBroadcastAnglerQuest = true;
    public const bool BossReportEnabled = true;
    public const bool BossDamageReportEnabled = true;
    public const bool BossDeathReportEnabled = true;
    public const bool IncludeZeroDeathPlayers = false;
    public const int BossFinishDelayMilliseconds = 1000;
    public const int NoticeIconItemId = 507;   // 铃铛
    public const int ItemsPerLine = 10;
    public const int MaxLinesPerMessage = 10;
}
```

## 11. 验收标准

### 11.1 旅商

- [ ] 自然生成旅商，全服收到一次库存播报。
- [ ] 用 FishShop 召唤旅商，播报的是刷新后的库存。
- [ ] 播报第一行以 `[i:507]` 开头，商品行只有图标、没有价格，一行最多 10 个。
- [ ] 短时间内重复生成，不重复刷屏。
- [ ] 库存为空时，玩家收不到空消息，日志有记录。
- [ ] 世界加载过程中的 NPC 生成不播报。

### 11.2 `/notice shop`

- [ ] 玩家 20 格内只有骷髅商人，执行后全服收到骷髅商人库存。
- [ ] 播报里不出现距离。
- [ ] 玩家 20 格内只有旅商，执行后全服收到旅商库存。
- [ ] 玩家 20 格内同时有多个商店 NPC，只播报最近的那个。
- [ ] 玩家 20 格内是普通城镇 NPC（如商人），执行后播报其商店库存。
- [ ] 距离 > 20 格时，只有执行者收到「附近没有商店 NPC」。
- [ ] 冷却期间重复执行，只提示冷却中。
- [ ] 控制台执行时提示「只能由玩家执行」。

### 11.3 `/notice fish`

- [ ] 手动 `/notice fish`，全服只收到一行：鱼图标 +「抓捕位置」。
- [ ] 任务已完成时，显示鱼图标和「今日任务已完成」。
- [ ] 天亮后渔夫在场且非无家可归，自动播报一次。
- [ ] 天亮时渔夫不在 / 无家可归，之后渔夫满足条件时补播一次。
- [ ] 当天已自动播报后，不再重复；第二天天亮重新播报。
- [ ] 无人在线时不自动播报；有人上线后补播。

### 11.4 `/notice 1`~`/notice 10`

- [ ] 快捷栏第 3 格放带词缀、数量 > 1 的物品，执行后全服看到图标带数量、悬停显示带词缀名称。
- [ ] 空格子只提示执行者，不广播。
- [ ] 控制台执行时提示「只能由玩家执行」。

### 11.5 BOSS 战报

- [ ] 单人击杀普通 BOSS，播报伤害与死亡。
- [ ] 双子 / 月总等多部位 BOSS，只在最终结束时播报一次。
- [ ] BOSS 战期间玩家 A 离开，报告仍包含 A 的死亡数并标记「已离服」。
- [ ] BOSS 战期间玩家 B 加入，只统计加入后的死亡。
- [ ] 玩家重连后死亡数归零，不会出现负数。
- [ ] BOSS 逃跑 / 自然消失，不播报，且下一场能正常开始。
- [ ] 无伤害记录时，伤害部分给出兜底提示。
