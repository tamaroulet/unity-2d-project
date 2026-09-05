# 監査レポート #1：総合アーキテクチャ監査およびローグライク育成シミュレーションへの拡張性評価

監査実施日：2026-09-02
監査対象：第3週（Step 1〜Step 9）完了時点のコードベース全体（コミット `2151f02`）
監査手段：リポジトリ全体（`Game/Assets/Core`, `Features/*`, `UI`, `Tests`, `Scenes/MainGame.unity`, `docs/spec/*`）の静的読解。ビルド・実行・Unity Editor 上での検証は行っていない。

対象ファイル数：本文 `.cs` 20件、テスト `.cs` 7件（47テスト）、`.asmdef` 6件、`.asset` 15件、`.unity` 1件。

---

## 総評（先出し）

型フェーズの成果物として、3層分離とデータ駆動の原則は**驚くほど一貫して守られている**。ロジック層に `MonoBehaviour` 依存が一切なく、47件のテストがすべて `ScriptableObject.CreateInstance` のみで完結している時点で、このアーキテクチャは「テストが書けるふりをしている」段階ではなく実際にテスト可能である。

一方で、ローグライク拡張を見据えると、現状のコードは**単発の分岐ロジック（if/switch）の集合**として書かれており、「複数の効果を合成する」「フックポイントに任意個のパッシブを差し込む」という要求に対する拡張点が明示的に用意されていない。特に `GameState.FiredEventMask`（`int` ビットマスク、上限32件）は、レリック・中間イベントを合わせて32種を超えた瞬間に**サイレントに破綻する**設計上の時限爆弾であり、最優先で対処すべき項目である。

---

## 1. アーキテクチャの健全性と設計品質

### 1-1. 3層分離の徹底度 — 評価：良好

| 層 | 実装 | 確認結果 |
|---|---|---|
| ロジック | `GameState`（record, `Core/Data/GameState.cs`）、`TurnRules`（static純粋関数, `Core/Data/TurnRules.cs`）、`CommandResolverSO` / `EventResolverSO` / `EndingResolverSO` | `UnityEngine.Time` / `Random` / `DateTime` / `Debug` への参照が全ロジック層に**皆無**。`Resolve()` 系メソッドはすべて引数から戻り値を計算する純粋関数として実装されている（`Features/Command/Scripts/CommandResolverSO.cs:14-49`, `Features/Event/Scripts/EventResolverSO.cs:15-37`, `Features/Ending/Scripts/EndingResolverSO.cs:15-27`）。 |
| 通信 | `EventChannelSO<T>`（`Core/Events/EventChannelSO.cs`）とその3派生 | `Raise()` / `OnEventRaised` のみの薄い実装。`OnDisable()` で購読を全解除する後始末も実装済み（`Core/Events/EventChannelSO.cs:28-31`）。Publisher/Subscriber間の直接参照は確認できなかった。 |
| ビュー | `StatusView` / `CommandButtonView` / `EventDialogView` / `EndingView` | 4クラスとも「購読 → 表示更新」「クリック → SO呼び出し」のみで、条件分岐や数値計算を一切持たない。規約が求める「薄いMonoBehaviour」を体現している。 |

`GameFlowController`（`Features/GameFlow/Scripts/GameFlowController.cs`）自身も、加算・クランプ・終了判定のロジックを一切持たず、`TurnRules` と各 `*ResolverSO` への委譲のみで構成されている点は、指示書（`CodingSpec.md` §4）の要求を正確に満たしている。

### 1-2. ScriptableObject 純粋関数設計とデータ駆動 — 評価：良好、ただし1件のデータ駆動違反あり

`CommandDataSO` / `GameRulesSO` / `EndingRulesSO` / `GameEventSO` / `GameEventCatalogSO` はいずれも調整値をフィールド化しており、ハードコードされた定数は見当たらない。

ただし1点、ゲームオーバー条件が例外的にハードコードされている。

> `Core/Data/TurnRules.cs:32`
> ```csharp
> if (state.Mental <= 0)
> {
>     return TerminationKind.GameOver;
> }
> ```

