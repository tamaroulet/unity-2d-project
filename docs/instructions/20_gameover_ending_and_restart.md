# 指示書 20: 敗北時（GameOver）のエンディング表示と周回復帰

```
ROLE: Executor
BRANCH: main
判断: docs/handoff/2026-09-05-01/03-instruction.md
前提: 指示書 19 完了済み（コミット 88b9490）。周回ループ（Raise → EndingView → RequestRestart
      → MetaShopDialogView → StartGame）は勝利側で動作している。今回はこれを敗北側にも通す。
```

## 目的

人間の報告は **「Defeat したあとフリーズする」**。

裏取りした結果、原因は次の 2 箇所。**勝利時だけ `_endingDecidedChannel.Raise` を呼んでおり、
敗北時（GameOver）は 2 経路とも呼んでいない。**

```
GameFlowController.cs:223-226  ステータス枯渇（Mental <= 0）による GameOver
GameFlowController.cs:329-344  ボス戦敗北による GameOver
```

`_currentPhase = GamePhase.GameOver` を代入した後、画面遷移が一切起きないため、
以後のコマンドボタンはすべて `ExecuteCommand` の `WaitingInput` ガードで拒絶され続ける。
これが「フリーズ」の正体である。

`EndingView` / `RequestRestart()` / `MetaShopDialogView` はいずれもフェーズを判定しておらず、
敗北時にもそのまま使い回せる（`03-instruction.md` の裏取り参照）。
**新しい View やチャンネルは作らない。** `EndingKind` に 1 値足し、2 箇所で `Raise` を呼ぶだけで直る。

---

## 1. 実装方針（この形で直すこと。自分で別案に変えないこと）

### A. `EndingKind` — `Defeat` を追加する

`Game/Assets/Core/Data/EndingKind.cs`

```csharp
public enum EndingKind
{
    True,
    Skill,
    Mental,
    Stamina,
    Defeat
}
```

既存 4 値の並び・値は変更しない（`Defeat` を末尾に追加するのみ。シリアライズ済みの
`EndingLabel[]` 等が既存値でインデックスを持っていた場合の破壊を避ける）。

### B. `GameFlowController` — 敗北時にも Raise する

`Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`

1. `AdvanceTurn()` の `TerminationKind.GameOver` 分岐（`:223-226`）を次の形にする。
   勝利時分岐（`:227-234`）と対称に、**Raise → phase 設定 → FinalizeRun** の順を揃える。

```csharp
case TerminationKind.GameOver:
    _endingDecidedChannel?.Raise(EndingKind.Defeat);
    _currentPhase = GamePhase.GameOver;
    FinalizeRun(isGameClear: false);
    break;
```

2. `BeginTurn()` のボス戦敗北分岐（`:329-344`）。**ここは Raise のタイミングに注意する。**
   ボスバトルダイアログ（`BossBattleDialogView`）が先に閉じてから `EndingView` を開く必要がある
   （でないと 2 枚のモーダルが重なる）。`OnBossBattleOccurred` の購読者がいる場合は
   ダイアログの dismiss コールバック内で、いない場合（テスト等のフォールバック）は即座に Raise する。

```csharp
else
{
    _currentPhase = GamePhase.GameOver;
    if (OnBossBattleOccurred != null)
    {
        OnBossBattleOccurred.Invoke(boss, battleResult, () =>
        {
            _endingDecidedChannel?.Raise(EndingKind.Defeat);
            FinalizeRun(isGameClear: false);
        });
    }
    else
    {
        _endingDecidedChannel?.Raise(EndingKind.Defeat);
        FinalizeRun(isGameClear: false);
    }
    return;
}
```

`FinalizeRun` 自体は変更しない。`isGameClear: false` でも `turnBonus` / `skillBonus` /
`bossBonus` は加算される（`clearBonus` のみ 0）ため、敗北後もショップを開く意味がある。

### C. `EndingView` — 敗北時の表示名を足す

`Game/Assets/UI/Scripts/EndingView.cs`

`ResolveDisplayName`（`:144-165`）の switch に `Defeat` を追加する。

```csharp
return ending switch
{
    EndingKind.True => "TRUE ENDING",
    EndingKind.Skill => "SKILL MASTER ENDING",
    EndingKind.Mental => "MENTAL FORTITUDE ENDING",
    EndingKind.Stamina => "IRON VITALITY ENDING",
    EndingKind.Defeat => "GAME OVER",
    _ => "GAME CLEAR"
};
```

`_endingLabels`（Inspector 上書き用の配列）は現状シーン側で 1 件も設定されていない
（`UILayoutBuilder.cs` に該当箇所なし）ため、上のフォールバック switch だけで表示は決まる。
シーン側の変更は不要。

**他のフィールド・メソッドは変更しない。** `OnRestartClicked` / `Bind` / `IsPanelActive` /
`DisplayedResult` はフェーズ非依存のまま、敗北時もそのまま動く。

### D. `MetaShopDialogView` / `StatusView` — 変更なし

