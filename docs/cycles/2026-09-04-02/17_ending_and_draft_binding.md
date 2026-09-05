# 指示書 17: エンディング画面とレリックドラフトの結線・購読修正

```
ROLE: Executor
BRANCH: main
判断: docs/cycles/2026-09-04-02/03-instruction.md（案2 採用・実装方針は本書に従う）
```

## 目的

ターン 24 の最終ボスに勝ってもエンディング画面が出ず、クリア後に完全停止する。
ターン 6/12/18 のボス勝利後もレリックドラフト画面が出ずに素通りする。

原因は 2 つが重なっている。**片方だけ直しても直らない。**

1. `UILayoutBuilder.SetupEndingPanel()`（335-349 行）は `EndingPanel` と `EndingView` を
   生成するが、`SerializedObject` を一切触っていない。`_endingDecidedChannel` /
   `_panelRoot` / `_resultText` が全て null。`BindEndingViewSceneReferences` は存在しない。
2. `EndingView` も `RelicDraftDialogView` も **`OnEnable()` でしか購読しない**のに、
   両者が乗っている GameObject が `SetActive(false)` で生成されている
   （349 行 / 292 行）。`OnEnable()` が一度も走らず、購読が成立しない。

指示書 16 で直った `BossBattleDialogView` は、常時アクティブな `StatusView` が
直参照で呼び出す形だったため、この問題を踏んでいない。

## 実装方針（この形で直すこと。自分で別案に変えないこと）

`Canvas` 直下に**常時アクティブな空のホスト** `UIViews` を作り、
`EndingView` と `RelicDraftDialogView` をそこに移す。
各 View の `_panelRoot` には**モーダルパネルの GameObject 自身**を張る。

```
Canvas
  UIViews                  ← 新規・アクティブ・RectTransform のみ・Image を付けない
    ├ EndingView           _panelRoot = EndingPanel
    └ RelicDraftDialogView _panelRoot = RelicDraftDialogPanel
  EndingPanel              ← 非アクティブのまま
  RelicDraftDialogPanel    ← 非アクティブのまま
```

これで起動時に `OnEnable()` が走って購読が成立し、表示時は `_panelRoot.SetActive(true)` が
パネルごと開く。**`EndingView.cs` と `RelicDraftDialogView.cs` は変更しない。**

やってはいけない代替:
- パネルをアクティブのままにする → `ConfigureModalPanel` の全画面オーバーレイ Image が
  常時画面を覆い、コマンドボタンのクリックも奪う。View 側に消す手段が無い
- `_panelRoot` に View 自身のホストを張る → `OnEnable()` の `SetActive(false)` で自分が落ちる

## タスク

- [x] `Game/Assets/Editor/UILayoutBuilder.cs` に、`Canvas` 直下へ `UIViews`（`RectTransform` のみ、
      `Image` なし、アクティブ）を生成する処理を追加する。全パネル生成後・
      `RebindGameFlowControllerReferences` の前に置くこと
- [x] `EndingView` と `RelicDraftDialogView` のコンポーネント配置を `UIViews` 配下に移す。
      `SetupEndingPanel` / `SetupRelicDraftDialogPanel` は**パネルの見た目の生成のみ**を担当し、
      View の付与と結線は分離すること
- [x] `BindEndingViewSceneReferences(Transform canvasTr)` を追加し、581-583 行の既存 3 メソッドと
      同じ場所から呼ぶ。実装は `BindBossBattleDialogSceneReferences` に揃える
      - `_endingDecidedChannel` → `BindAsset<EndingDecidedChannelSO>` で
        `Assets/Data/Channels/EndingDecidedChannel.asset`
        （`RebindGameFlowControllerReferences` 562 行が使っているものと**同一アセット**であること）
      - `_panelRoot` → `EndingPanel` の GameObject
      - `_resultText` → `EndingPanel/PanelRoot/EndingTitleText`
        （`EndingDescriptionText` ではない。エンディング名を出すフィールドである）
- [x] `BindRelicDraftDialogSceneReferences` を、移動後の配置に合わせて更新する。
      `_gameFlowController` に加えて `_panelRoot` = `RelicDraftDialogPanel` の GameObject、
      `_cardViews` の 3 件、`_relicAcquiredChannel` が移動後も張られていること
