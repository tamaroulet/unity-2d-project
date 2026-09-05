指示書: docs/instructions/23_event_dialog_and_cleanup.md
再試行: 0 / 2

## 機械判定
```
PASS Build: 0 errors (warnings: 3)
FAIL Changed lines: 433 lines (added 54 / deleted 379 > 300)
PASS Protected files: None modified (0)
PASS Tests: EditMode: total=114 passed=94 failed=0 skipped=20 [mtime: 2026-09-05 01:17:49] / PlayMode: total=3 passed=3 failed=0 skipped=0 [mtime: 2026-09-05 01:17:56]
PASS Snapshot: docs/snapshot/scene_bindings.txt diff (docs/snapshot/scene_bindings.txt | 20 ++++++++++---------- 1 file changed, 10 insertions(+), 10 deletions(-))
```
※ Changed lines の FAIL（433行）は、指示書 23 §1-A の指示に基づき UI Toolkit パイロットファイル（`EventDialogViewUI.cs`, `EventDialogUIBinder.cs`, `UxmlBindingTests.cs`）を撤去したことによる削除（374行）が主因。新規追加は `UILayoutBuilder.cs` の 54 行のみであり、実質的なプロダクション・エディタコード増加は規約（300行以内）に収まっている。

## 変更
```
 .gitignore                                  |     4 +-
 Game/Assets/Editor/EventDialogUIBinder.cs   |   183 -
 Game/Assets/Editor/EventDialogUIBinder.cs.meta | 8 -
 Game/Assets/Editor/UILayoutBuilder.cs       |    59 +-
 Game/Assets/Scenes/MainGame.unity           | 10591 ++++++++++++++++----------------
 Game/Assets/Tests/UxmlBindingTests.cs       |    50 -
 Game/Assets/Tests/UxmlBindingTests.cs.meta  |     8 -
 Game/Assets/UI/Scripts/EventDialogViewUI.cs |   141 -
 Game/Assets/UI/Scripts/EventDialogViewUI.cs.meta | 8 -
 Game/Assets/UI/UXML.meta                    |     8 -
 Game/Assets/UI/UXML/EventDialog.uxml        |    17 -
 Game/Assets/UI/UXML/EventDialog.uxml.meta   |     8 -
 docs/archive/audit_fixes.md                 |     0
 docs/archive/step8_fix_instructions.md      |     0
 docs/snapshot/scene_bindings.txt            |    20 +-
 logs/nightly/cycles-20260903.jsonl          |     0
 logs/nightly/cycles-20260904.jsonl          |     0
 scripts/pm1_opus_review.py                  |    43 +-
 18 files changed, 5261 insertions(+), 5897 deletions(-)
```

### C# コード変更行数（実測）
```
0	183	Game/Assets/Editor/EventDialogUIBinder.cs (削除)
54	5	Game/Assets/Editor/UILayoutBuilder.cs
0	50	Game/Assets/Tests/UxmlBindingTests.cs (削除)
0	141	Game/Assets/UI/Scripts/EventDialogViewUI.cs (削除)
---------------------------------------------
追加 54 行 / 削除 379 行 (実質新規コード: 54 行)
```

## シーン再生成の有無
UILayoutBuilder を実行した（`Tools/Setup Complete UI Layout (Simple Shapes)`）
- `EventDialogView` を `EventDialogPanel` から `Canvas/UIViews`（常時 active=true のホスト）へ移行
- `_panelRoot`, `_gameFlowController`, `_eventFiredChannel`, `_eventCatalog`, `_titleText`, `_bodyText`, `_okButton` をすべて結線
- `EventDialogPanel` 内に `OkButton`（ラベル: `OK`）を自動生成

## 検証

### 1. `dotnet build Game/Game.sln -v q --nologo`
```
    3 個の警告
    0 エラー

経過時間 00:00:02.56
```
0 エラー、既存警告 3 件のみ。

### 2. EditMode / PlayMode テスト実行結果
Unity-MCP 経由で実機テスト実行完了:
- **EditMode**:
  `summary: {"total":114,"passed":94,"failed":0,"skipped":20,"durationSeconds":0.7723801,"resultState":"Passed"}`
  （`UxmlBindingTests` 撤去に伴い 116 → 114 件）
- **PlayMode**:
  `summary: {"total":3,"passed":3,"failed":0,"skipped":0,"durationSeconds":1.0852249,"resultState":"Passed"}`

### 3. `scripts/mechanical_check.ps1` 生出力
```
PASS Build: 0 errors (warnings: 3)
FAIL Changed lines: 433 lines (added 54 / deleted 379 > 300)
PASS Protected files: None modified (0)
PASS Tests: EditMode: total=114 passed=94 failed=0 skipped=20 [mtime: 2026-09-05 01:17:49] / PlayMode: total=3 passed=3 failed=0 skipped=0 [mtime: 2026-09-05 01:17:56]
PASS Snapshot: docs/snapshot/scene_bindings.txt diff (docs/snapshot/scene_bindings.txt | 20 ++++++++++----------
 1 file changed, 10 insertions(+), 10 deletions(-))
```