「メンタルが0以下でゲームオーバー」という判定対象パラメータと閾値が、`GameRulesSO` 等のSOフィールドではなくコード内リテラルとして固定されている。現状の3パラメータ構成では実害はないが、CodingSpec §6 が定める「調整値のC#側ハードコード禁止」の原則には反しており、後述のローグライク拡張（レリックによる「メンタル0でも1回だけ耐える」等の特殊敗北条件変更）に対しては直接のボトルネックになる。

### 1-3. EventChannelSO による疎結合な通信設計 — 評価：良好

`GameStateEventChannelSO` / `GameEventFiredChannelSO` / `EndingDecidedChannelSO` の3チャンネルとも `EventChannelSO<T>` を継承するのみのボイラープレートで、ジェネリック基底クラス1つに購読解除処理を集約できている点は保守性が高い。`Action<T>` ベースであり `UnityEvent` ではないため、Inspector上の暗黙結線（persistent listener）に頼らずコード上で結線状況を追跡できる点も良い判断である。

**軽微な設計上の隙**：`GameFlowController.BeginTurn()` はイベント発火時に `_currentPhase = GamePhase.ShowingEvent` を設定した直後、同一メソッド内で無条件に `_currentPhase = GamePhase.WaitingInput` へ上書きしている（`Features/GameFlow/Scripts/GameFlowController.cs:107-122`）。

```csharp
private void BeginTurn()
{
    _currentPhase = GamePhase.TurnStart;
    EventResult eventResult = _eventResolver.Resolve(_currentState, _eventCatalog);
    if (eventResult.HasFired)
    {
        _currentState = eventResult.State;
        _currentPhase = GamePhase.ShowingEvent;   // ここで立てても
        _gameStateChannel.Raise(_currentState);
        _eventFiredChannel.Raise(eventResult.FiredEvent.EventId);
    }
    _currentPhase = GamePhase.WaitingInput;        // 数行後に即座に上書きされる
}
```

`GamePhase.ShowingEvent` は外部から一度も観測不可能な状態であり、実質的に到達不能なenum値になっている。かつ `EventDialogView` はパネル表示のみを行い `GamePhase` を一切参照しないため、`CommandButtonView.OnCommandClick()`（`UI/Scripts/CommandButtonView.cs:75-83`）はイベントダイアログ表示中でも `_currentPhase == WaitingInput` の判定を素通りし、`GameFlowController.ExecuteCommand()` を呼び出せてしまう。プレイヤーがイベントダイアログの背後にあるコマンドボタンを誤ってクリックすると、ダイアログの内容を読む前にターンが進行する。現状はUIレイアウト上パネルがボタンを覆っていれば実害が出ない可能性があるが、状態機械としては「イベント表示中は入力を受け付けない」という意図をコードが保証していない。

### 1-4. MonoBehaviour の薄さと責務分離 — 評価：良好

`GameFlowController` はフィールド9個・パブリックメソッド3個の比較的小さいクラスに収まっている。ただし後述（3節）の通り、これは「まだ分岐が3種類（コマンド実行・イベント発火・エンディング確定）しかない」ためであり、フェーズが増えるたびに `AdvanceTurn()` の `switch` 文が線形に肥大化する構造になっている。現時点でクラスが薄いことは「設計が良い」ことの証明ではなく「まだ機能が少ない」ことの証明である点には注意が必要（3節で詳述）。

### 1-5. asmdef の境界と依存関係 — 評価：おおむね良好、ただし規約の記述と実態に乖離あり

```
Game.Core                    → 参照なし
Game.Features.Command        → Game.Core のみ
Game.Features.Event          → Game.Core のみ
Game.Features.Ending         → Game.Core のみ
Game.Features.GameFlow       → Game.Core, Game.Features.Command, Game.Features.Event, Game.Features.Ending
Game.UI                      → Game.Core, 上記4アセンブリすべて
```

`Command` / `Event` / `Ending` の3 Feature は確かに `Game.Core` のみに依存しており、CodingSpec §3 の表（`Game.Features.<機能名>: Game.Core のみ`）に厳密に適合している。

