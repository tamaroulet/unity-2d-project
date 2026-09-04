# 指示書 18: ボスパネルの全画面オーバーレイ残留と、画面に出る日本語の英語化

```
ROLE: Executor
BRANCH: main
判断: docs/handoff/2026-09-04-03/03-instruction.md（案 1-B + 案 2-A 採用・実装方針は本書に従う）
```

## 目的

ボス撃破後のレリック選択画面で、**カードをクリックしても反応せず先へ進めない**。
同時に、カード名・説明・選択ボタンが **□（豆腐）**になっている。

独立した 2 つの原因が重なっている。**片方だけ直しても人間の報告は解決しない。**

### 原因 1: 閉じたはずのボスパネルが透明な全画面ブロッカーとして残る

```
UILayoutBuilder.cs:363-374  ConfigureModalPanel はパネル直下に全画面 Image（ColorOverlay）を貼る
UILayoutBuilder.cs:105-106  RelicDraftDialogPanel より BossBattleDialogPanel が後発 sibling ＝ 手前
UILayoutBuilder.cs:607      BossBattleDialogView._panelRoot ← PanelRoot（パネルの子）
BossBattleDialogView.cs:99  Show() が gameObject.SetActive(true)
BossBattleDialogView.cs:183 Hide() は _panelRoot（子）しか落とさない。パネル自身は開いたまま
```

前サイクルで `EndingView` / `RelicDraftDialogView` は「`_panelRoot` = パネル自身」に揃えた
（`UILayoutBuilder.cs:706` / `761`）。**ボスダイアログだけが取り残されている。**

これは指示書 17 が「やってはいけない代替」として名指しした形そのものである。

### 原因 2: レリックのテキストが日本語で、TMP の既定フォントに字が無い

```
RelicAssetGenerator.cs:24-29  "鉄下駄" / "ターン開始時、スタミナが 2 回復する。" など 6 件
UILayoutBuilder.cs:488        CreateModalButton(..., "選択する", ...)
Data/Events/Event_MidExam.asset  _displayName / _body（ターン 12 で同じ豆腐になる）
Data/Commands/Study,Train,Rest.asset  _commandName（現状は画面に出ないが同じ地雷）
```

`LiberationSans SDF` に CJK グリフが無く、fallback も未設定のため □ に置換されている。

## 実装方針（この形で直すこと。自分で別案に変えないこと）

### A. ボスダイアログを `UIViews` ホストへ移し、`_panelRoot` にパネル自身を張る

```
Canvas
  UIViews                    ← 常時アクティブ。既に EndingView / RelicDraftDialogView が居る
    ├ EndingView              _panelRoot = EndingPanel
    ├ RelicDraftDialogView    _panelRoot = RelicDraftDialogPanel
    └ BossBattleDialogView    _panelRoot = BossBattleDialogPanel   ← 今回移す
  BossBattleDialogPanel      ← 非アクティブのまま。閉じるときはパネルごと落ちる
```

**オーバーレイ Image の `raycastTarget` は true のまま触らないこと。**
モーダル表示中に背後のクリックを止めるのは正しい役割である。
問題は「閉じたのにパネルが生きている」ことであって、オーバーレイの設定ではない。

やってはいけない代替:

- `Hide()` で `gameObject.SetActive(false)` を足す → `IsPanelActive` は `_panelRoot`（子）の
  `activeSelf` を見るので、**閉じているのに `IsVisible` が true を返す**状態を新設することになる
- オーバーレイの `raycastTarget` を false にする → モーダル中に背後のコマンドボタンが押せてしまう

### B. 画面に出る日本語を英語へ寄せる。既存アセットは「その場で書き換える」

**`Tools/Generate Relic Assets` を実行してはならない。**
`RelicAssetGenerator.cs:55` は `AssetDatabase.CreateAsset` を既存パスに対して呼ぶため、
アセットが作り直されて **GUID が変わり、`RelicCatalog` とシーンからの参照が全部切れる。**

`AssetDatabase.LoadAssetAtPath` + `SerializedObject` で**既存アセットを書き換える**
Editor メニューを 1 本作る。GUID は保たれ、`.asset` のテキスト編集も発生しない。

## タスク

### A. ボスパネルのオーバーレイ残留を止める

- [x] `UILayoutBuilder.SetupUIViews`（331 行〜）に `BossBattleDialogView` を追加する。
      既存の `EndingView` / `RelicDraftDialogView` と同じ書き方に揃えること
- [x] `UILayoutBuilder.SetupBossBattleDialogPanel`（274 行〜）は**パネルの見た目の生成のみ**にする。
      `new GameObject(..., typeof(BossBattleDialogView))` をやめ、
      **既にパネルに付いている `BossBattleDialogView` があれば `DestroyImmediate` で取り除く**。
      残すと `FindFirstObjectByType` がどちらを拾うか不定になる
