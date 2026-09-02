# 実装指示書 #3：イベントとエンディング（第2週）

## 1. 本書の位置づけ

本書は作業依頼であり、事前承認ではない。`AGENTS.md` 第1節に従い、各 Step は
変更計画の提示 → 人間の承認 → 実装 → 停止 の順で進める。計画提示と同一の応答内で
実装に着手してはならない。

本書と `docs/spec/CodingSpec.md` / `docs/spec/BuildSpec.md` / `AGENTS.md` が
矛盾する場合、実装せずに矛盾点を報告して停止すること。

## 2. 対象範囲

| Step | 対象 | アセンブリ |
|---|---|---|
| 4 | イベントチャンネル基盤 | `Game.Core` |
| 5 | イベント定義と条件評価 | `Game.Features.Event` |
| 6 | エンディング判定 | `Game.Features.Ending` |

各 Step の完了時に必ず停止する。Step をまたいで作業を続けない。

ビュー層（MonoBehaviour）、シーン、プレハブ、UI は本書の対象外である。
ターン進行のステートマシンへの統合も対象外であり、第3週に行う。

## 3. 前提条件

各 Step の着手前に以下を確認する。**存在しない場合は作成せず、報告して停止すること。**

| Step | 必要なもの |
|---|---|
| 4 | `Assets/Core/Events/` フォルダ |
| 5 | `Assets/Features/Event/Scripts/` フォルダと `Game.Features.Event.asmdef` |
| 6 | `Assets/Features/Ending/Scripts/` フォルダと `Game.Features.Ending.asmdef` |
| 5,6 | `Game.Tests.EditMode.asmdef` に該当アセンブリへの参照が追加済みであること |

フォルダおよび `.asmdef` の作成は人間が行う。エージェントは作成してはならない。

## 4. 全体の設計方針

### 4-1. 乱数の禁止

型フェーズでは確率的要素を実装しない（`CodingSpec` 7節）。

- `UnityEngine.Random` および `System.Random` を使用しない
- `static` な乱数状態に依存しない
- 将来乱数を挿入する場合は引数として受け取る形にする。実装を「乱数を考慮していない」
  状態にしないこと

### 4-2. 純粋関数

`EventResolverSO` および `EndingResolverSO` は純粋関数として実装する。
入力は引数のみ、出力は戻り値のみとし、自身のフィールドを書き換えない。
`Time` / `Application` / `Debug` を含む Unity のランタイム API に依存しない。

### 4-3. 型の制約

| 用途 | 使用する型 |
|---|---|
| ScriptableObject のクラス定義 | 通常の `class`。`record` は禁止 |
| 戻り値の複合型 | `record`（`record class`）を使用してよい |
| 値型 | `readonly struct`。`record struct` は C# 10 機能のため使用不可 |

`init` セッターに必要な `IsExternalInit` は `Game.Core` に `public` で宣言済みである。

### 4-4. AI生成の明示

作成する `.cs` ファイルの先頭行に、以下を記述する。

    // SPDX-AI-Disclosure: ai-generated

`CodingSpec` 15節に基づく。既存ファイルへの遡及付与は本書の対象外である。

## 5. Step 4：イベントチャンネル基盤

### 5-1. 作成するファイル

`Assets/Core/Events/` 配下に配置する。

| ファイル | 内容 |
|---|---|
| `EventChannelSO.cs` | `public abstract class EventChannelSO<T> : ScriptableObject` |
| `GameStateEventChannelSO.cs` | `EventChannelSO<GameState>` の具象型 |
| `GameEventFiredChannelSO.cs` | `EventChannelSO<int>` の具象型（発火したイベントのID） |
| `EndingDecidedChannelSO.cs` | `EventChannelSO<EndingKind>` の具象型 |

`EndingDecidedChannelSO` は `EndingKind` を必要とするため、`EndingKind` enum は
`Game.Core` 側（`Assets/Core/Data/`）に定義する。`Game.Core` は他アセンブリを
参照できないためである。

### 5-2. `EventChannelSO<T>` の要件

- `event System.Action<T>` を公開する
- `Raise(T value)` メソッドで購読者へ通知する
- 購読者がいない場合も例外を投げない
- `OnDisable()` で購読を解除する（エディタの Play モード切り替えで購読が
  重複して残るのを防ぐため）

ジェネリック型には `[CreateAssetMenu]` を付けない。**具象型3種にのみ付ける。**
Unity はジェネリックな ScriptableObject の直接生成に対応していないためである。

### 5-3. テスト

`Assets/Tests/` に `EventChannelSOTests.cs` を作成する。

