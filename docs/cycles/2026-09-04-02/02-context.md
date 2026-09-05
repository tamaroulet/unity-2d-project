# 相談 2026-09-04-02

## 人間の原文（逐語・改変禁止）
> ターン12/18/24も同じ地雷が無いか確認したい。handoff で回して

## 実測データ

### git
```
$ git status --porcelain
?? docs/cycles/2026-09-04-02/

$ git log --oneline -5
d78bdaa docs(handoff): approve instruction 16 and close cycle 2026-09-04-01
9446a3a fix(ui): bind BossBattleDialogView references and add SceneBindingReport (instruction 16)
3e26b54 feat(scripts): wire the IDE-to-Claude leg of the handoff loop
62fa9e1 docs(handoff): issue instruction 16 for the boss dialog binding (2026-09-04-01)
6375950 docs(handoff): add context for turn-6 boss battle progression issue (instruction 15)
```

### ビルド
```
$ dotnet build Game/Game.sln -v q --nologo
    3 個の警告
    0 エラー

経過時間 00:00:00.66
```

### テスト
```
取得方法: Unity-MCP run_tests

EditMode (job_id: 95504bbe6f2d4c10add67f28c53d0cfa):
total=115, passed=96, failed=0, skipped=19, durationSeconds=0.7668181, resultState=Passed

PlayMode (job_id: ed0186f4f3ee4c34b977a3a1906bbcd6):
total=2, passed=2, failed=0, skipped=0, durationSeconds=0.7651863, resultState=Passed
- MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions (Passed)
- MainGame_AdvanceToTurn6_BossBattleDismiss_AdvancesToTurn7_WithZeroExceptions (Passed)
```

### エラー・ログ / 実測値
```
Tools/Report Unbound Serialized Fields の実測値 (22件中、進行関連):
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
EndingView / _endingDecidedChannel  (★null)
EndingView / _panelRoot             (★null)
EndingView / _resultText            (★null)

GameFlowController.cs の遷移コード実測 (254-277行):
- Turn 12 (Act 2), Turn 18 (Act 3): isFinalBoss == false -> proceedToDraft() 経由でレリック獲得へ遷移。
- Turn 24 (Act 4): isFinalBoss == true -> triggerEnding() 経由で _endingDecidedChannel?.Raise(ending); FinalizeRun(isGameClear: true); へ遷移。
```

## 既に潰した仮説
- ターン12/18のボス戦自体で詰む説:
  指示書 16 で BossBattleDialogView の結線（_panelRoot, _dismissButton等）を完了したため、ターン12および18のボス戦ダイアログ表示と Dismiss は動作する。
- ターン12/18でレリックドラフトが詰む説:
  RelicDraftDialogView は依然として初期非アクティブのため UI は開かないが、GameFlowController.cs:288 の自動フォールバック OnRelicAcquired(fallbackRelicId) が働き、ソフトロックは回避されて次のターンへ進む（ただし UI は表示されない）。

## 私（Gemini）の見立て
- 存在している地雷:
  - 地雷A (確定詰み): **ターン24の最終ボス勝利後、エンディング画面（EndingView）が一切表示されずゲームクリア後に完全停止する。**
    - 原因: `EndingView` の `_endingDecidedChannel`, `_panelRoot`, `_resultText` が全て null（SceneBindingReport で実測済み）。さらに `EndingPanel` が初期非アクティブのためイベント購読も行われない。`UILayoutBuilder.cs` に `BindEndingViewSceneReferences` が存在しない。
  - 地雷B (UX欠落): **ターン6/12/18のボス勝利後、レリックドラフト画面（RelicDraftDialogView）が表示されずに素通りする。**
    - 原因: `RelicDraftDialogPanel` が初期非アクティブ（active: false）で生成されており、`OnEnable()` による `OnRelicDraftRequested` 購読が一度も走らない。
  - 地雷C (進行不能): **ボス戦敗北時（GameOver）に表示する UI が存在しない。**
- 推す優先度: 地雷A（ターン24の EndingView 結線）を最優先で解消すべき。

## Claude に判断してほしいこと
ターン12/18/24 の地雷解消方針として、指示書 17 でどこまでを対象とするか:
- 案1 (最小限): ターン24 の確定詰みである `EndingView` の結線（`UILayoutBuilder` への `BindEndingViewSceneReferences` 追加とシーン更新）を修正し、ターン 24 完走・クリア画面到達の PlayMode テストを追加する。
- 案2 (推奨・完全解消): 案1 に加え、`RelicDraftDialogView` の初期購読問題（`RelicDraftDialogPanel` の起動時バインドまたは明示的アクティブ化）も修正し、ドラフト UI 表示まで含めて 24 ターン完全通しを保証する。
- 案3 (全件一括): 案2 に加え、`MetaShopDialogView` を含む未結線 22 件を `UILayoutBuilder` で一掃する。
