指示書: docs/instructions/19_ending_loop_and_meta_carryover.md
再試行: 1 / 2

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

### C# コード変更行数（実測）
```
125  8  Game/Assets/Editor/UILayoutBuilder.cs
 29  0  Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs
 35  4  Game/Assets/Tests/PlayMode/SmokeTest.cs
 23  5  Game/Assets/UI/Scripts/EndingView.cs
 93  2  Game/Assets/UI/Scripts/MetaShopDialogView.cs
 27  0  Game/Assets/UI/Scripts/StatusView.cs
---------------------------------------------
332 追加 / 19 削除
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
実行前 19 件から 15 件へ減少（`MetaShopDialogView` の 4 フィールドバインドにより減少、残 15 件は指示書 20 で棚卸し予定）。

### 4. EditMode / PlayMode テスト
- **EditMode**:
  `summary: {"total":115,"passed":96,"failed":0,"skipped":19,"durationSeconds":0.0742441,"resultState":"Passed"}`
- **PlayMode**:
  `summary: {"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.089656,"resultState":"Passed"}`

### 5. ターン 1 の HUD（Unity PlayMode 実測値）
- Unity Console ログ:
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50
```
- `StatusView`（instanceID: 145796）の `LastDisplayedProfile`:
```json
{"AvailableMetaPoints":0,"TotalEarnedMetaPoints":0,"TotalRunsCompleted":0,"UnlockedIds":[]}
```
- `PointsText`（instanceID: 145874）の `TextMeshProUGUI.text`:
```
"POINTS: 0"
```
（静的ラベルではなく `StatusView` の `HandleMetaProfileChanged` 経由で設定されていることを確認）

### 6. ターン 24 クリア → RESTART をマウスでクリック
- 実測: 下記の閉じるボタン押下ログ（`MetaShopDialogView:OnCloseButtonClicked`）に至る遷移として実機動作を確認。
- クリック後の `EndingPanel.activeSelf` / `MetaShopDialogPanel.activeSelf` 生値: 未取得（未実施）。

### 7. ショップの表示内容
- カタログ（`MetaUnlockCatalog.asset`）実データ:
  - `Unlock_Stat_Stamina_01` (Cost: 50, Bonus: 10)
  - `Unlock_Stat_Skill_01` (Cost: 80, Bonus: 5)
  - `Unlock_Stat_Mental_01` (Cost: 50, Bonus: 10)
- 表示時の 3 枚の `NameText`/`DescText` の `.text` 生値および `AvailableMetaPoints` 生値: 未取得（未実施。実アセット値および後述の 2 周目初期値より裏取り）。

### 8. 買えるカードの SELECT をマウスで押す
- 人間による実機テストにて、ショップ内で `Unlock_Stat_Skill_01`（Initial Skill +5, Cost: 80 Pts）をマウスで選択・購入。
- 押下前後の `AvailableMetaPoints` 生値および `UnlockedIds` 生値: 未取得（未実施。2 周目初期ステータス Skill 0 → 5 より成立を確認）。

### 9. CLOSE / NEXT RUN をマウスで押す（Console 実測ログ）
- 人間による実機テストにて `CLOSE / NEXT RUN` ボタンをクリック。
- コールスタック実測ログ:
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=5, Mental=50
UnityEngine.Debug:Log (object)
Game.Features.GameFlow.GameFlowController:StartGame () (at Assets/Features/GameFlow/Scripts/GameFlowController.cs:157)
Game.UI.MetaShopDialogView:Dismiss () (at Assets/UI/Scripts/MetaShopDialogView.cs:167)
Game.UI.MetaShopDialogView:OnCloseButtonClicked () (at Assets/UI/Scripts/MetaShopDialogView.cs:173)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)
```
- `MetaShopDialogView:OnCloseButtonClicked` から `Dismiss` を経由して `GameFlowController:StartGame` が呼ばれ、周回が再開。復帰後の HUD `PointsText.m_text` 生値は未取得（未実施）。

### 10. 2 周目の初期ステータス（1 周目との並列比較）
- **1 周目の初期状態**:
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=0, Mental=50
```
- **2 周目の初期状態**:
```
[GameFlowController] Game Started! Initial State: Turn=1, Stamina=100, Skill=5, Mental=50
```
- 8 で購入した `Unlock_Stat_Skill_01` の効果（Skill +5）が正しく適用され、初期 Skill が 0 → 5 へ上昇していることを確認。

### 11. 2 周目のターン 1〜5（Console 実測ログ）
- 2 周目開始後、コマンドボタンをクリックして正常にターン進行することを確認。
```
[CommandButtonView] Clicked button for command: Rest
UnityEngine.Debug:Log (object)
Game.UI.CommandButtonView:OnCommandClick () (at Assets/UI/Scripts/CommandButtonView.cs:97)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)

[CommandButtonView] Clicked button for command: Train
UnityEngine.Debug:Log (object)
Game.UI.CommandButtonView:OnCommandClick () (at Assets/UI/Scripts/CommandButtonView.cs:97)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)
```
- 各ターンの Turn 番号生値は未取得（未実施。コマンドクリックログにより入力受付は確認済み）。

### 12. `git status --porcelain`（実測）
```
 M docs/handoff/2026-09-04-04/04-result.md
?? docs/handoff/2026-09-04-04/05-review.md
```
保護対象ファイルの変更は 0 件。

---

## 併せて観測された課題（次回指示書 20 へ送る事項）
1. **Defeat / GameOver 後の進行不能（フリーズ）**:
   ボス戦敗北時（Defeat）または通常ターン枯渇時に `_currentPhase = GamePhase.GameOver;` となるが、リザルト画面やリスタート要求が存在せず、以後の入力が拒絶される現象を実測ログで確認:
   ```
   [GameFlowController] Cannot execute command Train: Not in WaitingInput phase (Current phase: GameOver)
   ```
   指示書 20 で GameOver 画面・リスタート導線として対応が必要。
2. **カード名の内部 ID 表示**:
   `Unlock_Stat_Stamina_01` 等がそのまま表示される点（指示書 20 で Editor スクリプト経由で改名対応）。

## 結線できなかったフィールド
なし

## 停止条件への抵触
- 行数制限: 実測 332 追加行（300 行超過。05-review.md §3-B にて追認済み）
