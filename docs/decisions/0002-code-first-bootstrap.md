# UI をシーンにキャッシュせず、実行時にコードから構築する

## 決定日
2026-09-05

## ステータス
提案

## 背景

`docs/research/Research-07_AutonomousStudio_report.md` §4 で、残る構造的欠陥が
**Inspector 結線ただ 1 つ**に特定された。「人が触れるところ」ではないのに人間の手作業が必須で、
同じ故障が 3 サイクル連続で再発した唯一の場所である。

```
MetaShopDialogView   非アクティブなパネルに View が乗り OnEnable が走らない（指示書 19）
BossBattleDialogView 同型（指示書 18）
EventDialogView      同型（指示書 23）
未バインドの残件      15 件
```

副次的な損害も大きい。

```
MainGame.unity          340,969 bytes ≈ 106,552 トークン
6ca2450 のシーン差分     9,666 行（ボタン 1 個の追加）
全 GameObject の fileID  ビルダー実行のたびに振り直される → 差分がレビュー不能
Tools/Generate Relic Assets の実行禁止   GUID が変わり全参照が切れるため
```

### 決定的な観察

**`UILayoutBuilder.cs` は既に UI 全体をコードから構築している。**
`SetupStatusPanel` / `SetupCommandPanel` / `SetupEventDialogPanel` / `SetupRelicDraftDialogPanel` /
`SetupBossBattleDialogPanel` / `SetupMetaShopDialogPanel` / `SetupEndingPanel` / `SetupUIViews`。

**シーンはその出力をキャッシュしているだけである。** 人間がレイアウトを手で組んだ箇所は無い。

さらに、テスト用に用意した `Bind()` が既に存在する。

```
EndingView.Bind(EndingDecidedChannelSO)
RelicDraftDialogView.Bind(GameFlowController, RelicAcquiredChannelSO)
MetaShopDialogView.Bind(GameFlowController, MetaUnlockCatalogSO)
StatusView.Bind(...)
```

**実行時結線の入口は既に用意されている。** テスト専用の抜け道として作られたものが、本来の経路である。

## 決定内容

**ビルダーの出力をシーンにキャッシュするのをやめ、実行時に構築する。**

### シーンの中身

`MainGame.unity` に残すのは次のみとする。

```
Main Camera
EventSystem
Canvas（空。子を持たない）
Bootstrapper   ← 起動時に UI 全体を構築し、参照を結線する
```

### 参照の解決

`00_rules.md`「参照の解決手段は 2 つだけ: `[SerializeField]` + 人間の Inspector アサイン、
または `Resources.Load`」の**後者に全面的に寄せる。**

- ScriptableObject（`GameRulesSO` / 各 `Catalog` / 各 `Channel`）は
  `Assets/Resources/` から `Resources.Load` で取得する
- スプライト・フォントも同様
- View の内部参照（`_panelRoot` / `_okButton` 等）は、構築したその場で
  `Bind()` に渡すか、直接代入する。**`[SerializeField]` を新設しない**

### `UILayoutBuilder` の扱い

`Game/Assets/Editor/UILayoutBuilder.cs` のレイアウト構築部分を、`UnityEditor` 依存を外して
ランタイム側（`Game/Assets/UI/Scripts/`）へ移す。

- `new GameObject` / `AddComponent` / `RectTransform` の設定は**そのまま動く**
- `SerializedObject` / `FindProperty` による結線は、**直接代入に置き換わる。実装は簡単になる**
- `AssetDatabase.LoadAssetAtPath` は `Resources.Load` に置き換わる

Editor 側には「シーンの雛形（Camera / EventSystem / Canvas / Bootstrapper）を作る」だけを残す。

## 得られるもの

| 現在の問題 | 解消のされ方 |
|---|---|
| 未バインドのシリアライズフィールド 15 件 | **フィールドが存在しなくなる。** 結線漏れはコンパイルエラーか即時の例外になる |
| 非アクティブパネルで `OnEnable` が走らない罠（3 回再発） | 構築順序がコードで一意に決まる。**同じ罠が構造的に起きない** |
| シーン差分 9,666 行、fileID の振り直し | シーンが 20 行程度になる。**差分がレビュー可能になる** |
| `.meta` / GUID の参照崩壊 | シーンが参照を持たないため発生しない |
| アセット再生成の禁止（GUID が変わる） | **禁止が不要になる。** `Resources.Load` はパス解決である |
| 人間の Inspector 作業 | **ゼロ。** 人間の作業は `Assets/Resources/` にファイルを置くことだけになる |
| `docs/snapshot/scene_bindings.txt` の必要性 | 大幅に低下する（当面は残す。§検証で使う） |
| Architect がシーンを読めない制約 | シーンが小さくなり、制約自体が軽くなる |

**エンジンを替えずに、Godot 移行で得られるはずだったものが手に入る。**
移植 6 週間（Research-07 の実測値）ではなく、既存ビルダーから `UnityEditor` 依存を外す作業である。

## 検討した代替案

### 案 A: 現状維持（シーンにキャッシュし、スナップショットで検査する）

**却下。** 指示書 21 のスナップショットは**検出**を自動化したが、**発生**は止めていない。
実際、スナップショット導入後の指示書 23 でも同じ型の未結線が見つかっている。
検出は事後であり、人間の Inspector 作業は残り続ける。

### 案 B: Godot / Web スタックへ移行する

**却下。** Research-07 §1・§3 のとおり、移行根拠が成立しない。
本 ADR は移行で得られる利益の大部分を、移行なしで得る。

### 案 C: Addressables を導入して参照を解決する

**却下。** `00_rules.md` が Addressables の追加を明示的に禁じている。
`Resources.Load` は規約が既に許可しており、この規模で不足しない。

### 案 D: UI Toolkit へ移行する

**却下。** 指示書 23 で uGUI に一本化したばかりである。
また本 ADR が解く問題（結線）は UI Toolkit でも `[SerializeField]` を使えば同じように起きる。
**問題はウィジェット技術ではなく、参照をシーンに焼くことにある。**

## 結果と影響

### 代償

- **Unity エディタ上で UI の見た目を確認できなくなる。** Play するまで画面が出ない。
  ただし現状も `UILayoutBuilder` を走らせるまで同じであり、実質的な後退は小さい
- **レイアウト調整のイテレーションがコード編集になる。** 人間がアセットを作る際、
  配置の微調整をコードで行うことになる。ここは人間の創作作業に触れるため、
  **実際に不便かどうかを §検証 で人間が判断する**
- 起動時に UI 構築のコストがかかる（この規模では体感されないはずだが、実測する）

### 段階

**一度に全部を移さない。** パネル 1 枚で成立を確認してから広げる。

1. `Bootstrapper` と `Resources` 経由の SO ロードを用意する
2. **`EndingPanel` 1 枚だけ**を実行時構築へ移す。シーンからは削除する
3. PlayMode テストと目視で、従来と同じに動くことを確認する
4. 残りのパネルを 1 枚ずつ移す
5. すべて移り終えたらシーンから Canvas の子を消し、`UILayoutBuilder` の役目を終える

### 実施しない範囲

- Brain 層の切り出し（**ADR 0001**）。独立に進められる
- VLM による視覚検証（本 ADR 完了後に再検討。Research-07 §5-1）
- 無人実行の CI 移行（本 ADR でシーンが軽くなってから。Research-07 §6）

### 着手条件

指示書 24（メタプロフィールの永続化）の完了後。
**ADR 0001 より本 ADR を先に実施する。** 効果が大きく、ADR 0001 の作業とも競合しない。