一方 `Game.Features.GameFlow` は3つの Feature に直接依存しており、CodingSpec §3 の表を文字通り適用すると規約違反になる。`GameFlowController` はオーケストレーター（各Resolverを横断的に呼び出す統括役）という性質上、具象型（`CommandResolverSO` 等）への参照が構造的に必要であり、これ自体は妥当な設計判断である。しかし**この例外がCodingSpec上に明文化されていない**ため、次にAIエージェントが新規Featureを追加する際「GameFlowは特別扱いしてよい」という前例を正しく汲み取れる保証がない。指示文ではなくコンパイラによる強制を志向する本プロジェクトの方針（CodingSpec §3 理由欄）と矛盾するため、以下のいずれかで明文化することを推奨する。

- (a) CodingSpec §3 に「オーケストレーション層（GameFlow）は複数Featureへの依存を許可する」という例外規定を追記する。
- (b) `Game.Core` に `ICommandResolver` 等のインターフェースを定義し、`GameFlowController` はインターフェース越しに呼び出す。各 `*ResolverSO` がインターフェースを実装する形にすれば、`Game.Features.GameFlow` は `Game.Core` のみに依存する形に是正でき、表の一般規則を崩さずに済む。将来的にレリック効果注入時にResolverを差し替え可能にする副次的メリットもある。

現状では実害はなく、(a) の追記のみでも十分だが、2節・3節の拡張を見据えるなら (b) の投資対効果が高い。

---

## 2. テストの網羅性と堅牢性

47件のテストは6ファイルに分散しており、内訳は概ね以下の通り（テストメソッド数をカウント）。

| ファイル | 件数 | 対象 |
|---|---|---|
| `CommandResolverSOTests.cs` | 9 | コマンド効果適用・クランプ・ターン進行・終了判定 |
| `EventResolverSOTests.cs` | 9 | イベント発火判定・優先度・重複発火防止・クランプ |
| `EndingResolverSOTests.cs` | 6 | 閾値判定・最大値エンド・同値タイブレーク |
| `GameFlowControllerTests.cs` | 5 | フェーズ遷移・チャンネル発火回数 |
| `UIViewTests.cs` | 9 | 4 View の表示更新・購読解除 |
| `EventChannelSOTests.cs` | 5 | チャンネル基盤（購読・解除・複数購読者） |
| `GameRulesSOTests.cs` | 1 | 初期状態生成 |
| 実測小計 | **44** | |

STATUS.md 記載の「47件」との差（3件）は数え漏れの可能性があるため、正確な件数は `dotnet test` 相当のTest Runner実行結果を正とされたい。件数の正確性そのものは本監査の主眼ではないため参考情報にとどめる。

### 2-1. 設計品質・保守性 — 評価：良好

- 全テストクラスが `[SetUp]`/`[TearDown]` で `ScriptableObject.CreateInstance` 済みインスタンスを `List<Object>` に蓄積し `DestroyImmediate` する明示的な後始末を徹底しており、EditModeテスト特有のリーク・状態汚染を避けている。
- `GameRulesSOFactory` / `GameEventSOFactory` / `EndingRulesSOFactory` によりSOの組み立てをテスト間で共有し、private setter フィールドへの `SetField`（reflection経由）も専用ヘルパーに集約されている。この設計は「フィールド名がリネームされた場合に意味不明なNRE落ちではなく `InvalidOperationException` で即座に検出できる」ようにする配慮が入っており（例：`GameFlowControllerTests.cs:260-271`）、保守性への意識が高い。
- テスト名が `動詞+条件+結果` の文で構成されており（例：`EndsInGameOverAndDoesNotAdvanceTurnWhenMentalReachesZero`）、実行結果だけで仕様書として読める。

### 2-2. 境界値検証 — 評価：良好

`CommandResolverSOTests` は上限クランプ・下限クランプ・スタミナ不足時の拒否・ゲームオーバー優先（`PrioritizesGameOverWhenNormalEndWouldAlsoApply`, 24ターン連続実行のシミュレーションまで、境界条件を丁寧に踏んでいる。`EventResolverSOTests` も優先度同値時のID比較、重複発火防止、上限/下限クランプを個別ケースで検証している。

### 2-3. 検出できなかったギャップ

