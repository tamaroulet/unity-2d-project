# GATE2-01: Opus 監査結果および確定実装指示書

発行日: 2026-09-03
発行者: Claude Opus（アーキテクト / Gate 2 承認権者）
実装担当: Gemini（Gate 3・忠実実装のみ。逸脱・追加設計を一切禁止）
対象: EditMode テスト 127件中 3件 Failed の解消

---

## 0. 監査結果サマリ（Gate 2 判定）

**Gemini からの修正計画素案は本チャットに本文が到達していない**（Gate 1 診断データも3件目の途中で切断）。
したがって「素案の監査」は実施できず、代わりに Opus 自身が Gate 1 をソース実地確認で再取得し、
**素案に代わる確定指示書を本書として発行する**。Gemini は本書のみに従うこと。

**判定: 3件の Failed は、いずれもテスト側の誤りではなく、9/02 22:51〜9/03 00:20 の
16連続 `fix:` コミット群で本番コードに混入した「対症療法」による回帰である。**
よって **テストを書き換えて緑にすることを禁止する。本番コードを直す。**

| # | 失敗テスト | 混入元 | 根本原因 |
|---|---|---|---|
| 1 | `ExecuteCommand_IsIgnoredWhilePhaseIsShowingEvent` | 23:55「コマンド実行時のフェーズ自動復帰(Auto-recovery)」 | `ExecuteCommand` が `ShowingEvent` を勝手に `WaitingInput` へ書き換えて実行してしまう |
| 2 | `StartGame_TriggersBossBattleAtBossTurn_AndTransitionsToShowingRelicDraftOnVictory` | 9c02c10「OnBossBattleOccurred 経由で…」 | 購読者不在時に `OnRelicAcquired()` を即時自己呼び出しし、`ShowingRelicDraft` を即 `WaitingInput` へ落とす |
| 3 | `BossBattleDialogView_Show_SetsPanelActive_AndUpdatesTextsAndGauges` | fbe0757「ボタンテキストを実行時に ASCII 英語へ動的上書き」 | `_dismissButton.GetComponentInChildren<TextMeshProUGUI>()` の無制限探索が `_bossNameText` を掴み、ボス名を "VICTORY / NEXT ACT" で破壊 |

---

## 1. 修正 A: `GameFlowController.ExecuteCommand` の Auto-recovery 撤去

**ファイル**: `Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`
**対象行**: 174〜186 行（13行）

### 削除する現行コード（完全一致）

```csharp
            if (_currentPhase != GamePhase.WaitingInput)
            {
                if (_currentPhase == GamePhase.ShowingRelicDraft || _currentPhase == GamePhase.ShowingEvent || _currentPhase == GamePhase.TurnStart)
                {
                    Debug.Log($"[GameFlowController] Auto-recovering phase {_currentPhase} to WaitingInput for player command execution.");
                    _currentPhase = GamePhase.WaitingInput;
                }
                else
                {
                    Debug.LogWarning($"[GameFlowController] Cannot execute command {command?.name}: Not in WaitingInput phase (Current phase: {_currentPhase})");
                    return;
                }
            }
```

### 置換後コード

```csharp
            if (_currentPhase != GamePhase.WaitingInput)
            {
                Debug.LogWarning($"[GameFlowController] Cannot execute command {command?.name}: Not in WaitingInput phase (Current phase: {_currentPhase})");
                return;
            }
```

### 根拠

`ExecuteCommand` は「入力を受け付けるか否かの判定者」であって「フェーズの所有者」ではない。
呼び出され側が自分の都合で状態機械を書き換えることは、状態機械の契約の破棄に等しい。
当該テストはこの契約を明文化した唯一のテストであり、契約側が正しい。

### 副作用の事前検証（Opus 実施済み）

- `GameMonteCarloSimulationTests.cs` の全3ループ（70/154/263 行）は
  `if (CurrentPhase == ShowingEvent) controller.OnEventDismissed();` を明示的に呼んでおり、
  Auto-recovery に依存していない。**回帰しない。**
- `GameFlowControllerTests.cs` 318〜329 行のループはイベント無しカタログを使用。**回帰しない。**
- `GameFlowControllerRelicTests.cs` 84/105 行は `WaitingInput` 中の呼び出し。**回帰しない。**

---

## 2. 修正 B: ボス戦勝利時の `OnRelicAcquired` 即時自己呼び出しの撤去

**ファイル**: `Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`
**対象行**: 289〜305 行（`BeginTurn()` 内・勝利分岐）

### 削除する現行コード（完全一致）

