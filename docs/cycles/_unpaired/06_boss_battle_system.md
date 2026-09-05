# 指示書 #6：中間試練・ボスバトルシステム（第5週）

## 1. 概要と目的

本指示書は、第4週（レリック・パッシブシステム）完了後の第5週として、
段階的に出現するボス試練（第12ターン・第24ターン・第36ターン極限試練）の
3層分離アーキテクチャと実装仕様を定義する。

プレイヤーはこれまで育成したパラメータ（体力・技術・精神）および所持レリックのパッシブ効果を駆使して、
ボスの目標ゲージ（HP）を削り切るターン制コマンド戦闘を行う。

**核心仕様（ビルド深化とボス撃破報酬）**:
- **ボス撃破報酬（覚醒ボスパッシブ）**: ボスを撃破した際、通常のレリックを遥かに凌駕する強力な「覚醒ボスパッシブ（Boss Relic）」を3択ドラフトで獲得し、次なる階層（Act 2 / Act 3）へと進出する。
- **強さに応じたプレイ深度のスケーリング**: ビルドが完成していない初期は第1ボスでサクッと敗北してソウルを回収し、壊れコンボが完成したランでは第2・第3の強大ボスへと到達して超ロングランの熱狂を味わえる。

---

## 2. 3層分離アーキテクチャ設計

```mermaid
graph TD
    subgraph 🎮 UI 層 (Game.UI)
        BV[BossBattleView<br>ボスHPバー / 行動予告 / 戦闘コマンドUI]
    end

    subgraph ⚙️ 進行制御層 (Game.Features.GameFlow)
        GFC[GameFlowController<br>GamePhase.BossBattle への遷移制御]
    end

    subgraph 🧠 ロジック層 (Game.Features.Boss)
        BSO[BossSO<br>最大HP / 攻撃力 / 行動パターン ScriptableObject]
        BState[BossState<br>不変レコード: CurrentHP / Shield / TurnCount]
        BResolver[BossBattleResolverSO<br>戦闘計算純粋関数 Resolver]
    end

    BV -->|戦闘コマンド選択| GFC
    GFC -->|状態とコマンドを委譲| BResolver
    BResolver -->|新しい GameState & BossState を返却| GFC
    GFC -->|チャンネル通知| BV
```

---

## 3. 型定義と不変データ構造

### 3.1 `BossState`（不変レコード）
```csharp
namespace Game.Features.Boss
{
    public record BossState
    {
        public int BossId { get; init; }
        public int CurrentHp { get; init; }
        public int MaxHp { get; init; }
        public int Shield { get; init; }
        public int BattleTurn { get; init; }
        public bool IsDefeated => CurrentHp <= 0;
    }
}
```

### 3.2 `BossActionKind`（ボスの行動種別）
```csharp
namespace Game.Features.Boss
{
    public enum BossActionKind
    {
        Attack,         // プレイヤーのスタミナにダメージ
        MentalPressure, // プレイヤーのメンタルにダメージ
        Guard,          // 自身にシールド付与
        SpecialAttack   // 大ダメージ予告攻撃
    }
}
```

### 3.3 `BossSO`（ScriptableObject 定義）
```csharp
namespace Game.Features.Boss
{
    [CreateAssetMenu(menuName = "Game/Boss/Boss", fileName = "Boss")]
    public class BossSO : ScriptableObject
    {
        [SerializeField] private int _bossId;
        [SerializeField] private string _bossName;
        [SerializeField] private int _maxHp;
        [SerializeField] private int _attackPower;
        [SerializeField] private int _mentalPressurePower;
        [SerializeField] private List<BossActionKind> _actionPattern;

        public int BossId => _bossId;
        public string BossName => _bossName;
        public int MaxHp => _maxHp;
        public int AttackPower => _attackPower;
        public int MentalPressurePower => _mentalPressurePower;
        public IReadOnlyList<BossActionKind> ActionPattern => _actionPattern;
    }
}
```

---

## 4. 純粋関数 `BossBattleResolverSO`

```csharp
namespace Game.Features.Boss
{
    [CreateAssetMenu(menuName = "Game/Boss/BossBattleResolver", fileName = "BossBattleResolver")]
    public class BossBattleResolverSO : ScriptableObject
    {
        /// <summary>
        /// プレイヤーのアクションとボスの行動を計算し、戦闘結果を返す純粋関数。
        /// </summary>
        public BattleTurnResult ResolveTurn(
            GameState playerState,
            BossState bossState,
            BattlePlayerAction playerAction,
            BossSO bossData,
            IReadOnlyList<RelicSO> activeRelics,
            GameRulesSO rules)
        {
            // 1. プレイヤーのアクション適用（スキル値依存の攻撃 / スタミナ回復防御 / 精神統一）
            // 2. 所持レリックによるダメージ・防御補正
            // 3. ボスの行動適用（スタミナ減少 / メンタル減少 / シールド展開）
            // 4. 勝敗判定（Boss HP <= 0 なら勝利、Player Stamina <= 0 なら敗北）
        }
    }
}
```

---

## 5. Step 展開計画（第5週）

| Step | 対象ファイル / 責務 | 担当 |
|---|---|---|
| **Step 12** | `BossState`, `BossActionKind`, `BossSO`, `BossCatalogSO`（ボスデータ基盤） | Claude Code |
| **Step 13** | `BossBattleResolverSO` & 単体テスト（戦闘計算・レリック相乗効果） | Claude Code |
| **Step 14** | `GameFlowController` への `GamePhase.BossBattle` 統合 & イベントチャンネル | Gemini / Claude |
| **Step 15** | `BossBattleView`（ボスHPバー・行動予告・戦闘UI）& シーン結合 | Gemini |

---

## 6. Claude 投入時のプロンプト設計（20:49用）

20:49 のセッション枠回復時、安全ラッパー経由で以下の通り Claude を投入する：
```powershell
powershell -File scripts/invoke_claude_safe.ps1 -Prompt "docs/cycles/_unpaired/06_boss_battle_system.md に従い、Step 12 および Step 13 の C# クラス群（Game.Features.Boss）と包括的な単体テストコードを実装し、全テスト合格を達成してください。"
```
