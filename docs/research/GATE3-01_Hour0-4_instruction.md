# GATE3-01: Hour 0-4「解体と清掃」確定指示書

- 発行: Claude Opus（アーキテクト） / 2026-09-03
- 実行者: Gemini（§2, §3, §5）+ 人間（§1, §4）
- 上位裁定: `docs/research/ARCH-01_DeepResearch_verdict.md`
- **この指示書に書かれていないファイルは触らないこと。**

---

## 0. Gemini への前提条件（読み飛ばし厳禁）

1. **ルールは `.agents/rules/00_rules.md` のみ**になった。旧 5 ファイルは
   `docs/archive/rules_v1_34kb/` へ退避済み。参照しないこと。
2. `.claude/hooks/guard.js` が稼働中である。以下は**ツールレベルで拒否される**:
   - `.unity` / `.prefab` / `.asset` / `.meta` / `.asmdef` の編集
   - ランタイム `.cs` へのエディタ専用 API・シーン内検索の書き込み
   拒否されたら回避策を探すな。**手を止めて人間に報告せよ。**
3. 作業単位は §2 → §3 → §5 の順。**各節の終わりで必ずコミットし、次に進む前に停止して報告する。**
4. シーンを開く・Inspector を触る作業は人間の担当である。Gemini は着手しない。

---

## 1. 【人間・最優先】シリアライズ参照の確定アサイン

Gemini が §2 でハックを削除すると、**Inspector が空のままなら実行時に必ず落ちる。**
先にこの作業を終えること。

Unity エディタで `Assets/Scenes/MainGame.unity` を開き、以下を Inspector で埋める。

### 1-A. `GameFlowController` の 17 スロット

| フィールド | 割り当てるアセット |
|---|---|
| `_gameRules` | `Assets/Data/Rules/GameRules.asset` |
| `_commandResolver` | `Assets/Data/Commands/CommandResolver.asset` |
| `_eventCatalog` | `Assets/Data/Events/GameEventCatalog.asset` |
| `_eventResolver` | `Assets/Data/Events/EventResolver.asset` |
| `_endingRules` | `Assets/Data/Endings/EndingRules.asset` |
| `_endingResolver` | `Assets/Data/Endings/EndingResolver.asset` |
| `_relicCatalog` | `Assets/Features/Relic/Instances/RelicCatalog.asset` |
| `_relicResolver` | `Assets/Features/Relic/Instances/RelicResolver.asset` |
| `_gameStateChannel` | `Assets/Data/Channels/GameStateChannel.asset` |
| `_eventFiredChannel` | `Assets/Data/Channels/EventFiredChannel.asset` |
| `_endingDecidedChannel` | `Assets/Data/Channels/EndingDecidedChannel.asset` |
| `_relicAcquiredChannel` | `Assets/Features/Relic/Instances/RelicAcquiredChannel.asset` |
| `_bossCatalog` | `Assets/Features/Boss/Instances/BossCatalog.asset` |
| `_autoBattleResolver` | `Assets/Features/Boss/Instances/AutoBattleResolver.asset` |
| `_metaPointResolver` | `Assets/Features/MetaProgression/Instances/MetaPointResolver.asset` |
| `_metaUnlockCatalog` | `Assets/Features/MetaProgression/Instances/MetaUnlockCatalog.asset` |
| `_bossBattleTurns` | `6, 12, 18, 24`（4 要素） |

> ⚠ **コードが読みに行っていたパス 2 本は実在しなかった。**
> `GameStateEventChannel.asset` → 正しくは `GameStateChannel.asset`
> `GameEventFiredChannel.asset` → 正しくは `EventFiredChannel.asset`
> 自己修復コードはこの 2 つを**エディタ上でも復元できていなかった**。手で確実に入れること。

### 1-B. `CommandButtonView` × 3 個

各ボタンの `_command` に対応する SO を割り当てる。
`_button` / `_nameText` / `_gameFlowController` も Inspector で埋める。

| GameObject | `_command` |
|---|---|
| STUDY ボタン | `Assets/Data/Commands/Study.asset` |
| TRAIN ボタン | `Assets/Data/Commands/Train.asset` |
| REST ボタン | `Assets/Data/Commands/Rest.asset` |

### 1-C. `StatusView` / `BossBattleDialogView` / `EventDialogView`

