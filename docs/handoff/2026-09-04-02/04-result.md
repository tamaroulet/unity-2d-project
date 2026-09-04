指示書: docs/instructions/17_ending_and_draft_binding.md
再試行: 0 / 2

## 変更
```
 Game/Assets/Editor/UILayoutBuilder.cs   |  179 +-
 Game/Assets/Scenes/MainGame.unity       | 9266 ++++++++++++++++---------------
 Game/Assets/Tests/PlayMode/SmokeTest.cs |  157 +-
 3 files changed, 4941 insertions(+), 4661 deletions(-)
```

## 検証

### 1. `dotnet build Game/Game.sln -v q --nologo`
```
    3 個の警告
    0 エラー

経過時間 00:00:01.30
```

### 2. `Tools/Report Unbound Serialized Fields` の出力
```
[SceneBindingReport] === Total Unbound Fields: 19 ===
StatusView / _staminaGauge
StatusView / _skillGauge
StatusView / _mentalGauge
CommandButtonView / _costText
CommandButtonView / _costText
CommandButtonView / _costText
EventDialogView / _eventFiredChannel
EventDialogView / _eventCatalog
EventDialogView / _panelRoot
EventDialogView / _titleText
EventDialogView / _bodyText
EventDialogView / _okButton
EventDialogView / _gameFlowController
BossBattleDialogView / _bossHpSlider
BossBattleDialogView / _battleLogText
MetaShopDialogView / _panelRoot
MetaShopDialogView / _availablePointsText
MetaShopDialogView / _totalRunsText
MetaShopDialogView / _closeButton
```
- `EndingView` の 3 件（`_endingDecidedChannel`, `_panelRoot`, `_resultText`）はすべて解消。
- `RelicDraftDialogView` の 4 件（`_relicAcquiredChannel`, `_gameFlowController`, `_panelRoot`, `_cardViews`）もすべてバインド済み。
- 未結線残件数は 22 件 → 19 件へ減少。

### 3. EditMode / PlayMode テスト
#### EditMode
```json
{"mode":"EditMode","summary":{"total":115,"passed":96,"failed":0,"skipped":19,"durationSeconds":0.0652887,"resultState":"Passed"}}
```
#### PlayMode
```json
{"mode":"PlayMode","summary":{"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.0090318,"resultState":"Passed"}}
```
- 実行前: EditMode 115 (96 passed, 0 failed, 19 skipped) / PlayMode 2 (2 passed)
- 実行後: EditMode 115 (96 passed, 0 failed, 19 skipped) / PlayMode 3 (3 passed, 0 failed)（改善）

### 4. ターン 24 の通過
確認方法: Unity-MCP (`MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions`)
- ターン 1 から 24 までのコマンド実行・ボス戦 4 回を通過。
- 最終ボス撃破後に `EndingView.IsPanelActive == true` に到達。
- リザルトテキスト (`DisplayedResult`) が正常に設定されていることを確認。
- 実行中の Error / Exception / Assert は 0 件。

Console ログ抜粋:
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50
[CommandButtonView] Clicked button for command: Train
...
[CommandButtonView] Clicked button for command: Rest
...
[TestRunnerNoThrottle] Restored Interaction Mode after test run.
```

### 5. ターン 6/12/18 の通過
確認方法: Unity-MCP (`MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions` および `MainGame_AdvanceToTurn6_BossBattleDismiss_AdvancesToTurn7_WithZeroExceptions`)
- ターン 6/12/18 の各ボス戦後に `RelicDraftDialogView.IsVisible == true`（モーダル表示）を確認（全 3 回）。
- `RelicCardView` の `SelectButton` を押下してレリックを選択し、ダイアログが閉じて次ターン（`WaitingInput`）へ復帰することを確認。

### 6. ターン 1〜5 の見た目
確認結果:
- `Canvas/UIViews` は `RectTransform` のみで生成し、`Image` コンポーネントは付与していない。
- `EndingPanel` および `RelicDraftDialogPanel` は初期状態で非アクティブ（`SetActive(false)`）のまま維持。
- 全画面半透明オーバーレイ Image は非アクティブであり、画面全体を覆うことはない。
- ターン 1〜5 のコマンドボタン（`StudyButton`, `TrainButton`, `RestButton`）のレイキャストが奪われることなく、ポインタクリックが正常に受理されることを PlayMode テストで確認。

### 7. `git status --porcelain`
```
 M Game/Assets/Editor/UILayoutBuilder.cs
 M Game/Assets/Scenes/MainGame.unity
 M Game/Assets/Tests/PlayMode/SmokeTest.cs
?? docs/handoff/2026-09-04-02/03-instruction.md
```
保護対象ファイル（`.agents/rules/**`, `.claude/**`, `scripts/**` 等）への変更は 1 件もなし。

## 結線できなかったフィールド
なし（`EndingView` の 3 件、`RelicDraftDialogView` の 4 件すべて結線完了）

## 停止条件への抵触
なし
