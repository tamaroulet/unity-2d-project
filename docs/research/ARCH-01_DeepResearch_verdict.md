# ARCH-01: Deep Research レポート監査結果とアーキテクト裁定

- 日付: 2026-09-03
- 対象: `docs/research/Research-02_DeepResearch_report.md`（軸A〜F + 添付物 a〜e）
- 裁定者: Claude Opus（アーキテクト）
- 結論: **条件付き承認。診断は全面採用、48時間計画は 5 点の修正を加えて承認。**

---

## 1. 診断（軸A〜F）の検証結果

レポートの主張をリポジトリ実体と照合した。**数値・技術的主張はすべて事実である。**

| レポートの主張 | 実測 | 判定 |
|---|---|---|
| 12 アセンブリ分割 | `.asmdef` 12 個 | ✅ 一致 |
| 8,911 行 | `Game/Assets/**/*.cs` 77 ファイル 8,931 行 | ✅ ほぼ一致 |
| 34KB の自然言語ルール | `.agents/rules/*.md` 計 34,969 bytes | ✅ 一致 |
| `#if UNITY_EDITOR` + `AssetDatabase` による実行時自己修復 | `GameFlowController.cs:92`, `CommandButtonView.cs:56`, `StatusView.cs:109` の 3 箇所 | ✅ 実在 |
| `Update()` 内の毎フレーム再バインド | `BossBattleDialogView.cs:91-95`, `StatusView.cs:47-53` | ✅ 実在 |
| PlayMode テストが存在しない | PlayMode テストディレクトリ 0 件 | ✅ 一致 |
| CI にテストゲートがない | `.github/workflows/` は Pages デプロイのみ | ✅ 一致 |

### 監査で新たに判明した事実（レポートに無い、より深刻な欠陥）

`GameFlowController.EnsureDependencies()` が読みに行く 15 本のアセットパスのうち **2 本は実在しない**。

- コード: `Assets/Data/Channels/GameStateEventChannel.asset` → 実体: `GameStateChannel.asset`
- コード: `Assets/Data/Channels/GameEventFiredChannel.asset` → 実体: `EventFiredChannel.asset`

つまり「安全網」と称された自己修復コードは、**エディタ上ですら既に沈黙して null を返している**。
Reward Hacking で書かれたコードが、ハック対象の指標すら満たしていない。これが根拠 A である。

---

## 2. 48時間計画への修正（5点）

### 修正1: 【最重要】Hour 4-8「シーンの人間による再構築」を却下する

レポートは「一度も画面を出さずに 32 時間」という前提で書かれているが、**この前提は現時点で古い**。
2026-09-02 23:30 のスクリーンショットで、`MainGame.unity` は既に動作している:
TURN 1/24 の表示、Stamina/Skill/Mental の 3 ゲージ、STUDY/TRAIN/REST の 3 コマンドボタン。

**動いている唯一の資産をゼロから作り直すのは、破綻の再生産である。**

- ❌ 却下: MainScene をゼロから作り直す
- ✅ 採用: `MainGame.unity` を維持し、`AssetDatabase` ハックが隠蔽していた
  **未アサインの `[SerializeField]` スロットを人間が Inspector で埋める**

これは同じ「人間がシリアライズを担当する」原則を、既存資産を捨てずに満たす。

### 修正2: 「12 → 2 アセンブリ」は Unity 上で成立しない。正解は「12 → 3」

asmdef は `Assembly-CSharp`（既定アセンブリ）を参照**できない**。参照方向は逆である。
よって「全 asmdef を削除して Main と Tests の 2 つに」を実行すると、
テストアセンブリからゲームコードが一切見えなくなり、127 件のテストが全て壊れる。

正しい構成は 3 つ:

| asmdef | 配置 | 役割 |
|---|---|---|
| `Game.asmdef` | ランタイムコードのルート | ゲーム本体（1 つに統合） |
| `Game.Editor.asmdef` | `Game/Assets/Editor/` | `includePlatforms: ["Editor"]`、`Game` を参照 |
| `Game.Tests.asmdef` | `Game/Assets/Tests/` | `Game` + nunit を参照。EditMode/PlayMode 兼用 |