| # | 検証内容 |
|---|---|
| 1 | `Raise()` で購読者のハンドラが1回呼ばれる |
| 2 | 渡した値がハンドラに正しく届く |
| 3 | 購読者が0件の状態で `Raise()` しても例外が発生しない |
| 4 | 購読解除後は `Raise()` してもハンドラが呼ばれない |
| 5 | 複数の購読者すべてにイベントが届く |

`ScriptableObject.CreateInstance<T>()` でインスタンスを生成する。`.asset` に依存しない。

## 6. Step 5：イベント定義と条件評価

### 6-1. 作成するファイル

`Assets/Features/Event/Scripts/` 配下に配置する。

| ファイル | 内容 |
|---|---|
| `EventTriggerKind.cs` | `enum { TurnReached, ParameterBelow }` |
| `TrackedParameter.cs` | `enum { Stamina, Skill, Mental }` |
| `GameEventSO.cs` | イベント1件の定義（通常クラス） |
| `GameEventCatalogSO.cs` | `GameEventSO` のリストを保持する |
| `EventResolverSO.cs` | 条件評価。純粋関数 |
| `EventResult.cs` | 評価結果（`record`） |

### 6-2. `GameEventSO` のフィールド

すべて `[SerializeField]` の private フィールドとし、public プロパティで公開する。

| フィールド | 型 | 用途 |
|---|---|---|
| イベントID | `int` | 0〜31。発火済み管理のビット位置 |
| 表示名 | `string` | 第3週の UI で使用 |
| 本文 | `string` | 同上 |
| 発火条件の種別 | `EventTriggerKind` | |
| 発火ターン | `int` | `TurnReached` の場合に使用 |
| 対象パラメータ | `TrackedParameter` | `ParameterBelow` の場合に使用 |
| 閾値 | `int` | 同上 |
| 優先度 | `int` | 値が大きいものを優先 |
| 効果 | `CommandEffect` の各値 | `StaminaCost` は 0 とする |

効果値は `int` のフィールドとして持ち、評価時に `CommandEffect` を組み立てる。
`CommandEffect` は `readonly struct` でありシリアライズに適さないためである。

### 6-3. `GameState` への追加

`Assets/Core/Data/GameState.cs` に発火済み管理のフィールドを追加する。
**`Game.Core` への変更はこの1点のみ許可する。**

- フィールド名は `FiredEventMask`、型は `int`、`init` セッターとする
- イベントID `n` が発火済みであることを、ビット `n` が立っている状態で表す
- 初期値は 0。`GameRulesSO.CreateInitialState()` の変更が必要な場合は
  計画に含めて報告すること

`List` や配列ではなくビットマスクを使用する理由は、`record` の値等価性を
維持するためである。参照型のコレクションをフィールドに持つと、内容が同一でも
等価と判定されず、既存テストの前提が崩れる。

### 6-4. `EventResolverSO.Resolve()` の要件

シグネチャは以下とする。

    public EventResult Resolve(GameState state, GameEventCatalogSO catalog)

| # | 処理 |
|---|---|
| 1 | カタログ内の全イベントを走査する |
| 2 | 発火済み（ビットが立っている）ものを除外する |
| 3 | 条件を満たすものを抽出する。`TurnReached` は `CurrentTurn == 発火ターン`、`ParameterBelow` は対象パラメータ `< 閾値` |
| 4 | 該当が複数ある場合、優先度が最大の1件のみを選ぶ。同値の場合はイベントIDが小さい方を選ぶ |
| 5 | 選ばれたイベントの効果を状態に適用し、パラメータを 0〜100 にクランプする |
| 6 | 該当イベントのビットを立てた新しい `GameState` を返す |
| 7 | 該当が0件の場合、状態を変更せず返す |

パラメータの上下限は `GameRulesSO` が持つが、`Game.Features.Event` は
`Game.Features.Command` を参照できない（`CodingSpec` 3節）。
**上下限の値は `GameEventCatalogSO` のフィールドとして別に持たせること。**
ハードコードしてはならない。

`EventResult` は `record` とし、更新後の `GameState`、発火したイベント（なければ
`null`）、発火の有無を示す `bool` を持つ。

### 6-5. カタログの内容（`.asset` として人間が作成する）

エージェントは `.asset` を作成しない。以下は設計の参考であり、実装対象ではない。

| ID | 種別 | 条件 | 優先度 |
|---|---|---|---|
| 0 | TurnReached | ターン 5 | 10 |
| 1 | TurnReached | ターン 10 | 10 |
| 2 | TurnReached | ターン 15 | 10 |
| 3 | TurnReached | ターン 20 | 10 |
| 4 | ParameterBelow | Stamina < 20 | 20 |
| 5 | ParameterBelow | Mental < 20 | 20 |

### 6-6. テスト

`Assets/Tests/EventResolverSOTests.cs` を作成する。