- [x] `BindBossBattleDialogSceneReferences`（586 行〜）を、`UIViews` 上の View を対象に更新する。
      `_panelRoot` には **`BossBattleDialogPanel` の GameObject 自身**を張る（607 行の
      `PanelRoot` ではない）。子要素（`_bossNameText` 等 6 件）の探索元は
      これまでどおり `BossBattleDialogPanel/PanelRoot` のままでよい
- [x] `BindStatusViewSceneReferences`（675-680 行）の `_bossBattleDialog` を、
      `UIViews` 上の `BossBattleDialogView` から取るように更新する。
      **ここを直さないと `StatusView` の参照が null になり、ボス戦画面自体が出なくなる**
- [x] `BossBattleDialogView.cs:99` の `gameObject.SetActive(true)` を削除する。
      パネルを開くのは `_panelRoot.SetActive(true)` の役目になる。
      あわせて `Show()` の先頭で `_panelRoot == null` のとき `Debug.LogError` を出す
      （黙って何も表示しないのを防ぐ。`BindAsset` と同じ方針）
- [x] `StatusView.cs:59` の `_bossBattleDialog.gameObject.SetActive(true);` を削除する。
      移設後は `UIViews`（常時アクティブ）を起こす無意味な行になり、読む人を誤らせる
- [x] `BossSceneBinder.cs:67,82`（`Tools/Bind Boss to MainGame Scene`）を同じ形に更新する。
      **このメニューはパネルへ View を `AddComponent` し直し、`_panelRoot` に `PanelRoot`（子）を
      張り直す。放置すると 1 回叩くだけで今回の修正が消える**
- [x] `Tools/Setup Complete UI Layout (Simple Shapes)` を実行してシーンを更新する

### B. 画面に出る日本語を英語にする

- [x] `Game/Assets/Editor/PlayerFacingTextTools.cs` を新規作成し、
      `[MenuItem("Tools/Rewrite Player-Facing Text To English")]` を実装する。
      `AssetDatabase.LoadAssetAtPath` + `SerializedObject` で**既存アセットをその場で更新**し、
      `EditorUtility.SetDirty` → `AssetDatabase.SaveAssets` する。
      **`AssetDatabase.CreateAsset` / `DeleteAsset` は使わない**（GUID が変わる）。
      対象アセットが見つからない場合は黙って飛ばさず `Debug.LogError` で名指しする。
      **何度実行しても同じ結果になること（冪等）**

| アセット | フィールド | 書き込む値 |
|---|---|---|
| `Features/Relic/Instances/Relic_01_IronBoots` | `_displayName` | `Iron Geta` |
| 同上 | `_description` | `Recover 2 Stamina at the start of each turn.` |
| `Relic_02_FocusBand` | `_displayName` | `Focus Headband` |
| 同上 | `_description` | `Gain +3 extra Skill on each command.` |
| `Relic_03_EnergyDrink` | `_displayName` | `Energy Drink` |
| 同上 | `_description` | `Recover 2 Mental at the end of each turn.` |
| `Relic_04_LightArmor` | `_displayName` | `Light Armor` |
| 同上 | `_description` | `Stamina cost of commands reduced by 20%.` |
| `Relic_05_MeditationRing` | `_displayName` | `Meditation Ring` |
| 同上 | `_description` | `Recover 3 Mental at the start of each turn.` |
| `Relic_06_PowerWrist` | `_displayName` | `Power Wristband` |
| 同上 | `_description` | `Gain +5 Skill on each command, but Stamina cost +3.` |
| `Data/Commands/Study` | `_commandName` | `STUDY` |
| `Data/Commands/Train` | `_commandName` | `TRAIN` |
| `Data/Commands/Rest` | `_commandName` | `REST` |
| `Data/Events/Event_MidExam` | `_displayName` | `Midterm Exam` |
| 同上 | `_body` | `The midterm exam is here. Everything you have studied is put to the test!` |

- [x] 同じファイルに `[MenuItem("Tools/Report Non-ASCII Player-Facing Text")]` を実装する。
      `RelicSO` / `CommandDataSO` / `GameEventSO` / `BossSO` / エンディング系 SO の
      **プレイヤーに見える文字列フィールド**を全アセット走査し、非 ASCII を含むものを
      「アセットパス / フィールド名 / 値」の形で Console に全件出す。最後に件数を出す。
      **テストにはしない**（指示書 16 の `SceneBindingReport` と同じ方針。まず可視化する）
- [x] `RelicAssetGenerator.cs:24-29` のリテラルを上表と同じ英語に書き換える。
      **`Tools/Generate Relic Assets` は実行しない。** 次に誰かが生成したとき
      日本語に戻らないようにするための、ソース側の同期である
- [x] `UILayoutBuilder.cs:488` の `"選択する"` を `"SELECT"` にする
- [x] `Tools/Rewrite Player-Facing Text To English` を実行する

### C. 同じ見落としが二度と起きないようにする