### 修正3: 優先順位を入れ替える。第一の欠陥は asmdef 数ではなくビルドの死

現状 WebGL ビルドを実行すると、`#if UNITY_EDITOR` ブロックがコードごと消え、
15 本の SO 参照が全て null のままゲームが起動する。**Hour 8-12 の CI 構築を待たずに死ぬ。**

したがって Hour 0-4 の第一タスクはアセンブリ統合ではなく、
**シリアライズ参照の正常化（ハック 3 箇所の削除 + 人間による Inspector アサイン）** とする。
アセンブリ統合はその後で行う。順序を誤ると「統合したがビルドは相変わらず死んでいる」状態になる。

### 修正4: 添付物(d) のフック設定例は動作しない。修正済みのものを実装した

レポートの `.claude/settings.json` 例は `$CLAUDE_TOOL_INPUT` 環境変数を参照しているが、
**Claude Code のフックはツール入力を環境変数では渡さない。stdin に JSON を渡す。**
また `grep -q '\.unity$'` はファイルパスではなくペイロード全体に対する検査になっており、
仮に動いても意図した位置でマッチしない。

そのまま採用すると「フックは設置されているが一度もブロックしない」という、
本件で最も避けるべき"見かけ上の安全"が再生産される。
**stdin JSON を読む実装に書き直し、11 ケースの実地テストで検証済み**（§3 参照）。

### 修正5: 「約4,000行を破棄」「テストの50%を削除」という数値目標を却下する

行数削減目標は、それ自体が Reward Hacking 可能な指標である
（動いているコードを消せば達成できてしまう）。
数値目標ではなく**判定基準**に置き換える。基準は指示書 GATE3-01 §3 に定義した。

---

## 3. 本日実装済みの物理ゲート

| 成果物 | 状態 |
|---|---|
| `.claude/hooks/guard.js` | 実装・**11 ケースの入出力テスト合格** |
| `.claude/settings.json` | PreToolUse フック配線済み（既存の `model` 設定は保持） |
| `.agents/rules/00_rules.md` | 34,969 bytes / 5 ファイル → **3,036 bytes / 1 ファイル** |
| `docs/archive/rules_v1_34kb/` | 旧ルールを破棄せず退避 |

guard.js がブロックするもの（exit 2）:

1. `.unity` / `.prefab` / `.asset` / `.meta` / `.asmdef` への直接編集
2. ランタイム `.cs` 内のエディタ専用 API（`UnityEditor` / `AssetDatabase` / 条件付きコンパイル）
3. ランタイム `.cs` 内のシーン内検索（型検索・名前検索・パス検索）
4. `rm -rf /`、`git push --force`、`git reset --hard`

`Editor/` および `Tests/` 配下の `.cs` は 2・3 の対象外（正当な用途があるため）。
Markdown 等の非 `.cs` ファイルも対象外。

> このフックは実装中に自分自身のドキュメントをブロックし、その場で過剰検知を修正した。
> 動作していることの実地証明である。

---

## 4. 残存リスク（人間の判断が必要）

1. **GameCI には Unity ライセンスが必要。** Personal ライセンスでも
   `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` を GitHub Secrets に登録する必要がある。
   これは人間しか実行できない。Hour 8-12 の前提条件。
2. **添付物(b) の YAML はこのリポジトリでは動かない。** Unity プロジェクトが `Game/` 配下にあるため
   `projectPath: Game` の指定と、キャッシュパス `Game/Library` への修正が必須。
   `actions/cache@v3` も v4 へ更新すること。Hour 8-12 の指示書で確定させる。
3. **Hour 12-24 以降を Gemini の自律ループに戻す判断は、PlayMode テストが緑になるまで保留。**
   曳光弾が通らないうちに実装を再開させれば、同じ 32 時間が再現する。
