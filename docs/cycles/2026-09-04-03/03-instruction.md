# 判断 2026-09-04-03

判定者: Claude（Architect）
発行する確定指示書: `docs/cycles/2026-09-04-03/18_boss_overlay_and_english_text.md`

## 採用する案

- 課題 1（クリック不達 / Raycast 遮蔽）: **案 1-B を採用**する。1-A は却下。
- 課題 2（文字化け）: **案 2-A を採用**する。ただし **`Tools/Generate Relic Assets` の再実行は禁止**。
  理由は下の「2-A の実装で 1 点だけ変える」を見ること。

## 裏取り

`02-context.md` の主張をコードから独立に検証した。**2 つとも当たっている。**
そのうえで、案の選択を変える事実が 2 つ出た。

### 課題 1 の見立ては正しい（4 点すべて確認）

```
UILayoutBuilder.cs:105-106  SetupRelicDraftDialogPanel → SetupBossBattleDialogPanel の順で生成
                            → BossBattleDialogPanel が後発 sibling ＝ 手前に描画される
UILayoutBuilder.cs:363-374  ConfigureModalPanel がパネル直下に全画面 Image（ColorOverlay）を貼る
                            raycastTarget は既定 true のまま。CreateOrUpdateBackground(140行) は
                            同じ全画面 Image に raycastTarget = false を明示している ← 差が出ている
UILayoutBuilder.cs:607      BossBattleDialogView._panelRoot ← BossBattleDialogPanel/PanelRoot（子）
UILayoutBuilder.cs:706      RelicDraftDialogView._panelRoot ← RelicDraftDialogPanel（パネル自身）
UILayoutBuilder.cs:761      EndingView._panelRoot          ← EndingPanel（パネル自身）
BossBattleDialogView.cs:99  Show() が gameObject.SetActive(true)
BossBattleDialogView.cs:183 Hide() は _panelRoot.SetActive(false) のみ。gameObject は落ちない
```

**ボスダイアログだけが「子を閉じる」形になっている。** 前サイクルで EndingView /
RelicDraftDialogView を「パネル自身を閉じる」形に揃えたときに、ボスだけ取り残された。
結果、ボス戦後は `BossBattleDialogPanel`（＝透明な全画面 Raycast ブロッカー）が
`RelicDraftDialogPanel` の手前に居座り続ける。**進行不能は再現するべくして再現している。**

### これは指示書 17 が名指しで禁じた形である

`docs/cycles/2026-09-04-02/17_ending_and_draft_binding.md` の「やってはいけない代替」:

> パネルをアクティブのままにする → `ConfigureModalPanel` の全画面オーバーレイ Image が
> 常時画面を覆い、コマンドボタンのクリックも奪う。View 側に消す手段が無い

**すでに文書化された地雷を、ボスパネルだけが踏み続けている。**
案 1-A（`Hide()` で `gameObject.SetActive(false)` も呼ぶ）は症状を止めるが、
「View が自分の乗っている GameObject を落とす」という、17 が (b) として却下した形に近づく。
`IsPanelActive` は `_panelRoot.activeSelf` を見るので、gameObject だけ落としても
**閉じているのに IsVisible が true を返す**（子の activeSelf は変わらない）。
テストと画面が食い違う状態を新しく作ることになる。**1-A は却下する。**

案 1-B なら、4 つのモーダルすべてが 1 つの形に揃い、オーバーレイの地雷が残る面が無くなる。

### 1-B に付随して直す必要があるもの（02-context.md に無い）

移設だけでは壊れる箇所が 2 つある。**見落とすと直後に別の不具合になる。**

```
StatusView.cs:57-60          _bossBattleDialog の直参照で Show() を呼んでいる。
                             コンポーネントを UIViews へ移すとこの参照が切れる
UILayoutBuilder.cs:675-680   その参照を BossBattleDialogPanel から拾って張っている
BossSceneBinder.cs:67,82     Tools/Bind Boss to MainGame Scene が
                             BossBattleDialogPanel に View を AddComponent し直し、
                             _panelRoot に PanelRoot（子）を張り直す
                             → 放置すると、このメニューを 1 回叩くだけで今回の修正が消える
```

### 課題 2 の見立ても正しい。ログの文字コードが一致している

