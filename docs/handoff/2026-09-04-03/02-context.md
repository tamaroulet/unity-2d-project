# 相談 2026-09-04-03

## 人間の原文（逐語・改変禁止）
> 勝った後にスキル選べず文字化けしてフリーズする

添付画像: Unity Game View にて、ボス撃破後のレリック選択モーダル（`RELIC DRAFT (SELECT PASSIVE)`）が表示されているが、カード名・説明文・ボタンがすべて `□□□`（豆腐/文字化け）になり、画面下部の Unity Console に「`The character with Unicode value \u308B was not found in the [LiberationSans SDF] font asset or any potential fallbacks...`」の警告が出ている。また、画面上のボタンをクリックしても反応せず先へ進めない（操作不能/フリーズ）。

## 実測データ

### git
```
$ git status --porcelain
?? docs/handoff/2026-09-04-03/

$ git log --oneline -5
05adff6 docs(handoff): record cycle 2026-09-04-02, the first fully scripted round trip
ff1f3fb docs(instructions): tick 17 after verifying each item landed
23d6b02 docs(workflow): stop Claude from driving Unity, and make Gemini tick its own boxes
d20692b feat(ui): bind EndingView and RelicDraftDialogView via UIViews and add 24-turn PlayMode test
50d2566 docs(instructions): tick completed items so the nightly runner picks the right work
```

### ビルド
```
$ dotnet build Game/Game.sln -v q --nologo
    3 個の警告
    0 エラー

経過時間 00:00:01.13
```

### テスト（Unity-MCP run_tests）
```json
// EditMode
{"mode":"EditMode","summary":{"total":115,"passed":96,"failed":0,"skipped":19,"durationSeconds":0.065,"resultState":"Passed"}}

// PlayMode
{"mode":"PlayMode","summary":{"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.009,"resultState":"Passed"}}
```

### エラー・ログ（Editor.log 生ログ）
```
The character with Unicode value \u9244 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [NameText].
The character with Unicode value \u4E0B was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [NameText].
The character with Unicode value \u99C4 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [NameText].
The character with Unicode value \u30BF was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [DescText].
The character with Unicode value \u30FC was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [DescText].
The character with Unicode value \u30F3 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [DescText].
The character with Unicode value \u9078 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [Text].
The character with Unicode value \u629E was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [Text].
The character with Unicode value \u3059 was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [Text].
The character with Unicode value \u308B was not found in the [LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1 in text object [Text].
```

## 既に潰した仮説
- スキル選択ダイアログの表示自体は正常に行われている（`RelicDraftDialogView.IsVisible == true` は成立し、画面上にカード 3 枚が表示されている）。
- 例外（NullReferenceException 等）でゲームループがクラッシュしてフリーズしているわけではない（例外ログは 0 件）。

## 私（Gemini）の見立て
人間が報告した現象は、独立した 2 つの問題が同時に発生しています。

### 問題 1: スキルを選べずフリーズする（クリック不達）
- **原因**: `BossBattleDialogPanel` の全画面オーバーレイ（`Image`）が、ボス戦終了後もアクティブのまま残って GraphicRaycaster を全画面で遮蔽している。
  - `BossBattleDialogView.Show()`（99行）で `gameObject.SetActive(true)` される。
  - ボス戦終了時の `Dismiss()` / `Hide()`（185行）は `_panelRoot.SetActive(false)` を呼ぶが、`gameObject`（`BossBattleDialogPanel` 自身）は `SetActive(false)` されない。
  - `BossBattleDialogPanel` 直下には全画面オーバーレイ `Image`（`raycastTarget = true`）が存在する。
  - Canvas 内の並び順で `BossBattleDialogPanel` は `RelicDraftDialogPanel` より手前（後発 sibling）にあるため、マウスのクリック判定がすべて `BossBattleDialogPanel` の透明オーバーレイに吸着され、奥の `RelicDraftDialogPanel` のボタンまで届かない。
  - **PlayMode テストで見落とした理由**: `SmokeTest.cs` では `ExecuteEvents.Execute(cardBtn.gameObject, ...)` を直接呼んでおり、uGUI の `GraphicRaycaster` をバイパスしていたため、Raycast 遮蔽があっても通過していた。

### 問題 2: 文字化け（豆腐 `□` 表示）
- **原因**: レリックアセット（`RelicSO`）の `DisplayName`（"鉄下駄" 等）や `Description`（"ターン開始時、スタミナが回復する。" 等）、および `UILayoutBuilder` 488行のカードボタン名（"選択する"）が日本語で定義されているが、TextMeshPro のデフォルトフォント `LiberationSans SDF` には ASCII 以外のグリフが存在せず豆腐表示になっている。
- なお、ゲーム内の他 UI（`STUDY`, `TRAIN`, `REST`, `BOSS BATTLE`, `RELIC DRAFT (SELECT PASSIVE)`, `TURN 6 / 24`）はすべて英語表記である。

## Claude に判断してほしいこと

### 課題 1（クリック不達 / Raycast 遮蔽）の解決方針
- **案 1-A（推奨）**: `BossBattleDialogView` の `Hide()` / `Dismiss()` 時に `gameObject.SetActive(false)`（または `_panelRoot` にパネル自身を割り当ててモーダル開閉を一貫化）する。また、`UILayoutBuilder.ConfigureModalPanel` のオーバーレイ Image に不要な Raycast ブロックが起きないよう整理する。
- **案 1-B**: `BossBattleDialogView` も前サイクルの `EndingView` / `RelicDraftDialogView` と同様に `UIViews` ホストへ移設し、モーダルパネル構造を完全に統一する。

### 課題 2（文字化け）の解決方針
- **案 2-A（推奨）**: ゲーム全体の UI トーン（`STUDY`, `TRAIN`, `REST`, `BOSS BATTLE`）に合わせて、レリック名・説明文・選択ボタンの文言を英語表記（English）に統一する（例: `Iron Getas`, `Recover Stamina at start of turn.`, `SELECT`）。フォントアセットの追加インポートや TMP アトラス生成が不要で即座に安定する。
- **案 2-B**: 日本語グリフを含むフォントアセット（Notosans 等）を Unity にインポートし、TMP の Fallback Font または Default Font に設定する。
