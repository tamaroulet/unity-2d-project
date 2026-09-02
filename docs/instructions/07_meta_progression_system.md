# 第6週 指示書：周回メタ永続化・アンロックシステム（MetaProgression）

## 1. 目的と責務
ターン終了・ゲームオーバー・クリア時に獲得する `MetaPoints` を計算し、初期ステータス底上げや新規レリックの解放を行うメタプログレッション基盤を構築する。

3層分離・純粋関数・不変レコード規約を厳守し、UIや通信から完全に独立した C# クラス群として実装する。

---

## 2. アセンブリ構成（Game.Features.MetaProgression）

- **asmdef**: `Game.Features.MetaProgression`
- **依存**: `Game.Core`, `Game.Features.Command`, `Game.Features.Relic`, `Game.Features.Boss`

---

## 3. 型定義・不変レコード仕様

### 3.1 `MetaProfileState` (record)
```csharp
public sealed record MetaProfileState
{
    public int AvailableMetaPoints { get; init; }
    public int TotalEarnedMetaPoints { get; init; }
    public int TotalRunsCompleted { get; init; }
    public IReadOnlyList<int> UnlockedIds { get; init; } = System.Array.Empty<int>();
}
```

### 3.2 `MetaUnlockKind` (enum)
- `InitialStaminaBonus` (初期スタミナ底上げ)
- `InitialSkillBonus` (初期スキル底上げ)
- `InitialMentalBonus` (初期メンタル底上げ)
- `UnlockRelic` (新レリックのドラフトプール解放)

### 3.3 `MetaUnlockSO` (ScriptableObject)
- `_unlockId`: int (一意キー)
- `_unlockName`: string (メタデータキー。例: `Unlock_Stat_Stamina_01`)
- `_cost`: int (必要メタポイント)
- `_kind`: MetaUnlockKind
- `_bonusValue`: int (加算値)
- `_targetRelicId`: int (解放対象レリックID)

### 3.4 `MetaPointResolverSO` (ScriptableObject / 純粋関数)
- **計算式**:
  - 到達ターンボーナス: `CurrentTurn * 5`
  - 最終スキルボーナス: `Skill * 2`
  - ボス撃破ボーナス: `BossDefeatedCount * 50`
  - ゲームクリアボーナス: `IsGameClear ? 100 : 0`
- `CalculateEarnedPoints(GameState finalState, bool isGameClear, int bossDefeatedCount) => int`
- `ApplyUnlock(MetaProfileState currentProfile, MetaUnlockSO unlock) => (MetaProfileState newProfile, bool success)`
