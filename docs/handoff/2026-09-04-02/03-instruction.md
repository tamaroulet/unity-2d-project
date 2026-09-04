# 判断 2026-09-04-02

判定者: Claude（Architect）
発行する確定指示書: `docs/instructions/17_ending_and_draft_binding.md`

## 採用する案

**案2**（EndingView の結線 + RelicDraftDialogView の初期購読問題）を採用する。
ただし **案2 の実装方針は `02-context.md` の想定とは異なる**。理由は下の裏取りを見ること。
案1 は不足、案3 は却下する。

## 裏取り

`02-context.md` の主張を独立に検証した。**地雷Aの症状は正しいが、原因の切り分けが 1 段浅い。**

### 確認できたこと（地雷A）

```
UILayoutBuilder.cs:335-349  SetupEndingPanel()
  - EndingPanel と EndingView を生成し、PanelRoot / EndingTitleText /
    EndingDescriptionText / RestartButton を作る
  - SerializedObject を一切触っていない。BindAsset も BindComponentReference も無い
  - 最終行 349: go.SetActive(false)
UILayoutBuilder.cs:581-583  結線メソッドの呼び出しは Status / RelicDraft / BossBattle の 3 つのみ
  → BindEndingViewSceneReferences は存在しない
```

`_endingDecidedChannel` / `_panelRoot` / `_resultText` が全て null という実測値は、
コード側から見ても整合する。**地雷Aは実在する。**

### 見落とされていること（ここが本題）

**結線を足すだけでは EndingView は直らない。**

```
EndingView.cs:39  private void OnEnable()  ← ここでしかチャンネルを購読しない
UILayoutBuilder.cs:349  go.SetActive(false)  ← EndingView が乗っている GameObject 自体が非アクティブ
```

`OnEnable()` が一度も走らないので、`_endingDecidedChannel` を正しく張っても
`OnEventRaised += OnEndingDecided` は実行されない。`triggerEnding()` が
`_endingDecidedChannel?.Raise(ending)` を呼んでも、購読者はゼロのままである。

`02-context.md` はこれを「さらに EndingPanel が初期非アクティブのため」と 1 行で併記しているが、
**これは付随条件ではなく主因**であり、案1 の作業内容（結線 + シーン更新）だけを実施すると
「結線は済んだのにターン 24 でやはり何も出ない」で終わる。

同じ構造が `RelicDraftDialogView` にもある（`OnEnable()` → `HookController()`、
`UILayoutBuilder.cs:292` で `go.SetActive(false)`）。**地雷Aと地雷Bは同一の欠陥である。**

### なぜボスダイアログだけ動いているのか

指示書 16 で直った `BossBattleDialogView` も `BossBattleDialogPanel` ごと非アクティブ
（`UILayoutBuilder.cs:313`）だが、こちらは自分で購読していない。
**常時アクティブな `StatusView` が `_bossBattleDialog` の直参照を持って呼び出している**
（`BindStatusViewSceneReferences`）。呼ばれた側の `Show()` が `_panelRoot.SetActive(true)` する。

```
動く形:  常時アクティブなオブジェクト → 直参照で呼ぶ → 非アクティブなパネルを開く
壊れる形: 非アクティブなオブジェクト → 自分の OnEnable で購読する → 永遠に走らない
```

`EndingView` と `RelicDraftDialogView` は後者に該当する。

## 採用する実装方針

**View コンポーネントを、常時アクティブなホストへ移し、`_panelRoot` にモーダルパネル本体を指させる。**

```
Canvas
  UIViews                （常時アクティブ・RectTransform のみ・描画なし）
    ├ EndingView          _panelRoot = EndingPanel
    └ RelicDraftDialogView _panelRoot = RelicDraftDialogPanel
  EndingPanel            （非アクティブ。オーバーレイ + PanelRoot カードを含む）
  RelicDraftDialogPanel  （非アクティブ）
```

こうすると `OnEnable()` が起動時に走って購読が成立し、`OnEndingDecided()` の
`_panelRoot.SetActive(true)` がパネルごと開く。**`EndingView.cs` も `RelicDraftDialogView.cs` も
一行も変えなくてよい。**

### 却下した実装方針

**(a) パネルをアクティブのままにし、`_panelRoot` を内側の `PanelRoot` に張る**
`ConfigureModalPanel()` はパネル直下に**全画面の半透明オーバーレイ Image** を貼る
（`anchorMin/Max = 0..1`, `ColorOverlay`）。パネルをアクティブにすると、
カードが隠れていてもオーバーレイだけが常時画面を覆い、コマンドボタンのレイキャストも奪う。
どちらの View もオーバーレイ Image を制御するフィールドを持たないので、閉じる手段が無い。

**(b) `_panelRoot` にパネル自身を張る**
`OnEnable()` の `_panelRoot.SetActive(false)` が自分のホストを落とすので、
二度と `OnEndingDecided` を受け取れない。

**(c) `StatusView` に `_endingView` / `_relicDraftDialog` を持たせてハブにする**
ボスダイアログの形に揃うが、`StatusView`（ステータス表示）が UI 全体のバインダを兼ねることになる。
関心が混ざり、次に画面が増えるたびに `StatusView` が肥大する。

## 案1・案3 を却下する理由

**案1（EndingView だけ）**
ターン 24 の確定詰みは塞がるが、地雷Bを残す。地雷Bは**同じ 1 つの欠陥の別の現れ方**であり、
上の方針なら追加コストはホストへの移動 1 行分しかない。分けると、まったく同じ調査を
もう一度やることになる。今日すでに 3 回繰り返している失敗と同じ形。

**案3（未結線 22 件の一掃）**
`MetaShopDialogView` は**発火元がまだ決まっていない**。`EventDialogView` も UI Toolkit
パイロット（`EventDialogViewUI`）との二重管理が未決着で、指示書 16 の判断で
「パイロットが画面で確認できてから横展開する」と決めたばかりである。
結線の可否ではなく設計判断が残っている対象を、進行不能バグの修正に混ぜない。

## 地雷C（GameOver UI）について

**今回の対象外とする。** 敗北時 UI は結線漏れではなく**未実装の機能**であり、
「何を表示して、どこへ戻すか」の設計が要る。人間の原文は
「ターン12/18/24も同じ地雷が無いか確認したい」であり、勝ち筋の進行不能の解消が問われている。
別サイクルで扱う。`03` に記録した以上、消えはしない。

## 受け入れ条件

**結線されたことでも、テストが緑になったことでも判定しない。**
**PlayMode で実際にターン 24 を通過し、エンディング画面が画面に出ることで判定する。**

指示書 16 の注意はここでも有効である。EditMode にはシーンが無く、
`OnRelicDraftRequested` も `EndingDecidedChannel` も購読者ゼロのまま
`GameFlowController` のフォールバックが働くため、**24 ターン完走の EditMode テストは
この不具合に対して必ず緑になる**。偽陰性を受け入れ条件にしないこと。