- **`FiredEventMask` の桁あふれが未テスト**：`EventId` を32以上に設定した場合の挙動（ビットシフトのオーバーフロー、または同一ビットへの衝突）を検証するテストが存在しない。1-2節・3-1節で述べる設計上の時限爆弾が、テストによっても検出されない状態になっている。
- **`GameFlowController` の統合テストがコマンド1手分に限定**：`ExecuteCommand` は個別に1回呼ぶテストのみで、「イベント発火→コマンド実行→次のイベント発火」のような複数ターンにまたがるシーケンス全体を通しで検証するテストがない（`CommandResolverSOTests.EndsNormallyAtTurnTwentyFourAfterTwentyFourConsecutiveRests` はResolver単体に対する多ターンループはあるが、`GameFlowController` 経由のE2E相当は無い）。
- **`ShowingEvent` フェーズ滞留中の入力拒否が未テスト**：1-3節で指摘した「イベント表示中にコマンド実行できてしまう」バグは、そもそも `ShowingEvent` 状態を保持するテストが無いため、テストスイート上は「異常なし」に見える。振る舞いを直すなら先にテストを追加すべき箇所。
- **`CommandButtonView`/`EventDialogView` の「ボタン連打・二重発火」耐性が未検証**：MonoBehaviourのライフサイクルに関わるテストではあるが、`Button.interactable` の制御や多重クリックガードのテストは見当たらない。
- **`EndingRulesSO` の `PriorityOrder` が空配列・null の場合のフォールバック**が未テスト（`EndingResolverSO.SelectByMaxParameter` は `foreach` がゼロ件でも `EndingKind.True` を返すため実害は小さいが、意図した挙動かどうかテストで明示されていない）。

総じて、**単一Resolverの計算ロジックに対する単体テストは非常に手厚い**一方、**複数コンポーネントを跨ぐ状態遷移（フェーズ・ダイアログ・複数ターン）のテストが手薄**という偏りがある。これは典型的な「純粋関数はテストしやすいので厚く、オーケストレーション層は後回しになりがち」というパターンであり、Step 7以降で急速に追加されたGameFlowControllerのテストがまだ育っていないことの裏返しでもある。

---

## 3. ローグライク育成シミュレーションへの拡張性評価

### 3-1. 最優先の構造的ブロッカー：`FiredEventMask` のビットマスク上限

```csharp
// Core/Data/GameState.cs:21
public int FiredEventMask { get; init; }

// Features/Event/Scripts/EventResolverSO.cs:71, :112
return (state.FiredEventMask & (1 << gameEvent.EventId)) != 0;
int firedEventMask = state.FiredEventMask | (1 << gameEvent.EventId);
```

`int` は32ビットのため、`EventId` が0〜31の範囲を超えると `1 << eventId` が未定義動作寄りの挙動（C#では `eventId % 32` として解釈される）になり、**全く無関係な2つのイベントが同じビットを共有して誤判定される**。ローグライク化で「中間試練イベント」「レリック獲得イベント」「ランダムイベント」を積み増していくと、32件は数プレイ分の追加で容易に到達する規模であり、しかもエラーは出ずに黙って誤動作する（CodingSpec §8がまさに警戒している「サイレント障害」と同種の性質）。

**推奨対応**：`FiredEventMask` を `long`（63件まで）に拡張するのは延命に過ぎない。恒久対応としては、`GameState` に `IReadOnlySet<int>` 相当の不変集合（`ImmutableHashSet<int>` 等、`record` との相性がよい不変コレクション）を持たせるか、発火済みIDを可変長のビット配列でラップした専用の値型（`record struct FiredEventSet` で `Contains`/`With` を提供）に置き換えることを推奨する。件数上限による暗黙の破綻を構造的に排除できる。

### 3-2. レリック・パッシブ効果（ターン開始時／コマンド実行時／終了時フック）

現状、フックポイントに相当する箇所は存在しない。`GameFlowController` の3メソッド（`BeginTurn` / `ExecuteCommand` / `AdvanceTurn`）に処理がベタ書きされており、「任意個のレリックを差し込んで、各々が指定タイミングで `GameState` を変換する」という要求には非対応。

**推奨設計アプローチ**：既存の `CommandResolverSO.DetermineEffect()`（`Features/Command/Scripts/CommandResolverSO.cs:54-57`）が「今は恒等関数だが、後から乱数を挿入できるように分離してある」という設計思想をそのまま流用し、パッシブ効果にも同型のパイプラインを導入するのが最も既存資産と親和性が高い。