`transform.Find(...)` で拾っていた子オブジェクト（`TurnText`, `StaminaGroup/Label`,
`PanelRoot/*` 等）と `_gameStateChannel`、`GameFlowController` 参照を、
**すべて Inspector から手でドラッグして埋める。**

### 1-D. 完了条件

シーンを保存し、Play を押して `TURN 1 / 24` が表示され Console に例外が 0 であること。
**この状態でコミットしてから Gemini に §2 を許可する。**

---

## 2. 【Gemini】対症療法コードの削除

対象は以下の **5 ファイルのみ**。他は触らない。

| ファイル | 削除する内容 |
|---|---|
| `Features/GameFlow/Scripts/GameFlowController.cs` | `EnsureDependencies()` の `#if UNITY_EDITOR` ブロック全体（L92-116）。`_bossBattleTurns` の再代入も含む。メソッドが空になるなら呼び出し側ごと削除する |
| `UI/Scripts/CommandButtonView.cs` | `EnsureReferences()` の `#if UNITY_EDITOR` ブロック（L56-63）と `FindFirstObjectByType<GameFlowController>()`（L54）、`transform.Find("Text")`（L53）、L118 の `FindFirstObjectByType<StatusView>()` |
| `UI/Scripts/StatusView.cs` | `Update()`（L47-54）を削除。`EnsureReferences()` の `#if UNITY_EDITOR` ブロック（L109-115）と全 `transform.Find(...)`、L58 の `FindFirstObjectByType` |
| `UI/Scripts/BossBattleDialogView.cs` | `Update()`（L91 以降）を削除。L39-43 の `transform.Find(...)` 群、L95 の `FindFirstObjectByType` |
| `UI/Scripts/EventDialogView.cs` | L120 の `FindFirstObjectByType<GameFlowController>()` |

### 置き換えの規則

- 削除した参照は **`[SerializeField] private <型> _fieldName;` の宣言に置き換える**。
  フィールドが既に存在するならフィールドはそのまま、代入コードだけ消す。
- `Update()` で行っていた状態同期は、**既存のイベントチャネル購読（`OnEnable`/`OnDisable`）に寄せる**。
  新しいイベントチャネル SO は作らない。既存のもので届かない場合は実装せず報告する。
- **`Resources.Load` への置き換えは行わない。** `Assets/Resources/` は削除済みであり、
  復活させると人間のアサインと二重管理になる。参照は Inspector 一本に統一する。

### 完了条件

- `Game/Assets` 配下のランタイム `.cs`（`Editor/`・`Tests/` 以外）で
  `#if UNITY_EDITOR` / `AssetDatabase` / `FindFirstObjectByType` / `transform.Find` の
  検索結果が **0 件**。
- Unity のコンパイルエラー 0。
- ここでコミットし、**停止して人間に報告**。人間が Play で例外 0 を確認するまで §3 に進まない。

---

## 3. 【Gemini】テストの整理（行数目標ではなく判定基準で行う）

**「4,000 行削除」「50% 削除」といった数値目標は採用しない。**
数値目標は達成のために動くコードを消す誘因になる。以下の基準だけで判定する。

### 判定基準

| 基準 | 処置 |
|---|---|
| 入力と出力が純粋な計算（`ScriptableObject.CreateInstance` をデータ置き場としてのみ使う） | **残す** |
| `new GameObject()` / `AddComponent<>()` で MonoBehaviour を組み立てて結合を検証している | 下記の通り |
| ↳ 検証対象が UI の見た目・表示更新 | **削除**（PlayMode 曳光弾が代替する） |
| ↳ 検証対象がターン進行・バランスなどのゲームルール | **`[Explicit]` 属性を付けて CI ゲートから外す**（知見を捨てない） |

### 確定した処置一覧（127 件の内訳）

**削除する 2 ファイル（14 件）** — UI モックの罠に該当:
- `Tests/UIViewTests.cs`（12 件 / `AddComponent` 34 箇所）
- `Tests/RelicDraftDialogViewTests.cs`（2 件 / `AddComponent` 5 箇所）

**`[Explicit]` を付けて凍結する 3 ファイル（19 件）** — 削除はしない:
- `Tests/GameFlowControllerTests.cs`（12 件）
- `Tests/GameFlowControllerRelicTests.cs`（4 件）
- `Tests/GameMonteCarloSimulationTests.cs`（3 件）