| # | 検証内容 |
|---|---|
| 1 | 条件を満たすイベントが1件のとき、そのイベントが発火する |
| 2 | 条件を満たすイベントが0件のとき、状態が変化しない |
| 3 | 発火済みのイベントは再発火しない |
| 4 | 優先度が異なる2件が同時成立したとき、高い方のみが発火する |
| 5 | 優先度が同値の2件が同時成立したとき、IDが小さい方が発火する |
| 6 | 発火後に `FiredEventMask` の該当ビットが立つ |
| 7 | 効果適用後のパラメータが上限を超えない |
| 8 | 効果適用後のパラメータが 0 を下回らない |
| 9 | 入力の `GameState` が変更されていない（不変性） |

`GameEventSO` と `GameEventCatalogSO` は `CreateInstance` で生成し、テスト用の
ファクトリを `Assets/Tests/` に用意する。既存の `GameRulesSOFactory` に倣うこと。

## 7. Step 6：エンディング判定

### 7-1. 作成するファイル

`EndingKind.cs` は `Assets/Core/Data/` に、それ以外は
`Assets/Features/Ending/Scripts/` に配置する。

| ファイル | 内容 |
|---|---|
| `EndingKind.cs` | `enum { True, Skill, Mental, Stamina }` |
| `EndingRulesSO.cs` | 重み・閾値・同値時の優先順 |
| `EndingResolverSO.cs` | 判定。純粋関数 |

### 7-2. `EndingRulesSO` のフィールド

| フィールド | 既定値 |
|---|---|
| Skill の重み | 2 |
| Mental の重み | 1 |
| Stamina の重み | 1 |
| 最上位エンドの閾値 | 250 |
| 同値時の優先順 | `TrackedParameter` の配列。既定は Skill, Mental, Stamina |

同値時の優先順に `TrackedParameter` を用いるため、この enum は
`Assets/Core/Data/` に配置する。Step 5 の時点で `Game.Core` 側に置くこと。

### 7-3. `EndingResolverSO.Resolve()` の要件

シグネチャは以下とする。

    public EndingKind Resolve(GameState state, EndingRulesSO rules)

| # | 処理 |
|---|---|
| 1 | スコアを計算する。`Skill × 重み + Mental × 重み + Stamina × 重み` |
| 2 | 閾値以上なら `EndingKind.True` を返す |
| 3 | 閾値未満なら、3つのパラメータのうち最大値のものに対応するエンドを返す |
| 4 | 最大値が複数ある場合、`EndingRulesSO` の優先順が先のものを返す |

`TerminationKind.Normal` に到達した場合にのみ呼ばれることを前提とする。
ゲームオーバー時の分岐は本メソッドの責務ではない。

### 7-4. テスト

`Assets/Tests/EndingResolverSOTests.cs` を作成する。

| # | 検証内容 |
|---|---|
| 1 | スコアが閾値ちょうどのとき `True` を返す |
| 2 | スコアが閾値を1下回るとき `True` を返さない |
| 3 | Skill が最大のとき `EndingKind.Skill` を返す |
| 4 | Mental が最大のとき `EndingKind.Mental` を返す |
| 5 | Stamina が最大のとき `EndingKind.Stamina` を返す |
| 6 | Skill と Mental が同値で最大のとき、優先順に従い `Skill` を返す |
| 7 | 重みを変更すると判定結果が変わる（値がハードコードされていないこと） |

## 8. 禁止事項と代替行動

| # | 禁止 | 代替 |
|---|---|---|
| 1 | フォルダの新規作成 | 報告して停止する |
| 2 | `.asmdef` の作成・編集 | 報告して停止する |
| 3 | `.asset` の作成 | 型の定義までを実装し、`.asset` は人間が作る |
| 4 | シェルコマンドによるアセット操作 | Unity エディタ経由。エージェントは行わない |
| 5 | 乱数の使用 | 固定値で実装する |
| 6 | 調整値のハードコード | SO のフィールドから読む |
| 7 | Feature 同士の横方向の依存 | `Game.Core` の型を経由する |
| 8 | `record struct` の使用 | `readonly struct` |
| 9 | ScriptableObject を `record` で定義 | 通常の `class` |
| 10 | `Game.Core` への変更 | 6-3 に記載した `GameState` の1点のみ許可。他は報告して停止 |
| 11 | MonoBehaviour の作成 | 第3週の対象。本書では作らない |
| 12 | 既存テストの変更 | `GameState` へのフィールド追加で既存テストが壊れる場合、報告して指示を仰ぐ |

## 9. 各 Step の完了条件

- Test Runner の EditMode テストが全件通過すること
- コンパイル警告が新規に発生していないこと
- 変更したファイルの一覧を報告すること
- コミットは行わず、報告して停止すること