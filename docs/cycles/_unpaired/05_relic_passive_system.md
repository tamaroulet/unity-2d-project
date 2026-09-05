# 第4週 指示書：レリック（パッシブ能力）・ドラフト獲得システム

本指示書は、ローグライク育成シミュレーションの核となる「レリック（パッシブ能力）システム」および「3択ドラフト獲得UI」の仕様・実装手順を定義する。

---

## 1. 全体の方針とアーキテクチャ

CodingSpec 4節「アーキテクチャ」の3層分離を厳守する。

| 層 | 実装 | 責務 |
|---|---|---|
| **ロジック層** | `RelicSO`, `RelicCatalogSO`, `RelicResolverSO` | 純粋関数によるレリック効果の計算・適用 |
| **状態層** | `GameState.AcquiredRelicIds` | 不変レコードによる獲得済みレリックIDの保持 |
| **通信層** | `RelicAcquiredChannelSO` | レリック獲得時の疎結合イベント通知 |
| **ビュー層** | `RelicDraftDialogView`, `RelicListView` | 3択レリック選択画面、所持レリック一覧表示 |

---

## 2. 作成するファイル一覧

* アセンブリ: `Game.Features.Relic`（参照: `Game.Core`, `Game.Features.Command`）
* ディレクトリ: `Assets/Features/Relic/Scripts/`

| ファイル | 内容 |
|---|---|
| `RelicTriggerKind.cs` | `enum RelicTriggerKind { OnTurnStart, OnCommandExecuted, OnTurnEnd }` |
| `RelicEffect.cs` | `readonly struct RelicEffect(int staminaDeltaBonus, int skillDeltaBonus, int mentalDeltaBonus, float staminaCostMultiplier)` |
| `RelicSO.cs` | ScriptableObject。レリック定義データ |
| `RelicCatalogSO.cs` | ScriptableObject。全レリックカタログおよびランダム3択抽出 |
| `RelicResolverSO.cs` | ScriptableObject。純粋関数 `ApplyCommandRelics(...)`, `ApplyTurnRelics(...)` |
| `RelicAcquiredChannelSO.cs` | `EventChannelSO<int>`。レリックID獲得通知 |
| `RelicResolverSOTests.cs` | レリック効果の計算、重複適用、上限値クランプ等の単体テスト |
| `RelicDraftDialogView.cs` | 3択レリック獲得画面 MonoBehaviour |

---

## 3. レリック定義（代表例 6種）

1. **集中のハチマキ (ID: 1)**: 特訓コマンド実行時、獲得スキル +5
2. **癒しの香炉 (ID: 2)**: 休養コマンド実行時、スタミナ回復 +15
3. **効率的なノート (ID: 3)**: 座学コマンド実行時、メンタル +5
4. **鋼の意志 (ID: 4)**: 全コマンドのスタミナ消費が 20% 軽減（コスト倍率 0.8）
5. **モーニングルーティン (ID: 5)**: 毎ターン開始時、スタミナ +5
6. **瞑想の極意 (ID: 6)**: 毎ターン終了時、メンタル +3

---

## 4. 完了条件

- `RelicResolverSOTests` を含む全 EditMode テストが全件通過すること。
- コンパイルエラー・警告が 0 件であること。