> 処置: 各クラス宣言の直上に `[Explicit("PlayMode 曳光弾で置換予定")]` を 1 行足すだけ。
> テスト本体は書き換えない。Unity Test Runner は既定で実行せず、CI ゲートから外れる。

**そのまま残す 11 ファイル（94 件）** — 純粋ロジック:
`AutoBattleResolverTests`(24) / `MetaPointResolverTests`(30) / `CommandResolverSOTests`(11) /
`EventResolverSOTests`(9) / `EndingResolverSOTests`(7) / `RelicResolverSOTests`(7) /
`EventChannelSOTests`(5) / `GameRulesSOTests`(1) および 3 つの `*Factory.cs`

### 今後の禁止事項

新規の EditMode テストで `new GameObject()` / `AddComponent<>()` を使ってはならない。
結合の検証が必要なら PlayMode テストを書く。

### 完了条件

CI ゲート対象が 94 件になり、全て緑。ここでコミットして**停止・報告**。

---

## 4. 【人間】アセンブリ統合（12 → 3）

> ⚠ **レポートの「12 → 2」は Unity 上で成立しない。**
> asmdef は既定アセンブリ `Assembly-CSharp` を参照できないため、
> 全 asmdef を削除するとテストアセンブリからゲームコードが見えなくなる。**必ず 3 つにする。**

Unity の Project ウィンドウ上で操作すること（`.meta` の整合を Unity に任せるため）。

### 手順

1. **エディタ専用スクリプトを 1 箇所に集約**
   `Features/Relic/Editor/RelicSceneBinder.cs` を `Assets/Editor/` へ移動。
   これで全エディタスクリプトが `Assets/Editor/` に揃う。

2. **ランタイムコードの受け皿を作る**
   `Assets/Scripts/` を作成し、`Assets/Core/`・`Assets/Features/*/Scripts/`・`Assets/UI/Scripts/`
   の **`.cs` のみ**を移動する。`.asset`（SO 実体）は `Assets/Data/` と
   `Assets/Features/*/Instances/` に置いたまま動かさない。
   > Project ウィンドウでの移動なら GUID が維持され、シーンの参照は切れない。

3. **12 個の `.asmdef` のうち 11 個を削除**（残すのは `Game.Tests.EditMode.asmdef` の名前だけ）

4. **3 つを作り直す**

   | 新 asmdef | 配置 | 設定 |
   |---|---|---|
   | `Game` | `Assets/Scripts/` | 参照: TextMeshPro, Unity.ugui |
   | `Game.Editor` | `Assets/Editor/` | `includePlatforms: ["Editor"]` / 参照: `Game` |
   | `Game.Tests` | `Assets/Tests/` | `includePlatforms: ["Editor"]` / 参照: `Game`, nunit / `"defineConstraints": ["UNITY_INCLUDE_TESTS"]` |

5. コンパイルが通るまで Unity 上で `using` を修正。**この修正は人間が行う**
   （Gemini に投げると §2 で消したハックを復活させる誘因になる）。

### 完了条件

`find Game/Assets -name "*.asmdef" | wc -l` が **3**。コンパイルエラー 0。Play で例外 0。

---

## 5. 【Gemini】Hour 0-4 の締め

上記がすべて終わった時点で、以下を報告せよ。**自然言語の達成宣言ではなく、コマンド出力を貼ること。**

```bash
# 1. ハックが 0 件であること
grep -rn "#if UNITY_EDITOR\|AssetDatabase\|FindFirstObjectByType\|FindObjectOfType\|transform\.Find\|GameObject\.Find" \
  Game/Assets --include=*.cs | grep -v "/Editor/" | grep -v "/Tests/"

# 2. アセンブリが 3 つであること
find Game/Assets -name "*.asmdef"

# 3. 変更量が停止条件（300 行）以内であること
git diff --stat HEAD~1
```

**Hour 4-8 には自分の判断で進まないこと。** 次の指示書 GATE4-01 を待て。

---

## 付記: Hour 4-8 の方針変更（先出し）

レポートの「人間が MainScene をゼロから作り直す」は**却下した**。
2026-09-02 23:30 のスクリーンショットの通り、`MainGame.unity` は既に
TURN 1/24・3 ゲージ・3 コマンドボタンが描画されている。
**動いている唯一の資産を捨てない。** §1 のアサインで同じ目的（人間がシリアライズを持つ）を達成する。

Hour 4-8 は「シーン再構築」ではなく「PlayMode 曳光弾テストの新規作成」に充てる。