1. `Game.Core` に `IGameStateTransform`（または `IPassiveEffectSO` 基底クラス）を追加し、`GameState Apply(GameState state, TriggerContext context)` のような単一メソッドを持たせる。`CommandResolverSO` / `EventResolverSO` が既に「純粋関数のSO」として実装されているため、この抽象化は既存コードのリズムを崩さない。
2. `RelicDataSO : ScriptableObject`（レリック1件＝効果1件、`CommandDataSO`/`GameEventSO` と同格の「データ + 参照する効果ロジック」構成）を `Game.Features.Relic` として新設。`Game.Core` のみに依存させ、既存の横依存禁止ルールを守る。
3. `RelicCatalogSO`（`GameEventCatalogSO` と同型のコレクションSO）を `GameFlowController` に持たせ、`BeginTurn()` の先頭・`ExecuteCommand()` の効果適用前後・`AdvanceTurn()` の判定前、の3箇所で `foreach (RelicDataSO relic in catalog.Relics) { state = relic.Apply(state, context); }` の形で畳み込む。
4. `TriggerContext` には「発火タイミング種別（TurnStart/OnCommandExecute/TurnEnd）」「直前に実行されたコマンドの参照」を持たせる。3-1節で述べた通り、現在の `CommandResolverSO.Resolve()` はコマンドの識別子を受け取らず `CommandEffect`（数値のみ）しか見ないため、「特定コマンド使用時のみ発動するレリック」を実現するには **`GameFlowController.ExecuteCommand()` から `CommandDataSO` 自体（またはそのID）をパイプラインに渡す配線変更が必要**になる。現状は `command.Effect` だけを取り出して渡しており、コマンドの identity がResolver層に到達しない設計になっている点は、着手前に直しておくべき既存コードの前提条件である。

この設計であれば、`CommandResolverSO` / `EventResolverSO` 自体は改修不要（純粋関数のまま）で、レリックは「GameStateを受け取ってGameStateを返す」という既存の語彙だけで表現でき、EditModeテストの書き方（`ScriptableObject.CreateInstance` → `Resolve`/`Apply` → `Assert`）もそのまま流用できる。

### 3-3. 中間バトル・試練イベントの組み込み

`GamePhase` enum（`Features/GameFlow/Scripts/GamePhase.cs`）は現在8値の直線的な状態機械であり、`GameFlowController.AdvanceTurn()` の `switch` 文に分岐が追加されるたびに線形に複雑化する構造になっている。バトル・試練を「イベントの一種」として `GameEventSO`／`EventTriggerKind` を拡張するだけで足りるか、独立した `GamePhase.Battle` を新設する必要があるかは、バトルが「即時解決（数値演算のみ）」か「複数ターンにまたがる別ミニゲーム」かで設計が分かれる。

- **即時解決型**（例：試練＝現在のパラメータで成功/失敗判定し、成否に応じて効果を適用）であれば、`EventTriggerKind` に条件分岐後の分岐先を持たせるだけで実装可能。3-4節の分岐イベントと統合できる。
- **複数ターン型の戦闘**（コマンド選択を伴うミニゲーム）であれば、`GamePhase` に `Battle` 系の状態を追加し、`GameFlowController` から `Game.Features.Battle`（新規asmdef、`Game.Core` のみに依存）へ処理を委譲する必要がある。この場合、**現在1クラスに集約されている `GameFlowController` のswitch文が肥大化し「薄いMonoBehaviour」ではなくなるリスクが高い**。3-2節のフック機構と同様に、`GamePhase` ごとの処理を `IGamePhaseHandler`（`Enter`/`Execute`/`Exit`）に切り出し、`GameFlowController` は「現在のHandlerに処理を委譲するだけ」のディスパッチャに縮退させる設計（State パターン）への移行を、バトル導入前に済ませておくことを推奨する。今の switch 文ベースのままバトルを追加すると、`AdvanceTurn()` 1メソッドの責務過多（God Method化）が避けられない。

### 3-4. イベント分岐やランダム性の追加

