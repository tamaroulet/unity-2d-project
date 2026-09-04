指示書: docs/instructions/20_gameover_ending_and_restart.md
再試行: 0 / 2

## 変更
```
 Game/Assets/Core/Data/EndingKind.cs                   |  3 ++-
 .../Features/GameFlow/Scripts/GameFlowController.cs   |  3 +++
 Game/Assets/Tests/GameFlowControllerTests.cs          | 19 +++++++++++++++++++
 Game/Assets/UI/Scripts/EndingView.cs                  |  1 +
 4 files changed, 25 insertions(+), 1 deletion(-)
```

### C# コード変更行数（実測）
25 追加 / 1 削除（計 26 行、停止条件 150 行以内を達成）

## 検証

### 1. `dotnet build Game/Game.sln -v q --nologo`
```
    3 個の警告
    0 エラー

経過時間 00:00:02.49
```
DLL 競合警告 3 件のみ。エラー 0 件。

### 2. `Tools/Report Non-ASCII Player-Facing Text`
```
[PlayerFacingTextTools] All player-facing text is pure ASCII. OK.
```
0 件。

### 3. `Tools/Report Unbound Serialized Fields`
```
[SceneBindingReport] === Total Unbound Fields: 15 ===
```
実行前 15 件と同数を維持。

### 4. EditMode / PlayMode テスト
- **EditMode**:
```
summary: {"total":116,"passed":96,"failed":0,"skipped":20,"durationSeconds":0.8853816,"resultState":"Passed"}
```
新設テスト `ExecuteCommand_TransitionsToGameOverAndRaisesEndingDecidedChannelWithDefeatWhenMentalDepleted` を含め全緑。
- **PlayMode**:
```
summary: {"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.0442608,"resultState":"Passed"}
```
全緑、実行中の例外 0 件。

### 5〜7. 実機マウス操作による GameOver・リスタート・ショップ復帰（Unity Console 生ログ）
人間による Unity PlayMode でのマウス操作により、ステータス枯渇（またはボス敗北）による GameOver 発生後、リザルト画面に「GAME OVER」が表示され、RESTART ボタンからショップ（MetaShopDialogView）が開き、CLOSE / NEXT RUN からターン 1 の初期ステータス入力待ちへ復帰できることを実証。
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50
UnityEngine.Debug:Log (object)
Game.Features.GameFlow.GameFlowController:StartGame () (at Assets/Features/GameFlow/Scripts/GameFlowController.cs:157)
Game.Features.GameFlow.GameFlowController:Start () (at Assets/Features/GameFlow/Scripts/GameFlowController.cs:93)

[CommandButtonView] Clicked button for command: Train
UnityEngine.Debug:Log (object)
Game.UI.CommandButtonView:OnCommandClick () (at Assets/UI/Scripts/CommandButtonView.cs:97)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)

[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50
UnityEngine.Debug:Log (object)
Game.Features.GameFlow.GameFlowController:StartGame () (at Assets/Features/GameFlow/Scripts/GameFlowController.cs:157)
Game.UI.MetaShopDialogView:Dismiss () (at Assets/UI/Scripts/MetaShopDialogView.cs:167)
Game.UI.MetaShopDialogView:OnCloseButtonClicked () (at Assets/UI/Scripts/MetaShopDialogView.cs:173)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)
```

### 8〜9. ボス戦敗北および通常クリア側の健全性
- ボス戦敗北時もダイアログ dismiss コールバック経由で同様に `EndingKind.Defeat` が Raise され同フローを通ることをコード上確認。
- 既存 PlayMode テスト（`MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions`）がパスしており、通常クリア側の進行・エンディング表示も壊れていない。

### 10. `git status --porcelain`
保護対象ファイルの変更は 0 件。

---

## 停止条件への抵触
なし（変更行数 26 行 < 150 行、エラー再試行 0 回、保護対象ファイル変更なし、新規 View/SO なし）。
