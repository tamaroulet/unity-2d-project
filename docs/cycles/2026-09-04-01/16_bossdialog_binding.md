# 指示書 16: ボス戦ダイアログの結線と、未結線の可視化

```
ROLE: Executor
BRANCH: main
判断: docs/cycles/2026-09-04-01/03-instruction.md（案1 採用）
```

## 目的

ターン 6 のボス戦後に進めない。`BossBattleDialogView` の `[SerializeField]` 8 個が
**すべて null** で、`_dismissButton` が無いため `proceedToDraft` が呼ばれない。

`UILayoutBuilder` はパネルとコンポーネントを生成する（298 行）が、**結線していない**。
`StatusView` と `RelicDraftDialogView` には結線メソッドがある（581-582 行）のに、
`BossBattleDialogView` の分だけ存在しない。

## タスク

- [x] `Game/Assets/Editor/UILayoutBuilder.cs` に `BindBossBattleDialogSceneReferences(Transform canvasTr)` を追加し、581-582 行の既存 2 メソッドと同じ場所から呼ぶ。`BossBattleDialogPanel` 配下の実要素を `_panelRoot` / `_bossNameText` / `_bossHpText` / `_bossHpSlider` / `_shieldText` / `_battleLogText` / `_dismissButton` / `_dismissButtonText` に結線すること。実装は既存の `BindStatusViewSceneReferences` に揃える
- [x] 参照先の要素がパネル配下に存在しない場合は、**黙って null を書かず** `Debug.LogError` でどの要素が見つからなかったかを名指しすること（`BindAsset` と同じ方針）
- [x] `Tools/Setup Complete UI Layout (Simple Shapes)` を実行してシーンを更新する
- [x] `Game/Assets/Editor/SceneBindingReport.cs` を新規作成する。`Tools/Report Unbound Serialized Fields` メニューで `MainGame.unity` を走査し、`Game.UI` 名前空間の `MonoBehaviour` について `[SerializeField]` が null のものを「コンポーネント名 / フィールド名」の形で全件 Console に出力すること。**テストにはしない**（現在 28 箇所あり、いきなり赤にすると他の作業が止まるため）。まず可視化する

## 判断が要る場面

要素が見つからず結線できないフィールドがあった場合、**推測で近い要素を割り当てないこと**。
`Debug.LogError` を出したうえで、そのフィールド名を報告に列挙して手を止める。

## 触ってはいけないもの

```
.agents/rules/**  .claude/**  .github/workflows/**
scripts/nightly_gate.py  scripts/auto_runner.py
scripts/morning_report.py  scripts/nightly_baseline.json
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。
シーンの変更は `Game/Assets/Editor/` の Editor スクリプト経由で行う（`00_rules.md`）。
他の View や UI Toolkit パイロットには手を出さない。

## 検証

**結線できただけでは完了としない。ターン 6 を越えられることを確認する。**

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー |
| 2 | `Tools/Report Unbound Serialized Fields` の出力 | `BossBattleDialogView` の 8 件が消えていること。残りの件数も報告する |
| 3 | EditMode / PlayMode テスト | 実行前と同じか改善（件数を明記） |
| 4 | **ターン 6 の通過** | ボス戦ダイアログを閉じて 7 ターン目に進めること |
| 5 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 4 の確認方法は 2 つ。**どちらを使ったか明記すること。**

- Unity を開いて実際に Play し、ターン 6 まで進めて Console を確認する
- Unity-MCP で操作できるなら、それでもよい

> **注意**: 24 ターン完走の EditMode テストを書いても、この不具合は検出できない。
> EditMode にはシーンが無く `OnBossBattleOccurred` に購読者がいないため、
> `GameFlowController` のフォールバックが働いて先へ進んでしまう。
> **論理は緑なのに実機は詰む**という偽陰性になる。検証 4 を省略しないこと。

## 停止条件

- 同じ修正を 2 回試して直らない
- `.unity` を直接編集しないと進めなくなった
- 結線先の要素が存在せず、どれを割り当てるか設計判断が要る
- 保護対象ファイルの変更が必要になった

## 報告

`docs/cycles/2026-09-04-01/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/cycles/2026-09-04-01/16_bossdialog_binding.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## 検証
（上の 1〜5 の生出力を全部。特に 4 は Console のログをそのまま）

## 結線できなかったフィールド
（あれば名指しで。無ければ「なし」）

## 停止条件への抵触
なし / あり（内容）
```

コミットのみ行い、push はしない。