- [x] `SmokeTest.cs` のレリックカード選択（256-259 行 / 347-352 行）について、
      **`ExecuteEvents.Execute` の前に `GraphicRaycaster` 経由の到達性を確認する**アサートを足す。
      `PointerEventData` の `position` をボタンの画面座標にして
      `GraphicRaycaster.Raycast` を呼び、**最上位ヒットが対象ボタン自身かその子孫**であること。
      違う場合は「何に遮られたか」（ヒットした GameObject 名）を assert メッセージに含めること
- [x] 同じ確認を、ボスダイアログの `dismissButton`（237-241 行）にも足す

> `ExecuteEvents.Execute` はボタンの GameObject へ直接イベントを送るため、
> **`GraphicRaycaster` を通らず、手前を何が覆っていても必ず成功する。**
> 今回の不具合が PlayMode 3/3 緑のまま人間の画面で再現したのは、これが理由である。

## 判断が要る場面

- Raycast の到達性チェックが、**レリックカード / ボスダイアログ以外の場所**で赤くなった場合
  （`MetaShopDialogPanel` や `EventDialogPanel` に遮られている等）は、**その場で直さない。**
  遮っていた GameObject 名と発生ターンを報告して手を止める。
  発火条件と設計が未決着の View を、進行不能バグの修正に混ぜない
- 英語化の対象アセットが見つからない、またはフィールド名が違った場合、
  **近そうなフィールドに推測で書き込まない。** `Debug.LogError` を出して報告に列挙する

## 触ってはいけないもの

```
.agents/rules/**  .claude/**  .github/workflows/**
scripts/nightly_gate.py  scripts/auto_runner.py
scripts/morning_report.py  scripts/nightly_baseline.json
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。
アセットとシーンの変更は `Game/Assets/Editor/` の Editor スクリプト経由で行う（`00_rules.md`）。

- **`Tools/Generate Relic Assets` を実行しない**（GUID が変わり参照が全部切れる）
- `EndingView.cs` / `RelicDraftDialogView.cs` は変更しない
- `ConfigureModalPanel` のオーバーレイ Image の `raycastTarget` を変えない
- `MetaShopDialogView` / `EventDialogView` / UI Toolkit パイロットには手を出さない
- 日本語フォント（NotoSans 等）のインポート・TMP fallback 設定はしない（別サイクルの判断）

## 検証

**テストが緑になっただけでは完了としない。実際にマウスでレリックを選べることで判定する。**

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー |
| 2 | `Tools/Report Non-ASCII Player-Facing Text` の出力 | **0 件** |
| 3 | `Tools/Report Unbound Serialized Fields` の出力 | `BossBattleDialogView` / `StatusView` の件数が増えていないこと。残件数を報告する |
| 4 | EditMode / PlayMode テスト | 実行前（EditMode 115 / 96 passed, PlayMode 3 / 3 passed）と同じか改善。件数を明記 |
| 5 | **ターン 6 のボス撃破後** | **マウスでカードをクリックして選択でき**、次ターンへ進めること |
| 6 | 同上の画面 | カード名・説明・ボタンが英語で読め、□ が 1 つも無いこと |
| 7 | `Editor.log` | `was not found in the [LiberationSans SDF] font asset` が **0 件** |
| 8 | ターン 1〜5 の見た目 | オーバーレイが画面を覆っていない。コマンドボタンがマウスで押せること |
| 9 | ターン 12 のイベントダイアログ | 英語で表示され、□ が無いこと |
| 10 | ターン 24 の通過 | エンディング画面が出ること（前サイクルの結果を壊していないこと） |
| 11 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 5・6・8・9・10 は **`ExecuteEvents` ではなく実際のマウス操作**で確認すること。
Unity を開いて Play するか、Unity-MCP で操作するか、**どちらを使ったか明記する。**

> **注意**: `ExecuteEvents.Execute` を使ったテストは、この不具合に対して**必ず緑になる**。
> 検証 5 を「PlayMode が通ったから OK」で代替しないこと。

## 停止条件

- 同じ修正を 2 回試して直らない
- `.unity` / `.asset` を直接編集しないと進めなくなった
- Raycast の到達性チェックが、レリックカード / ボスダイアログ以外の場所で赤くなった
- 英語化の対象アセットまたはフィールドが見つからない
- `StatusView` の `_bossBattleDialog` を移設後に結線できない
- 保護対象ファイルの変更が必要になった

## 報告

`docs/handoff/2026-09-04-03/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/instructions/18_boss_overlay_and_english_text.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## 検証
（上の 1〜11 の生出力を全部。特に 5・6・8・9・10 は Console のログをそのまま）

## 結線できなかったフィールド / 書き換えられなかったアセット
（あれば名指しで。無ければ「なし」）

## 停止条件への抵触
なし / あり（内容）
```

完了したタスクは、**その場で `- [x]` に更新してからコミットする**（規約 §0）。
コミットのみ行い、push はしない。
