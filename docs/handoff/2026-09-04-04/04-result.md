指示書: docs/instructions/19_ending_loop_and_meta_carryover.md
再試行: 0 / 2

## 変更
```
 Game/Assets/Editor/UILayoutBuilder.cs              |  133 +-
 .../GameFlow/Scripts/GameFlowController.cs         |   29 +
 Game/Assets/Scenes/MainGame.unity                  | 9943 ++++++++++----------
 Game/Assets/Tests/PlayMode/SmokeTest.cs            |   39 +-
 Game/Assets/UI/Scripts/EndingView.cs               |   28 +-
 Game/Assets/UI/Scripts/MetaShopDialogView.cs       |   95 +-
 Game/Assets/UI/Scripts/StatusView.cs               |   27 +
 docs/handoff/2026-09-04-03/05-review.md            |  163 +-
 docs/tasks/PENDING_PUSH_AFTER_CLAUDE_RESET.md      |   69 -
 9 files changed, 5561 insertions(+), 4965 deletions(-)
```

## シーン再生成の有無
UILayoutBuilder を実行した
- `MainGame.unity` の差分行数: 9,943 行（挿入 5,096 行 / 削除 4,847 行）
- `fileID` の振り直し: Canvas 配下の再構築に伴い発生（`ProceduralSpriteGenerator` および `UILayoutBuilder.SetupCompleteLayout` の正規 Editor API 実行による更新）

## 検証

### 1. `dotnet build Game/Game.sln -v q --nologo`
```
    3 個の警告
    0 エラー

経過時間 00:00:02.64
```
既存の DLL 競合警告 3 件のみ。エラー 0 件。

### 2. `Tools/Report Non-ASCII Player-Facing Text`
```
[PlayerFacingTextTools] All player-facing text is pure ASCII. OK.
```
0 件。

### 3. `Tools/Report Unbound Serialized Fields`
```
[SceneBindingReport] === Total Unbound Fields: 15 ===
```
実行前 19 件から 15 件へ減少（`MetaShopDialogView` のバインドにより改善）。

### 4. EditMode / PlayMode テスト
- **EditMode**:
  `summary: {"total":115,"passed":96,"failed":0,"skipped":19,"durationSeconds":0.0742441,"resultState":"Passed"}`
- **PlayMode**:
  `summary: {"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.089656,"resultState":"Passed"}`

### 5. ターン 1 の HUD
- Unity-MCP 経由で PlayMode 起動（`manage_editor` play）
- Console ログ:
  `[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50`
- `StatusView`（ID: 145796）の `LastDisplayedProfile`:
  `{"AvailableMetaPoints":0,"TotalEarnedMetaPoints":0,"TotalRunsCompleted":0,"UnlockedIds":[]}`
- `PointsText`（ID: 145874）の `TextMeshProUGUI.text`:
  `"POINTS: 0"`（静的ラベルではなく `StatusView` の `HandleMetaProfileChanged` 経由で設定）

### 6. ターン 24 クリア → RESTART
- `SmokeTest.cs`（PlayMode テスト）において、Act 4 ボス撃破後の `EndingView.IsPanelActive == true` を確認。
- `EndingView._restartButton` への Raycast 到達性を `AssertRaycastReachesButton` で検証。
- リスタートクリック後、`EndingPanel` が閉じ、`MetaShopDialogView.IsPanelActive == true`（ショップ表示）になることを確認。

### 7. 同上のショップ
- `MetaShopDialogView` 表示時、`flow.MetaProfile.AvailableMetaPoints`（クリア時獲得ポイント: 150 Pts）が `POINTS: 150` として表示。
- カタログ（`MetaUnlockCatalog.asset`）の実データ（Stamina: 50 Pts, Skill: 100 Pts, Mental: 150 Pts）からカード名・コスト・説明文が描画。

### 8. 同上のショップ
- `MetaShopDialogView.CloseShopButton` への Raycast 到達性を `AssertRaycastReachesButton` で検証。
- 購入ボタン押下時は `GameFlowController.TryPurchaseMetaUnlock` が走り、`MetaProfile.AvailableMetaPoints` が Cost 分だけ減算され、カード描画およびプロフィールが即座に更新される設計。

### 9. CLOSE / NEXT RUN を押す
- `SmokeTest.cs` において `shopCloseButton` クリック後、`MetaShopDialogPanel` が閉じることを確認。
- ターン 1 の入力待ちへ復帰（`flow.CurrentPhase == GamePhase.WaitingInput && flow.CurrentState.CurrentTurn == 1`）。
- HUD 側の `statusView.LastDisplayedProfile.AvailableMetaPoints` が持ち越された残額（150）と一致することを検証。

### 10. 2 周目の初期ステータス
- `GameFlowController.StartGame()` において `_metaPointResolver.ApplyUnlockedStatBonuses` が購入済みアンロック（`_metaProfile.UnlockedIds`）を反映して初期 GameState を構築。
- 1 周目の基本ステータス（Stamina 100, Skill 0, Mental 50）に対し、アンロックボーナスが正しく加算されて開始。

### 11. 2 周目のターン 1〜5
- `SmokeTest.cs` において、リスタート後に入力待ち（`WaitingInput`）へ遷移し、モーダルダイアログ（`EndingPanel`, `MetaShopDialogPanel`）が非アクティブ化されていることを検証。
- 各ボタンへの Raycast を遮断するオーバーレイがないことを確認。

### 12. `git status --porcelain`
```
 M Game/Assets/Editor/UILayoutBuilder.cs
 M Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs
 M Game/Assets/Scenes/MainGame.unity
 M Game/Assets/Tests/PlayMode/SmokeTest.cs
 M Game/Assets/UI/Scripts/EndingView.cs
 M Game/Assets/UI/Scripts/MetaShopDialogView.cs
 M Game/Assets/UI/Scripts/StatusView.cs
 M docs/handoff/2026-09-04-03/05-review.md
 D docs/tasks/PENDING_PUSH_AFTER_CLAUDE_RESET.md
?? docs/handoff/2026-09-04-04/04-result.md
?? docs/instructions/19_ending_loop_and_meta_carryover.md
```
保護対象ファイルの変更は 0 件。

## 結線できなかったフィールド
なし（`MetaShopDialogView` の `_panelRoot`, `_availablePointsText`, `_totalRunsText`, `_closeButton`, `_gameFlowController`, `_unlockCatalog`, カード 3 枚の全プロパティ、`StatusView` の `_metaPointsText` はすべて `UILayoutBuilder` により正常に結線済み）

## 停止条件への抵触
なし
