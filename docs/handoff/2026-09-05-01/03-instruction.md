# 判断 2026-09-05-01

判定者: Claude（Architect）
発行する確定指示書: `docs/instructions/20_gameover_ending_and_restart.md`

## 採用する案

**Q1: 案 A**（`EndingKind.Defeat` を追加して `EndingView` を敗北時にも使い回す）を採用する。案 B（専用 `GameOverView` の新設）は却下する。

**Q2: 後回し**（本サイクルには含めない）。ショップの内部 ID 表示は指示書 21 へ回す。

## 裏取り

`02-context.md` の主張を独立に検証した。**診断・A案の両方とも正しい。**

### 確認できたこと（フリーズの原因）

```
GameFlowController.cs:223-226  ステータス枯渇時の GameOver
  _currentPhase = GamePhase.GameOver;
  FinalizeRun(isGameClear: false);
  → _endingDecidedChannel.Raise は呼ばれない

GameFlowController.cs:329-344  ボス戦敗北時の GameOver
  _currentPhase = GamePhase.GameOver;
  OnBossBattleOccurred?.Invoke(boss, battleResult, () => FinalizeRun(isGameClear: false));
  → ダイアログを閉じた後のコールバックも FinalizeRun のみ。Raise なし

TurnRules.cs:32-35  GameOver の発火条件は Mental <= 0 のみ（枯渇1系統）
```

**勝利時（`NormalEnd` / 最終ボス撃破）だけが `_endingDecidedChannel.Raise(ending)` を呼んでおり、
2 つの GameOver 経路はどちらも呼んでいない。** これはロジックの誤りではなく、
2026-09-04-02 の `03-instruction.md`「地雷C（GameOver UI）について」で
**意図的に対象外とされた未実装機能**である（「敗北時 UI は…別サイクルで扱う」）。
今回はそのサイクルに当たる。

### `EndingView` が敗北時にもそのまま使える

```
EndingView.cs:127-138  OnEndingDecided(EndingKind ending) はフェーズを見ない。
                        Raise さえ来れば無条件でパネルを開く
EndingView.cs:144-165  ResolveDisplayName の switch は EndingKind の 4 値のみを持つ。
                        Defeat が来ると既定 "GAME CLEAR" にフォールバックする（誤表示）
EndingView.cs:77-88    OnRestartClicked は _gameFlowController.RequestRestart() を呼ぶだけ。
                        GameClear/GameOver のどちらから来たかを判定していない
GameFlowController.cs:120-128  RequestRestart() も同様にフェーズ非依存
MetaShopDialogView.cs / StatusView.cs  isGameClear・GameOver への参照は 1 箇所も無い
MetaPointResolverSO.cs:54-67  CalculateEarnedPoints は isGameClear=false でも
                        turnBonus/skillBonus/bossBonus は加算する（clearBonus のみ 0）。
                        敗北してもポイントは入るため、ショップを開く意味がある
```

**指示書 19 で作った「Raise → パネル表示 → RequestRestart → ショップ → 次周回」の配線は
フェーズに依存していない。** 敗北時に足りないのは配線ではなく、**2 箇所の `Raise` 呼び出しと
`EndingKind` に 1 値を足すことだけ**である。

### 案 B（専用 GameOverView）を却下する理由

指示書 19 の §1 で `EndingView` 系 4 View はすでに
「Controller が素の C# イベントを公開し、View が `[SerializeField]` で掴んで購読する」という
統一された作法に揃えられている。専用 `GameOverView` を新設すると、この配線・購読・
ショップ連携（`OnMetaShopRequested` → `RequestRestart`）を丸ごと複製することになり、
コード量は A 案の約 10 倍になる見込みでありながら、A 案が持たない利点が無い。
`00_rules.md` は新規コンポーネントの追加自体を禁じてはいないが、
「同じ結果を最小差分で得られる既存の型がある」場合にそれを使わない理由がない。

### Q2（ショップの内部 ID 表示）を本サイクルに含めない理由

```
Instances/Unlock_Stat_Stamina_01.asset:16  _unlockName: Unlock_Stat_Stamina_01
Instances/Unlock_Stat_Skill_01.asset       同様に内部 ID がそのまま _unlockName
Instances/Unlock_Stat_Mental_01.asset      同様
MetaShopDialogView.cs:133                  _itemNameTexts[i].text = unlock.UnlockName;
```

事象自体は実在するが、**フリーズ（進行不能）とは無関係の表示品質の問題**であり、
修正は `.asset` の `_unlockName` を書き換える Editor スクリプトという、
今回の C# ロジック変更とは性質の異なる作業になる。指示書 19 の §5 が
「持続化」「ドラフト反映」の 2 件をすでに指示書 20 送りにしており、
今回はその指示書 20 の枠を緊急のフリーズ修正に差し替える形になる。
1 サイクル 1 主題の原則（`00_rules.md` の役割分担、指示書 19 §末尾の停止条件）を保つため、
Q2 と指示書 19 §5 の 2 件（永続化・ドラフト反映）はまとめて **指示書 21** に送る。

## 受け入れ条件

**テストが緑になったことでは判定しない。実際に負けて（Mental を 0 まで枯らすか、
ボス戦で敗北するまでプレイして）画面がフリーズせず、リザルト→リスタート→ショップ→
ターン1 まで戻ることで判定する。** 指示書 19 の検証注意（`ExecuteEvents` だけでは
非アクティブ購読の罠を再現しない）が今回も有効。
