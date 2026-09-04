# 判断 2026-09-04-01

判定者: Claude（Architect）
発行する確定指示書: `docs/instructions/16_bossdialog_binding.md`

## 採用する案

**案1**（`UILayoutBuilder` に `BindBossBattleDialogSceneReferences()` を追加）を採用する。
案2 と案3 は却下する。

## 裏取り

`02-context.md` の主張を独立に検証した。**案A の診断は正しい。**

```
UILayoutBuilder:298      BossBattleDialogPanel と BossBattleDialogView を生成している
UILayoutBuilder:581-582  BindStatusViewSceneReferences / BindRelicDraftDialogSceneReferences
                         → BossBattleDialog 用の結線メソッドが存在しない
BossBattleDialogView     SerializeField 8 個（実測で全て null）
```

生成はしているが結線していない。`StatusView._bossBattleDialog` は正しく張られている
（`02-context.md` の実測どおり）ため、連鎖はこうなる。

```
GameFlowController → OnBossBattleOccurred → StatusView → BossBattleDialogView.Show(proceedToDraft)
   → ダイアログは表示されるが _dismissButton が null
   → proceedToDraft が呼ばれない
   → ターン 6 から進めない
```

## 却下の理由

**案2（`BossSceneBinder.cs` を修正・再実行）**
`BossSceneBinder` は `GameFlowController` の `_bossCatalog` / `_autoBattleResolver` /
`_bossBattleTurns` を張るもので、対象が「コントローラ」である。
今回必要なのは「ビューの内部要素」の結線であり、関心が違う。
`UILayoutBuilder` が既に `StatusView` と `RelicDraftDialogView` を同じ形で結線しているので、
そこに揃えるほうが対称で、次に誰が読んでも迷わない。

**案3（ボスダイアログも UI Toolkit 化を前倒し）**
UI Toolkit パイロット（`EventDialogViewUI`）は**まだ画面で確認されていない**。
結線とテストは通っているが、実際に表示されるかは誰も見ていない。
未検証の土台の上に 2 枚目を積むのは、本日 3 回繰り返した失敗
（「やった」と「効いた」を区別しない）と同じ形になる。
パイロットが画面で確認できてから横展開する。

## 併せて指示すること

同じ種類の不具合が本日 3 回出ている。

| # | 対象 | 症状 |
|---|---|---|
| 1 | `EventDialogView` | 4 フィールド未結線 |
| 2 | `RelicDraftDialogView._relicAcquiredChannel` | パス不一致で null |
| 3 | `BossBattleDialogView` | 8 フィールド未結線 |

シーン全体では **28 箇所**が未結線のまま残っている。
3 件目を手で直しても 4 件目が出る。**機械的に一覧できる手段を同時に作る。**

### 重要な注意

**24 ターン完走の EditMode テストでは、この種の不具合は捕まえられない。**
EditMode にはシーンが無いため `OnBossBattleOccurred` に購読者がおらず、
`GameFlowController` のフォールバックが働いて先へ進んでしまう。
**論理は緑なのに実機は詰む**という、最も危険な形の偽陰性になる。

必要なのは**シーン上の `[SerializeField]` を走査する検査**であり、
プレイスルーのシミュレーションではない。

## 受け入れ条件

修正の成否は「結線されたこと」ではなく、**ターン 6 を越えられること**で判定する。
`dotnet build` と既存テストが緑になっただけでは完了としない。
