# 実装指示書 #2 — 第1週：GameState とコマンド解決

> **注記（2026-09-02 追記）**
>
> 本書は 2026-09-02 にチャット履歴から復元したものであり、2026-09-01 に
> Claude Code へ投入された原本の逐語コピーではありません。
>
> - 第0・1・2節および第6節の各 Step 冒頭の「前提」は履歴に残る逐語です
> - 第3-5節以降と第4・5・7〜9節は逐語が失われており、確定済みの設計判断と
>   実装結果から再構成したものです
> - 第6節冒頭は、初回投入時に `AGENTS.md` 第1節との矛盾が検出されたため
>   差し替えた後の版です
> - `CodingSpec` 15節（SPDX-AI-Disclosure）は本書の投入後に制定されたため、
>   本書には含まれません
>
> 投入時の実際のやり取り、差し戻し内容、テスト結果は `docs/log.md` の
> 第1週の記録を参照してください。

## 0. あなたの役割と絶対条件

あなたは Rider 上で動作する C# 実装エージェントです。以下は例外なく適用されます。

- **書き込んでよいのは `.cs` ファイルのみ。**`.asmdef`、`.asset`、`.meta`、`.unity`、
  `.prefab`、プロジェクト設定ファイルを作成・変更・削除してはいけません
- **フォルダを新規作成してはいけません。**指定された既存フォルダにのみファイルを置きます
- **シェルコマンドによるファイル操作（`mv` / `rm` / `cp` / `mkdir`）を禁止します**
- **Step の途中で次の Step に進んではいけません。**各 Step の完了報告を出したら停止し、
  人間の指示を待ちます
- 「計画を提示します」と述べた場合、その応答内では実装に着手しません。
  計画のみを出力して停止します
- 禁止された操作が必要になった場合、代替手段を選ばず停止して報告します

`Game/AGENTS.md`、`docs/spec/CodingSpec.md`、`docs/spec/BuildSpec.md` を
**作業開始前に必ず読んでください。**本指示書と規約が矛盾する場合は、
実装せずに矛盾点を報告してください。

## 1. 今回のスコープ

### やること

ターン制育成シミュレーションの、ロジック層のみを実装します。

1. `GameState`（`record` 型の不変な実行時状態）
2. ターン進行と終了判定の純粋関数
3. コマンドの効果値を保持する ScriptableObject
4. コマンドを状態に適用する純粋関数としての ScriptableObject
5. 上記すべてに対する EditMode テスト

### やらないこと（実装したら不合格）

| 項目 | 理由 |
|---|---|
| MonoBehaviour の作成 | 第3週の範囲 |
| シーン・プレハブへの言及や変更 | ビュー層は第3週 |
| `EventChannelSO` | 第2週の範囲 |
| イベント発火条件・エンディング分岐 | 第2週の範囲 |
| 乱数・確率的要素 | 型フェーズでは実装しない（`CodingSpec` 7） |
| `async` / `await` / UniTask | 今回の対象は同期的な純粋関数のみ |
| PrimeTween | 演出は第3週 |
| UI・テキスト表示・ログ出力 | ロジック層は `Debug.Log` を含め出力を持ちません |

## 2. 配置

**作業ディレクトリは `Game/` です。**以下のパスはリポジトリルートからの
表記であり、`Game/` を起点とする場合は先頭の `Game/` を除いて解釈してください。

```
Game/Assets/Core/Data/                 … GameState、CommandEffect、TurnRules
Game/Assets/Features/Command/Scripts/  … CommandDataSO、CommandResolverSO
Game/Assets/Tests/EditMode/            … テストコード
```

- `Features/GameState/` と `Features/Turn/` は**作成しません。**
  `GameState` とターン進行は全 Feature から参照されるため `Game.Core` に置きます。
  Feature 同士の横依存を禁止する `CodingSpec` 3 を満たすための配置です
- `Features/Command/Instances/` に `.asset` を作る作業は人間が行います。
  あなたは `.asset` を作成しません
- 名前空間は `Game.Core`、`Game.Features.Command`、`Game.Tests.EditMode` とします

## 3. 実装する仕様

以下の数値は**仮の固定値**です。すべて SO のフィールドから読み、C# 側に定数として
書き込まないでください（`CodingSpec` 6）。ただしパラメータの上下限とターン上限は
ルール定義として `GameRulesSO` に持たせます。