- **分岐**：`GameEventSO` は現状「発火条件→単一の固定効果」の1対1構造であり、プレイヤー選択による分岐（選択肢A/Bで異なる効果）を表現するフィールドがない。`GameEventSO` に `EventChoiceSO[]`（各選択肢が独自の `CommandEffect` と表示文言を持つ）を追加し、`EventDialogView` に選択肢ボタンを増設する形が既存構造との親和性が高い。`EventResolverSO.Resolve()` 自体は「発火するイベントを選ぶ」役割のままで変更不要、選択後の効果適用だけを新設の `EventChoiceResolverSO` に切り出せば、既存の純粋関数群を汚さずに済む。
- **ランダム性**：CodingSpec §7が明記する通り、型フェーズでは意図的に未実装。`CommandResolverSO.DetermineEffect()` が既に分離済みの拡張点であるため、ここに `IRandomProvider`（`Game.Core` にインターフェースを置き、実装は `System.Random` をラップした薄いクラス）をSOのフィールドとして注入する形が最も既存規約（Time/Random直接参照禁止の思想）に沿う。**乱数シードをGameStateに含めるか外部注入するか**は、リプレイ性（ローグライクの売りである「同じシードで再現できるか」）に直結するため、設計時点で明確に決めておくべき論点として明記しておく。`GameState` は現状 `record` の不変値型であるため、シードを `GameState.RandomSeed` のような1フィールドとして持たせ、消費のたびに `with { RandomSeed = nextSeed }` で更新する設計にすれば、既存の「状態変化は `with` 式で表現する」という規約と矛盾なく統合できる。

---

## 4. 総合スコアと総評

| 評価区分 | 配点 | 得点 | 主な減点理由 |
|---|---|---|---|
| アーキテクチャの健全性と設計品質 | 45 | 39 | asmdef規約とGameFlowの実態乖離が未明文化（-2）／`ShowingEvent`が到達不能で入力ガードが機能しない（-3）／ゲームオーバー閾値のハードコード（-1） |
| テストの網羅性と堅牢性 | 25 | 20 | フェーズ遷移・複数ターンのE2E的検証が手薄（-3）／ビットマスク境界のテスト欠如（-2） |
| ローグライク拡張への構造的準備度 | 30 | 20 | `FiredEventMask` の32件上限が拡張前提を崩す（-5）／レリック・分岐・乱数のフック機構が未着手で、現行 `GameFlowController` はState パターン化前提の下地がない（-5） |
| **合計** | **100** | **79** | |

### 総評

型フェーズの完成度としては**上位クラス**である。3層分離・データ駆動・EventChannelSOによる疎結合・47件の純粋関数テストは、AIエージェントに逐次実装を委譲する開発体制において「レビューコストを大幅に下げる」という当初の設計意図を実際に達成できている。特にResolver系SOが一貫して`static`状態を持たない純粋関数として書かれている点は、今後レリックやランダム性を足していく際の土台として健全である。

一方で、ローグライク拡張は「今のコードに機能を足す」のではなく「今のコードが想定していない繰り返し（多数のイベント/レリック、複数ターンのバトル、分岐、乱数）」を持ち込む作業である。現状のコードは**単発の状態遷移（1コマンド・1イベント・1エンディング）を正しく扱うことに特化**しており、複数の効果を合成する語彙（フック・パイプライン・Stateパターン）がまだ存在しない。特に `FiredEventMask` の上限は「今動いているから大丈夫」に見えて実際は拡張の初期段階で確実に踏み抜く地雷であるため、レリック・追加イベントの実装に着手する**前**に対処することを強く推奨する。

優先順位をつけるなら次の順で着手するのが最も手戻りが少ない。

1. `FiredEventMask` の不変集合化（3-1節）— 以降のイベント追加すべてに影響するため最優先。
2. `CommandResolverSO.Resolve()` にコマンド識別子を渡す配線修正（3-2節）— レリックのコマンド依存条件を実現する前提条件。
3. `GamePhase` のState パターン化（3-3節）— バトル導入前に済ませないと `GameFlowController` がGod Method化する。
4. `ShowingEvent` フェーズの入力ガード修正（1-3節）— 分岐イベントでプレイヤーの選択待ちが増えるほど、この抜け穴の実害が大きくなる。
5. レリック効果パイプライン（`IGameStateTransform` 相当）の新設（3-2節）。

いずれも「既存の純粋関数群を書き直す」のではなく「既存の語彙（SO・EventChannel・record with式）を素直に敷衍する」形で対応可能であり、これまでのStep 1〜9で確立された設計思想を破壊せずに拡張できる見込みである。