```
Relic_01_IronBoots.asset  _displayName: "鉄下駄"   → Editor.log の NameText の 3 文字と完全一致
Relic_01_IronBoots.asset  _description: "ターン..." → DescText の タ ー ン と一致
UILayoutBuilder.cs:488    "選択する"                            → [Text] の 選 択 す る と一致
```

**推測ではなく、ログに出た 4 系統すべての出所が特定できている。**

日本語が残っている player-facing なデータは、確認した範囲でこれだけである。

| 場所 | 件数 | 画面に出るか |
|---|---|---|
| `Relic_01..06`（`_displayName` / `_description`） | 6 | **出ている**（今回の報告そのもの） |
| `UILayoutBuilder.cs:488` `"選択する"` | 1 | **出ている** |
| `Data/Commands/{Study,Train,Rest}.asset` `_commandName` | 3 | 現状は出ない（ボタン名は 488 行群のハードコード英語） |
| `Data/Events/Event_MidExam.asset` `_displayName` / `_body` | 1 | **ターン 12 で出る** |
| Boss / Ending 系アセット | 0 | — |

`Event_MidExam` は**同じプレイ中のターン 12 で必ず豆腐になる**。ここを残すと、
人間が同じ報告をもう一度書くことになる。今回に含める。

### 2-A の実装で 1 点だけ変える

`RelicAssetGenerator.CreateRelic`（55 行）は `AssetDatabase.CreateAsset` を使う。
**既存パスに対して呼ぶと、アセットは作り直され GUID が変わる。**
`Tools/Generate Relic Assets` を再実行すると `RelicCatalog.asset` ごと GUID が変わり、
`GameFlowController` やシーンからの参照が全部切れる。**再生成は禁止する。**

`Data/Commands/*` と `Data/Events/*` にはそもそも生成スクリプトが無く、
`.asset` のテキスト編集は `00_rules.md` で禁止されている。

したがって **`AssetDatabase.LoadAssetAtPath` + `SerializedObject` で既存アセットを
その場で書き換える Editor メニューを 1 本作る**。GUID は保たれ、規約も破らない。
`RelicAssetGenerator.cs` のリテラルも英語に直す（次に誰かが生成したとき日本語に戻らないように）。

### 案 2-B（日本語フォント導入）を却下する理由

やること自体は正当だが、いま人間が詰まっているものへの答えではない。
フォントの入手とライセンス確認、SDF アトラス生成、TMP Settings の fallback 設定、
バイナリアセットのコミットが要る。画面の他のテキストは全部英語（`STUDY` / `TRAIN` /
`BOSS BATTLE` / `RELIC DRAFT (SELECT PASSIVE)`）であり、**日本語は 10 箇所の取り残しであって
方針ではない。** 表記を英語へ寄せるほうが、いまの UI と整合する。
日本語表示を製品要件にするなら、それは別サイクルの設計判断とする。

## なぜ PlayMode テストが緑のまま通り抜けたか（ここを直さないと同じことが起きる）

```
SmokeTest.cs:256-259, 347-352  ExecuteEvents.Execute(cardBtn.gameObject, ..., pointerClickHandler)
```

`ExecuteEvents.Execute` はボタンの GameObject に直接イベントを送る。
**`GraphicRaycaster` を通らないので、手前を何が覆っていても必ず成功する。**
`02-context.md` の指摘どおりであり、これは偽陰性の構造的な原因である。

今回の受け入れ条件には、**`GraphicRaycaster.Raycast` の最上位ヒットが
狙ったボタンであること**を含める。ここを足さない限り、次に誰かがオーバーレイを
残しても、テストはまた緑になる。

## 対象を広げない線引き

`MetaShopDialogView` / `EventDialogView` の `_panelRoot` にも同じ形の危険がある
（`MetaShopDialogPanel` は `BossBattleDialogPanel` より後発 sibling なので、
もし開きっぱなしになれば全部を覆う）。**が、今回は直さない。**
代わりに、上の Raycast 検証が赤くなったらそこで手を止めて報告させる。
発火条件も設計も未決着のものを、進行不能バグの修正に混ぜない（指示書 17 と同じ判断）。

## 受け入れ条件

**テストが緑になったことでは判定しない。**

1. ターン 6 でボスを倒したあと、**マウスでレリックカードを選べて**次ターンへ進むこと
2. レリックのカード名・説明・ボタンが**豆腐（□）ではなく英語で読めること**
3. `Editor.log` に `was not found in the [LiberationSans SDF] font asset` が **0 件**であること