### 3-1. パラメータ

| 名称 | 範囲 | 初期値 |
|---|---|---|
| `Stamina` | 0〜100 | 100 |
| `Skill` | 0〜100 | 0 |
| `Mental` | 0〜100 | 50 |

すべて `int`。加減算の結果は範囲外に出た時点で上下限に丸めます（クランプ）。

### 3-2. ターン

| 項目 | 値 |
|---|---|
| 開始ターン | 1 |
| 最大ターン | 24 |

### 3-3. コマンド（4種）

効果値は `CommandDataSO` のフィールドです。以下は初期投入値の目安です。

| コマンド | Stamina | Skill | Mental |
|---|---|---|---|
| Train | -20 | +8 | -5 |
| Study | -10 | +4 | -10 |
| Rest | +30 | 0 | +10 |
| Play | -5 | 0 | +15 |

### 3-4. 実行可否

`Stamina` が消費量に満たない場合、そのコマンドは**実行できません。**
実行不可のコマンドが指定された場合、状態を一切変更せず、実行不可であることを
呼び出し元に返します。ターンは進みません。

### 3-5. 判定順序

`Resolve()` は以下の順序で処理します。順序を変更しないでください。

1. 実行可否を判定する。不可なら状態を変更せず返す
2. コマンドの効果を適用し、クランプする
3. **ゲームオーバー判定を行う。**成立した場合、ターンを加算せずに返す
4. ターンを加算する
5. 通常終了判定を行う

| 終了種別 | 条件 |
|---|---|
| ゲームオーバー | `Stamina` または `Mental` が 0 以下 |
| 通常終了 | ターン加算後に最大ターンを超えた |
| 継続 | 上記いずれにも該当しない |

**ゲームオーバーは通常終了より優先されます。**両方が同時に成立し得る場面で、
ターンが加算されないことが判定順序の証拠になります。

### 3-6. 消費と増減の分離

`CommandEffect` は、パラメータの増減量と `Stamina` の消費量を別のフィールドとして
持ちます。Rest は `Stamina` が +30 かつ消費 0 であり、増減量から消費量を導出できません。

## 4. 設計方針

### 4-1. 純粋関数

`TurnRules` と `CommandResolverSO` の公開メソッドは純粋関数として実装します。
入力は引数のみ、出力は戻り値のみとし、自身のフィールドを書き換えません。
`Time` / `Application` / `Debug` / `Random` を含む Unity のランタイム API に
依存しません。

### 4-2. テスト可能性のための値型分離

`CommandResolverSO.Resolve()` は `CommandDataSO` ではなく、`Game.Core` の
プレーンな値型 `CommandEffect` を引数に取ります。

```
CommandResult Resolve(GameState state, CommandEffect effect, GameRulesSO rules)
```

テストは `CommandEffect` を直接構築できるため、`private` フィールドへの
リフレクション注入なしに検証できます。`CommandDataSO` は自身のフィールドから
`CommandEffect` を組み立てて渡すだけの層になります。

### 4-3. 乱数の後入れ構造

効果値の決定は `DetermineEffect()` として分離します。型フェーズでは
引数をそのまま返す恒等関数です。確率的要素を導入する場合、この関数の内部のみを
差し替えれば済む構造を維持してください（`CodingSpec` 7）。

## 5. 型の制約

| 用途 | 使用する型 |
|---|---|
| 実行時の状態（`GameState`） | `record`（`record class`） |
| ScriptableObject のクラス定義 | 通常の `class`。`record` は禁止 |
| 戻り値の複合型（`CommandResult`） | `record`（`record class`） |
| 値型（`CommandEffect`） | `readonly struct`。`record struct` は C# 10 機能のため使用不可 |
| 終了種別（`TerminationKind`） | `enum` |

`init` セッターに必要な `IsExternalInit` は `Game.Core` に `public` で宣言済みです。

`GameState` に終了判定の結果を持たせないでください。導出可能な情報を状態に
混ぜると、状態の復元時に判定結果と実データが食い違う余地が生まれます。
終了種別は `CommandResult` の一部として返します。

## 6. 実装ステップ

各 Step は `AGENTS.md` 第1節の手順に従います。すなわち、指示を受けたら
まず変更計画を提示して停止し、人間の承認を得てから実装に着手します。
本指示書は作業依頼であり、事前承認ではありません。