- [x] 参照先が見つからない場合は**黙って null を書かず** `Debug.LogError` で名指しする
      （`BindAsset` / `BindComponentReference` と同じ方針）。
      `BindComponentReference` のエラーメッセージが `BossBattleDialogPanel` 決め打ちなので、
      使い回すなら対象名を引数で渡せるようにすること
- [x] `Tools/Setup Complete UI Layout (Simple Shapes)` を実行してシーンを更新する
- [x] PlayMode テストを 1 本追加する。**ターン 1 から 24 まで進め、最終ボス撃破後に
      `EndingView.IsPanelActive == true` になることを、例外ゼロで確認する**。
      既存の `MainGame_AdvanceToTurn6_BossBattleDismiss_AdvancesToTurn7_WithZeroExceptions` に
      形を揃える。途中のボス（6/12/18）では `RelicDraftDialogView.IsVisible == true` を
      確認してからカードを選択して閉じること

## 判断が要る場面

- 結線先の要素が見つからない場合、**推測で近い要素を割り当てない**。`Debug.LogError` を出し、
  フィールド名を報告に列挙して手を止める
- 24 ターン通しの途中で、**エンディングでもレリックドラフトでもない箇所**（イベントダイアログ、
  メタショップ等）で進行が止まった場合、その停止位置と Console の生ログを報告して手を止める。
  **本書の対象を勝手に広げないこと。** 未結線は他に 22 件あり、切り分けの設計判断が要る

## 触ってはいけないもの

```
.agents/rules/**  .claude/**  .github/workflows/**
scripts/nightly_gate.py  scripts/auto_runner.py
scripts/morning_report.py  scripts/nightly_baseline.json
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。
シーンの変更は `Game/Assets/Editor/` の Editor スクリプト経由で行う（`00_rules.md`）。
`EndingView.cs` / `RelicDraftDialogView.cs` の本体は変更しない。
`MetaShopDialogView` / `EventDialogView` / UI Toolkit パイロットには手を出さない。

## 検証

**結線できただけでは完了としない。ターン 24 でエンディング画面が出ることを確認する。**

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー |
| 2 | `Tools/Report Unbound Serialized Fields` の出力 | `EndingView` の 3 件が消えていること。残件数も報告する |
| 3 | EditMode / PlayMode テスト | 実行前（EditMode 115/96 passed, PlayMode 2/2 passed）と同じか改善。件数を明記 |
| 4 | **ターン 24 の通過** | 最終ボス撃破後にエンディング画面が**画面に出る**こと |
| 5 | **ターン 6/12/18 の通過** | レリックドラフト画面が**画面に出て**、選択後に次ターンへ進むこと |
| 6 | ターン 1〜5 の見た目 | オーバーレイが画面を覆っていないこと。コマンドボタンが押せること |
| 7 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 4・5 の確認方法は 2 つ。**どちらを使ったか明記すること。**

- Unity を開いて実際に Play し、ターン 24 まで進めて Console を確認する
- Unity-MCP で操作できるなら、それでもよい

検証 6 は今回の修正でオーバーレイの扱いを変えるため必須。**目視した結果を書くこと。**

> **注意**: 24 ターン完走の EditMode テストでは、この不具合は検出できない。
> EditMode にはシーンが無く `OnRelicDraftRequested` にも `EndingDecidedChannel` にも
> 購読者がいないため、`GameFlowController` のフォールバックが働いて先へ進んでしまう。
> **論理は緑なのに実機は詰む**という偽陰性になる。検証 4・5 を省略しないこと。

## 停止条件

- 同じ修正を 2 回試して直らない
- `.unity` を直接編集しないと進めなくなった
- 結線先の要素が存在せず、どれを割り当てるか設計判断が要る
- エンディング / レリックドラフト以外の箇所で 24 ターン通しが止まった
- 保護対象ファイルの変更が必要になった

## 報告

`docs/cycles/2026-09-04-02/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/cycles/2026-09-04-02/17_ending_and_draft_binding.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## 検証
（上の 1〜7 の生出力を全部。特に 4・5・6 は Console のログをそのまま）

## 結線できなかったフィールド
（あれば名指しで。無ければ「なし」）

## 停止条件への抵触
なし / あり（内容）
```

コミットのみ行い、push はしない。