```csharp
                    if (battleResult.Outcome == BattleOutcomeKind.Victory)
                    {
                        _bossDefeatedCount++;
                        _currentPhase = GamePhase.ShowingRelicDraft;
                        if (OnBossBattleOccurred != null)
                        {
                            OnBossBattleOccurred.Invoke(boss, battleResult, () =>
                            {
                                OnRelicAcquired(bossId);
                            });
                        }
                        else
                        {
                            OnRelicAcquired(bossId);
                        }
                        return;
                    }
```

### 置換後コード

```csharp
                    if (battleResult.Outcome == BattleOutcomeKind.Victory)
                    {
                        _bossDefeatedCount++;
                        _currentPhase = GamePhase.ShowingRelicDraft;
                        OnBossBattleOccurred?.Invoke(boss, battleResult, () =>
                        {
                            OnRelicAcquired(bossId);
                        });
                        return;
                    }
```

### 根拠

`ShowingRelicDraft` は「プレイヤーがレリックを選ぶまで待つ」ためのフェーズである。
購読者不在時に自分でドラフトを解決するのは、フェーズの存在意義そのものの否定であり、
「勝利したのにドラフト画面が出ない」という実行時バグと同義である。

### 副作用の事前検証（Opus 実施済み）

- シーン側の購読者は `StatusView.HandleBossBattle`（`StatusView.cs` 66〜78行）であり、
  `BossBattleDialogView` が見つからない場合は `callback?.Invoke()` で即座に解決する
  フォールバックを既に内蔵している。**シーン上でスタックしない。**
- `StatusView.HookFlowControllerEvents()` は `Start()` と `Update()` の両方で購読するため、
  最短のボス戦ターン（Turn 6 = プレイヤー入力5回後）までに購読は必ず完了している。
- テスト側は `GameMonteCarloSimulationTests`（77/161/270 行）および
  `GameFlowControllerTests` 320〜324 行が `ShowingRelicDraft` を明示的にハンドルしている。**回帰しない。**

### 敗北分岐（306〜321 行）は変更禁止

対称性は崩れるが、`FinalizeRun(isGameClear: false)` の呼び出し回数保証と、
`BossBattleDialogView.Update`（69〜82行）側フォールバックが `StartGame()` を呼ぶことによる
コールバック不整合が絡む。**本ゲートでは一切触らないこと**。第5節に積み残しとして記録する。

---

## 3. 修正 C: イベントダイアログの解除をコントローラへ結線

**ファイル**: `Game/Assets/UI/Scripts/EventDialogView.cs`
**対象行**: 112〜118 行

これは修正 A の前提条件である。現状 `ShowingEvent` を解除する経路がシーン上に存在せず
（`controller.OnEventDismissed()` の呼び出し元は MonteCarlo テストのみ）、それが Auto-recovery を
生んだ真因である。**修正 A と必ず同一コミットで実施すること。**

### 3-1. using の追加（3行目 `using Game.Core;` の直後）

```csharp
using Game.Features.GameFlow;
```

### 3-2. `OnEventDismissed()` の置換

削除する現行コード（完全一致）:

```csharp
        private void OnEventDismissed()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }
```

置換後コード:

```csharp
        private void OnEventDismissed()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            GameFlowController controller = Object.FindFirstObjectByType<GameFlowController>();
            if (controller != null && controller.CurrentPhase == GamePhase.ShowingEvent)
            {
                controller.OnEventDismissed();
            }
        }
```

`CurrentPhase == ShowingEvent` のガードにより、EditMode テスト
（`EventDialogView_OnEventDismissed_HidesPanel`, UIViewTests 237行）では確実に no-op となる。

---

## 4. 修正 D: ボス戦ダイアログのボタンラベル上書き範囲の限定

**ファイル**: `Game/Assets/UI/Scripts/BossBattleDialogView.cs`

### 4-1. フィールド追加（24 行 `_dismissButton` 宣言の直後）

```csharp
        [SerializeField] private TextMeshProUGUI _dismissButtonText;
```

### 4-2. `EnsureReferences()` 内 44〜51 行の置換

削除する現行コード（完全一致）:

```csharp
            if (_dismissButton != null)
            {
                TextMeshProUGUI btnText = _dismissButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "AUTO BATTLE / NEXT";
                }
            }
```

置換後コード:

```csharp
            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = "AUTO BATTLE / NEXT";
            }
```

### 4-3. `Show()` 内 107〜116 行の置換

削除する現行コード（完全一致）:

```csharp
            if (_dismissButton != null)
            {
                TextMeshProUGUI btnText = _dismissButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = battleResult != null && battleResult.Outcome == BattleOutcomeKind.Victory
                        ? "VICTORY / NEXT ACT"
                        : "DEFEAT / RESTART";
                }
            }
```

置換後コード:

```csharp
            if (_dismissButtonText == null)
            {
                _dismissButtonText = ResolveDismissButtonText();
            }

            if (_dismissButtonText != null)
            {
                _dismissButtonText.text = battleResult != null && battleResult.Outcome == BattleOutcomeKind.Victory
                    ? "VICTORY / NEXT ACT"
                    : "DEFEAT / RESTART";
            }
```

### 4-4. 解決メソッドの新規追加（`EnsureReferences()` メソッドの閉じ括弧の直後に配置）

```csharp
        /// <summary>
        /// 決定ボタンのラベルを解決する。ボス名・HP・シールド・戦闘ログとして
        /// 既にバインドされているテキストは、誤ってボタンラベルとして上書きしないよう除外する。
        /// </summary>
        private TextMeshProUGUI ResolveDismissButtonText()
        {
            if (_dismissButton == null)
            {
                return null;
            }

            foreach (TextMeshProUGUI candidate in _dismissButton.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (candidate == _bossNameText || candidate == _bossHpText ||
                    candidate == _shieldText || candidate == _battleLogText)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
```

### 根拠と挙動保証

- **テスト環境**: `_dismissButton` は `viewObject` 自身に AddComponent され、4つの
  TextMeshProUGUI は `viewObject` の子である。`GetComponentsInChildren` の候補は
  この4つのみで全て除外対象 → `null` を返し、**ボス名は上書きされない**。
- **実シーン**: `PanelRoot/AutoBattleNextButton` の子ラベルは上記4フィールドのいずれでもないため
  正常に解決され、従来通り ASCII 英語ラベルが適用される（文字化け対策は維持される）。
- `EnsureReferences()` 内では `_bossNameText`（39行）〜`_dismissButton`（42行）の解決が
  先行するため除外判定は必ず成立する。**4-2 のブロックは 42 行より後に置くこと。**

---

## 5. 本ゲートで意図的に扱わない事項（積み残し・次ゲート対象）

以下は Gemini が本ゲートで触れることを**禁止**する。勝手に直した場合は Gate 3 違反とする。

1. `GameFlowController.EnsureDependencies()` の `#if UNITY_EDITOR` 全体包囲（92〜113 行）。
   **修正 A・B により対症療法が剥がれるため、ビルド時の null 依存は今後隠蔽されず露出する。**
   これは望ましい変化だが、WebGL ビルドの初回検証で失敗し得る。次ゲートで
   `Resources.Load` / Addressables / シーンのシリアライズ参照へ移行する。
2. `StatusView.Update()` / `BossBattleDialogView.Update()` の毎フレーム `FindFirstObjectByType` 探索。
3. ボス戦敗北分岐（`GameFlowController.cs` 306〜321 行）のコールバック不整合。
4. `docs/webgl/` の再ビルドと公開（DIAGNOSIS-01 §2.3 証拠C）。

---

## 6. Gate 4 完了条件（DoD）

本指示書の実装は、以下を**全て**満たして初めて完了とする。

1. `refresh_unity` 実行後、コンパイルエラー 0 件。
2. `run_tests`（EditMode）で **127/127 Passed**。上記3件が解消し、かつ**新規 Failed が 0 件**
   （特に MonteCarlo 3件と Relic 系を目視確認）。
3. `read_console` で新規の LogError / 例外が 0 件。
4. **シーン再バインド**: `MainGame.unity` の `BossBattleDialogView` インスペクタで、
   新規追加した `_dismissButtonText` に `AutoBattleNextButton` 配下のラベルを**明示的に割り当てて保存する**
   （実行時解決に頼らない。これが本修正の要点である）。
5. **PlayMode 手動スモーク**: Play → コマンド押下でターンが進む → イベント発生時に OK でダイアログが閉じ、
   **その後もコマンドボタンが反応する**（修正 A・C の検証）→ Turn 6 でボス戦ダイアログが出て
   ボス名が正しく表示される（修正 D の検証）→ 例外 0 件。
6. `docs/spec/CodingSpec.md` の状態遷移記述を「`ShowingEvent` / `ShowingRelicDraft` は
   外部からの解除呼び出しを待つ」旨へピンポイント差分更新。

**4〜5 を飛ばして「テストが緑だから完了」と報告することを、DIAGNOSIS-01 §2 に基づき明示的に禁止する。**