`OnMetaShopRequested` / `OnMetaProfileChanged` の購読・表示ロジックは
`isGameClear` を一切参照していない。**触らない。**

---

## 2. やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/instructions/*.md（本書以外）  README.md  docs/spec/**  docs/decisions/**
.agents/rules/00_rules.md  .claude/hooks/guard.js  .github/workflows/**
scripts/handoff.ps1  scripts/nightly_gate.py  scripts/auto_runner.py
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。今回はシーン変更は不要（§1-C）。

- **新規 `GameOverView` コンポーネント、新規 `EventChannelSO` を作らない**（`03-instruction.md` Q1 裁定）
- ショップの内部 ID 表示（`Unlock_Stat_Stamina_01` 等）の修正は**本サイクルに含めない**。
  指示書 21 で扱う（`03-instruction.md` Q2 裁定）
- 指示書 19 §5 で分離した「`_metaProfile` の永続化」「`ResolveUnlockedRelics` のドラフト反映」も
  引き続き対象外。これらは指示書 21 にまとめる
- `EndingKind` 内 4 値（`True`/`Skill`/`Mental`/`Stamina`）の並び・意味は変更しない
- テストを通すためにプロダクションコードへ分岐や自己修復を足さない

---

## 3. 検証

**テストが緑になっただけでは完了としない。実際に負けてフリーズしないことで判定する。**

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー |
| 2 | `Tools/Report Non-ASCII Player-Facing Text` | **0 件**（"GAME OVER" は ASCII） |
| 3 | `Tools/Report Unbound Serialized Fields` | 実行前と同じか改善。件数を報告する |
| 4 | EditMode / PlayMode テスト | 実行前と同じか改善。件数を明記 |
| 5 | **ステータス枯渇での敗北**（Mental を 0 まで下げる操作をマウスで行う） | リザルト画面が表示され `GAME OVER` と出ること。フリーズしないこと |
| 6 | 上記からの **RESTART クリック** | エンディングが閉じ、**ショップが開く**こと（`POINTS:` に 0 でない値） |
| 7 | ショップの `CLOSE / NEXT RUN` | ターン 1 の入力待ちへ戻り、コマンドボタンが押せること |
| 8 | **ボス戦敗北での GameOver**（弱い状態でボス戦に突入させる） | ボスダイアログが閉じた後にリザルト画面が表示され、5〜7 と同じ流れで復帰できること |
| 9 | 5〜8 の後、**通常クリア側（ターン24到達）が壊れていないこと** | `TRUE ENDING` 等、既存 4 種のいずれかが表示されショップに繋がること |
| 10 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 5・6・8・9 は **実際のマウス操作**で確認すること。Unity を開いて Play するか、
Unity-MCP で操作するか、**どちらを使ったか明記する。**

> **注意**: `ExecuteEvents.Execute` を使った PlayMode テストだけでは、敗北経路の Raise
> 呼び出し漏れのような「呼ばれないコールバック」型の不具合は緑のまま見逃せてしまう場合がある
> （指示書 19 の同種注意を参照）。テストが緑でも §5・§8 のマウス確認は省略しないこと。

EditMode か PlayMode のテストに、敗北時のアサーションを 1 件追加すること
（`Game/Assets/Tests/` 配下。既存の `MainGame_AdvanceToTurn24_...` テストと同じスタイルで、
Mental を 0 まで枯らすか `TurnRules.EvaluateTermination` を直接使うなど、
決定的に GameOver を再現できる方法を選んでよい）。
最低限、`_endingDecidedChannel` が `EndingKind.Defeat` で 1 回だけ Raise されることと、
`GamePhase.GameOver` に到達することをアサートする。

---

## 4. 停止条件

- 1 タスクで 150 行を超える変更、または 2 コミットに到達した
- 同じ修正を 2 回試して直らない
- `.unity` / `.asset` を直接編集しないと進めなくなった
- ボスダイアログを閉じた直後に `EndingView` と `BossBattleDialogView` が同時に画面に残る
  （dismiss コールバックの順序を疑うこと。§1-B の形を離れない）
- 新しい `EventChannelSO` や新規 View コンポーネントを作らないと実装できないと判断した
  （**その時点で手を止めて人間に報告する**）
- 保護対象ファイルの変更が必要になった

---

## 5. 報告

`docs/handoff/2026-09-05-01/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/instructions/20_gameover_ending_and_restart.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## 検証
（上の 1〜10 の生出力を全部。特に 5・6・8・9 は Console のログをそのまま）

## 停止条件への抵触
なし / あり（内容）
```

完了したタスクは、**その場で `- [x]` に更新してからコミットする**（規約 §0）。
コミットのみ行い、push はしない。

---

## 6. 次サイクル（指示書 21）へ送るもの

- ショップの内部 ID 表示（`Unlock_Stat_Stamina_01` 等）を Editor スクリプト経由で改名する
  （`03-instruction.md` Q2）
- `_metaProfile` の永続化（PlayerPrefs 等）（指示書 19 §5-1 から持ち越し）
- `ResolveUnlockedRelics` のドラフト反映（指示書 19 §5-2 から持ち越し）
