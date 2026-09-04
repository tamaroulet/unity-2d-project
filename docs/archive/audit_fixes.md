# 監査指摘に基づくアーキテクチャ事前補強 指示書

Audit-01 レポートで指摘された、ローグライク拡張（レリック・中間試練）の前提となる基盤の補強を実施します。

---

## 1. 修正対象と具体的な仕様

### A. `Game/Assets/Core/Data/GameState.cs`
- `FiredEventMask` の型を `int` から `ulong`（64ビット）に変更し、イベントID上限を拡張する。

### B. `Game/Assets/Features/Event/Scripts/EventResolverSO.cs`
- `FiredEventMask` のビットシフト・ビットマスク演算を `ulong`（`1UL << (gameEvent.EventId - 1)` 等）に対応させる。

### C. `Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`
- `BeginTurn()` のフェーズ遷移修正:
  - イベントが発火した場合: `_currentPhase = GamePhase.ShowingEvent;` を設定し、`WaitingInput` には上書きしない（イベント表示状態で待機）。
  - イベントが発火しなかった場合: `_currentPhase = GamePhase.WaitingInput;` を設定。
- `OnEventDismissed()`:
  - `_currentPhase = GamePhase.WaitingInput;` に遷移する。
- `ExecuteCommand(CommandDataSO command)`:
  - `if (_currentPhase != GamePhase.WaitingInput) return;` のガードを徹底。

### D. `Game/Assets/Features/Command/Scripts/CommandResolverSO.cs`
- `Resolve(GameState state, CommandDataSO command, GameRulesSO rules)` メソッドを追加し、将来のレリック拡張（特定コマンドの判定）に備える。

### E. 全テストコード（`EventResolverSOTests.cs`, `GameFlowControllerTests.cs`, `UIViewTests.cs` 等）
- `ulong` への型変更やフェーズ遷移の厳密化に伴うテストの追従・パス確認。

---

## 2. 完了条件
- 既存の全テスト（47件）がすべてパスすること。
- コンパイルエラー・警告が 0 件であること。