**実装完了時にも必ず停止してください。**Step をまたいで作業を続けないでください。

### Step 1 — Core の型定義

**前提**: `Game/Assets/Core/Data/` フォルダは作成済みです。存在しない場合は
作成せず、その事実を報告して停止してください。

`Game/Assets/Core/Data/` に以下を作成します。

| ファイル | 内容 |
|---|---|
| `GameState.cs` | `record`。`CurrentTurn` / `Stamina` / `Skill` / `Mental`（すべて `int`、`init`） |
| `CommandEffect.cs` | `readonly struct`。`StaminaDelta` / `SkillDelta` / `MentalDelta` / `StaminaCost` |
| `TerminationKind.cs` | `enum`。継続 / ゲームオーバー / 通常終了 |
| `TurnRules.cs` | `static class`。ターン加算と終了判定の純粋関数 |

### Step 2 — Command の実装

**前提**: `Game/Assets/Features/Command/Scripts/` フォルダと
`Game.Features.Command` の asmdef は作成済みです。いずれかが存在しない場合は
作成せず、その事実を報告して停止してください。

| ファイル | 内容 |
|---|---|
| `GameRulesSO.cs` | パラメータの上下限、初期値、開始ターン、最大ターン |
| `CommandDataSO.cs` | コマンド1件の効果値 |
| `CommandResult.cs` | `record`。適用後の `GameState`、実行可否、終了種別 |
| `CommandResolverSO.cs` | `Resolve()` / `DetermineEffect()`。`[SerializeField]` を持たない |

`[CreateAssetMenu]` を `GameRulesSO` と `CommandDataSO` に付けてください。
`.asset` の生成は人間が行います。

> **投入後の追加指示（記録）**: `GameRulesSO` に `CreateInitialState()` を
> 追加しました。初期状態の組み立て場所が未定義だと、第3週にビュー層へ
> 漏れるためです。原本にはこの記述がありません。

### Step 3 — テスト

**前提**: `Game/Assets/Tests/EditMode/` フォルダと `Game.Tests.EditMode` の
asmdef は作成済みです。存在しない場合は作成せず、その事実を報告して
停止してください。

以下の振る舞いを検証してください。

| # | 検証内容 |
|---|---|
| 1 | ターンが加算される |
| 2 | パラメータが効果値どおりに増減する |
| 3 | 適用後に元の `GameState` が変更されていない（`record` の不変性） |
| 4 | 上下限でクランプされる |
| 5 | `Stamina` 不足時にコマンドが実行されず、状態もターンも変化しない |
| 6 | ゲームオーバーが判定される |
| 7 | 通常終了が判定される |
| 8 | ゲームオーバーが通常終了より優先され、ターンが加算されない |

`ScriptableObject.CreateInstance<T>()` でインスタンスを生成します。`.asset` に
依存しないでください。`[SerializeField] private` フィールドへの値注入に
リフレクションを用いる場合、失敗時にフィールド名を含む例外を投げてください。
実装の不具合とテストハーネスの不備を切り分けるためです。

手組みの `GameState` は全フィールドを明示的に初期化してください。未指定が 0 に
なると、意図しないゲームオーバーが成立してテストが誤って通ります。

## 7. 完了報告の形式

各 Step の完了時に、以下を出力して停止してください。

- 作成したファイルの完全パス
- 使用モデル
- 規約と衝突した点。なければ「なし」
- 人間の判断が必要な点。なければ「なし」
- 自己修正した回数とその内容

## 8. 禁止事項

| # | 禁止事項 |
|---|---|
| 1 | `.cs` 以外のファイルの作成・変更・削除 |
| 2 | フォルダの新規作成 |
| 3 | シェルコマンドによるファイル操作 |
| 4 | 計画を提示した応答内での実装着手 |
| 5 | Step をまたいだ連続実行 |
| 6 | 調整値・上下限・ターン上限の C# 側へのハードコード |
| 7 | 乱数の使用 |
| 8 | `Debug.Log` を含むログ出力 |
| 9 | `MonoBehaviour` の作成 |

## 9. 判断に迷った場合

自分で回避策を選ばず、停止して報告してください。禁止された操作が必要になった場合、
指示書と規約が矛盾する場合、前提となるフォルダや asmdef が存在しない場合が
これに該当します。