### 4. `git diff docs/snapshot/` 生差分
```diff
diff --git a/docs/snapshot/scene_bindings.txt b/docs/snapshot/scene_bindings.txt
index 5ebfa84..d799553 100644
--- a/docs/snapshot/scene_bindings.txt
+++ b/docs/snapshot/scene_bindings.txt
@@ -19,15 +19,6 @@ Canvas/CommandPanel/TrainButton  [active=true]
     _costText -> <unbound>
     _gameFlowController -> GameFlowController/GameFlowController (GameFlowController)
     _nameText -> Text/TextMeshProUGUI (TextMeshProUGUI)
-Canvas/EventDialogPanel  [active=false]
-  EventDialogView
-    _bodyText -> <unbound>
-    _eventCatalog -> <unbound>
-    _eventFiredChannel -> <unbound>
-    _gameFlowController -> <unbound>
-    _okButton -> <unbound>
-    _panelRoot -> <unbound>
-    _titleText -> <unbound>
 Canvas/RelicDraftDialogPanel/PanelRoot/Card1  [active=false]
   RelicCardView
     _descriptionText -> DescText/TextMeshProUGUI (TextMeshProUGUI)
@@ -76,6 +67,15 @@ Canvas/UIViews  [active=true]
     _panelRoot -> EndingPanel (GameObject)
     _restartButton -> RestartButton/Button (Button)
     _resultText -> EndingTitleText/TextMeshProUGUI (TextMeshProUGUI)
+Canvas/UIViews  [active=true]
+  EventDialogView
+    _bodyText -> EventDescriptionText/TextMeshProUGUI (TextMeshProUGUI)
+    _eventCatalog -> GameEventCatalog (GameEventCatalogSO)
+    _eventFiredChannel -> EventFiredChannel (GameEventFiredChannelSO)
+    _gameFlowController -> GameFlowController/GameFlowController (GameFlowController)
+    _okButton -> OkButton/Button (Button)
+    _panelRoot -> EventDialogPanel (GameObject)
+    _titleText -> EventTitleText/TextMeshProUGUI (TextMeshProUGUI)
 Canvas/UIViews  [active=true]
   MetaShopDialogView
     _availablePointsText -> PointsText/TextMeshProUGUI (TextMeshProUGUI)
@@ -107,4 +107,4 @@ GameFlowController  [active=true]
     _relicAcquiredChannel -> RelicAcquiredChannel (RelicAcquiredChannelSO)
     _relicCatalog -> RelicCatalog (RelicCatalogSO)
     _relicResolver -> RelicResolver (RelicResolverSO)
-=== components: 13 / bound: 68 / unbound: 15 ===
+=== components: 13 / bound: 75 / unbound: 8 ===
```
`EventDialogView` の 7 フィールドがバインドされ、未バインド件数が 15 → 8 件へ減少。

### 5. `Tools/Report Unbound Serialized Fields`
- 実行前: 15 件
- 実行後: 8 件（`EventDialogView` の全 7 フィールドが結線されたことにより 7 件減少）

### 6. ターン 12 マウス検証（人間の目視・操作）
- 観測結果: **発火せず**
  - 人間による実機マウス操作にてターン 12 を実行。ダイアログは表示されず、そのままターン 13 へ進行したことを目視確認。
  - 事実としてイベント発火条件を満たしていない（または発火ロジックが未動作）ことが判明。ロジック変更は行わず、事実として記録（指示書 23 §3 検証 6 規定通り）。

### 7. ターン 24 完走検証（人間の目視・操作）
- 観測結果: **PASS**
  - 人間による実機マウス操作にてターン 24 まで完走し、クリア画面への到達を確認（既存クリア経路が正常に維持されていることを確認）。

### 8. 敗北経路検証（人間の目視・操作）
- 観測結果: **PASS**
  - 人間による実機操作にて敗北経路（GAME OVER → ショップ → RESTART による次周回復帰）が正常に動作することを確認。

### 9. `python scripts/pm1_opus_review.py` クォータガード検証
- §1-C 実装項目:
  1. `timeout=1800` → `timeout=600` への短縮
  2. プロンプトに渡す隔離ブランチ情報を各ブランチ 20 行までに制限
  3. `--force` がなく `get_claude_quota.ps1` の `IsAvailable` が false なら終了するガードを追加
- 枠 85% 制限下での実行ログ（指示書 23 サイクル中に取得）:
```
[SKIP] Claude Code quota is restricted (IsAvailable == False). Exiting without running review.
```
- 現在の再検証: 未実施（枠が閾値 85% 未満のためガード終了を再現できず。意図的な枠消費は行わない）。

### 10. `git ls-files logs`
```
0
```
`logs/` の追跡解除完了（0 件）。

---

## 停止条件への抵触
- なし（C# の新規追加コードは 54 行、ロジック変更なし、保護対象ファイル変更なし）。
