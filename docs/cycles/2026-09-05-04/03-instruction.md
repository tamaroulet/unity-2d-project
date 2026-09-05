# 指示書 23: イベントダイアログの一本化と、不要物の撤去

```
ROLE: Executor
BRANCH: main
判断: 人間との直接協議。02-context.md なし
前提: 指示書 20 / 21 / 22 はコミット済み（1ef5ee8 / 945f7e4 / 15d83eb）だがテスト未実行
例外: 本サイクルに限り scripts/pm1_opus_review.py は保護対象から外す
```

## 目的

指示書 21 のスナップショットが、イベントダイアログの実態を出した。

```
docs/snapshot/scene_bindings.txt:22-26
Canvas/EventDialogPanel  [active=false]
  EventDialogView
    _eventCatalog      -> <unbound>
    _eventFiredChannel -> <unbound>
```

`EventDialogViewUI`（UI Toolkit パイロット）は**スナップショットに 1 行も出ていない＝シーンに存在しない。**
`EventDialogView`（uGUI）はシーンに居るが**全フィールド未結線。**

**イベントダイアログは 2 実装あって、どちらも動いていない。**

`UILayoutBuilder.cs:242` が毎回 `EventDialogView` を付け、`EventDialogUIBinder.cs:117-122` が
毎回それを Disable する。互いに打ち消し合っている。

### 未確認だが重い懸念

`GameFlowController.cs` はイベント発火時に `_currentPhase = GamePhase.ShowingEvent` にして
`_eventFiredChannel.Raise` する。復帰は `OnEventDismissed()` だが、**購読する View が無いので
誰も呼ばない。** 理屈上はターン 12 で `ShowingEvent` のまま固まる。
実際にはターン 24 まで到達した報告があるので、**イベントが発火していない可能性が高い。**
どちらなのかを §3-6 で確定させる。

---

## 1. 実装方針

### A. uGUI（`EventDialogView`）に一本化し、UI Toolkit パイロットを撤去する

既存の View はすべて uGUI + `[SerializeField]` + Controller 直参照で揃っている
（`CommandButtonView` / `StatusView` / `RelicDraftDialogView` / `EndingView` / `MetaShopDialogView`）。
**イベントダイアログだけ別系統にする理由が無い。** シーンに存在すらしていない側を捨てる。

削除する（`.cs` と `.meta` を対で。Unity 上で削除するか `git rm` を使う）。

```
Game/Assets/UI/Scripts/EventDialogViewUI.cs
Game/Assets/Editor/EventDialogUIBinder.cs
Game/Assets/UI/UXML/EventDialog.uxml
Game/Assets/Tests/UxmlBindingTests.cs
```

`Game/Assets/UI/UXML/` が空になったらディレクトリごと削除する。

### B. `EventDialogView` をシーンへ結線する

`Game/Assets/Editor/UILayoutBuilder.cs`

`BindSceneReferences`（`:589` 付近の `Bind*` が並ぶ箇所）に
`BindEventDialogSceneReferences(canvasTr, controller)` を足す。
中身は `BindMetaShopDialogSceneReferences`（指示書 22 で追加済み）と同じ形。

- **`EventDialogView` を `UIViews` へ移す。** 現状は `active=false` のパネル本体に付いており、
  `OnEnable` が走らない。**`MetaShopDialogView` と同じ罠**（指示書 19 §2-C-1）
- `_panelRoot` ← `EventDialogPanel`（パネル自身。子の `PanelRoot` ではない）
- `_eventFiredChannel` ← `Assets/Data/Channels/EventFiredChannel.asset`
- `_eventCatalog` ← `GameEventCatalog`（`GameFlowController` が持っているものと同じ実体）
- `_titleText` / `_bodyText` / `_okButton` ← `EventDialogPanel/PanelRoot` 配下の該当要素
- `_gameFlowController` ← `controller`

`SetupEventDialogPanel`（`:239-`）にタイトル・本文・OK ボタンが揃っていなければ作る。
ボタンのラベルは ASCII（`OK` など）。

**`EventDialogView.cs` のロジックは変更しない。** 結線だけで動くはずである。
動かない場合は §4 の停止条件に従う。

### C. `pm1_opus_review.py` の消費を削る

`scripts/pm1_opus_review.py`

1. `:61` の `timeout=1800` を `timeout=600` にする
2. プロンプトに渡す隔離ブランチ情報（`:35-38`）は `git diff --stat` の全文ではなく
   **各ブランチ 20 行まで**に切る
3. 冒頭に「引数 `--force` が無く、かつ `get_claude_quota.ps1` の `IsAvailable` が false なら
   何もせず終了する」ガードを足す。**現在このスクリプトは枠を見ずに走る**

`-Model opus` は**変えない**（`00_rules.md` の移行条件が未達）。

### D. 不要物の撤去

1. `docs/archive/audit_fixes.md` と `docs/archive/step8_fix_instructions.md` を
   `docs/archive/` へ移動する（削除しない。番号なしで現行の採番と混ざるのが問題）
2. `.gitignore` に `logs/` を追加し、`git rm -r --cached logs` で追跡から外す
   （ファイル自体は消さない）
3. `docs/webgl/`（13MB）は**本サイクルでは触らない。**
   GitHub Pages の配信元である可能性があり、移すなら公開 URL が変わる。§5 に送る

---

## 2. やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/cycles/*/03-instruction.md（本書以外。§1-D-1 の 2 枚は移動のみ）  README.md
docs/spec/**  docs/decisions/**  .agents/rules/00_rules.md  .claude/hooks/guard.js
.github/workflows/**  scripts/handoff.ps1  scripts/invoke_claude_safe.ps1
scripts/nightly_gate.py  scripts/auto_runner.py  Game/Packages/manifest.json  .mcp.json
```

- `.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止
- **`.unity` を読まない**（`00_rules.md`）。結線の確認は `docs/snapshot/scene_bindings.txt` で行う
- 新規 `EventChannelSO` / 新規 View コンポーネントを作らない。`EventFiredChannel` は既存
- `EventDialogView.cs` / `GameFlowController.cs` のロジックを変更しない。**結線だけで直す**
- `docs/webgl/` を移動・削除しない
- `-Model opus` を `sonnet` に変えない
- テストを通すためにプロダクションコードへ分岐や自己修復を足さない

---

## 3. 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー / 警告 3 件 |
| 2 | **EditMode / PlayMode テストを実行** | `logs/*_results.xml` が更新される。件数を貼る |
| 3 | `scripts/mechanical_check.ps1` | **`STALE` が消えて `PASS Tests` になること。** 生出力を貼る |
| 4 | `Tools/Generate Scene Snapshot` 後の `git diff docs/snapshot/` | `EventDialogView` の `_eventCatalog` / `_eventFiredChannel` / `_okButton` が `<unbound>` でなくなり、ホストが `Canvas/UIViews` になっていること。**差分をそのまま貼る** |
| 5 | `Tools/Report Unbound Serialized Fields` | 実行前より**減っていること**。件数を報告 |
| 6 | **ターン 12 を実際にマウスで通す** | イベントダイアログが英語で表示され、`OK` を押すとコマンド入力へ戻ること。**発火しなかった場合はその事実をそのまま報告する（推測で埋めない）** |
| 7 | ターン 24 まで通す | 既存のクリア経路が壊れていないこと |
| 8 | 敗北経路（指示書 20） | `GAME OVER` → ショップ → 復帰が動くこと |
| 9 | `python scripts/pm1_opus_review.py`（枠が 85% 超の状態で） | **何もせず終了すること。** 出力を貼る |
| 10 | `git status --porcelain` と `git ls-files logs \| wc -l` | 保護対象 0 件。`logs` の追跡が 0 件 |

検証 6・7・8 は **実際のマウス操作**で確認すること。どの手段を使ったか明記する。

> 検証 6 が「発火しなかった」場合、それは失敗ではなく**発見**である。
> `EventResolverSO` の発火条件を調べる作業は本サイクルに含めない。観測結果だけ報告する。

---

## 4. 停止条件

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- `EventDialogView` を結線してもダイアログが出ない
  （**結線以外の原因。ロジックを触らず手を止めて報告する**）
- `EventDialogUIBinder.cs` を消すと `UILayoutBuilder` がコンパイルできない
- `.meta` の削除でシーンに Missing 参照が出た
- 検証 7・8 が壊れた（本サイクルの変更が既存経路を壊した）
- 保護対象ファイルの変更が必要になった

---

## 5. 報告と、次サイクルへ送るもの

`docs/cycles/2026-09-05-04/04-result.md` に規約 §5 の様式で書く。
先頭に `## 機械判定`（`mechanical_check.ps1` の生出力）を置く。

次サイクルへ送る。

- `docs/webgl/` 13MB の扱い（Pages の配信元かを確認してから）
- `Game/Assets/Settings/Lit2DSceneTemplate.scenetemplate` 3.85MB が使われているかの確認
- `_metaProfile` の永続化 / `ResolveUnlockedRelics` のドラフト反映（指示書 19 §5 から持ち越し）
- ショップの内部 ID 表示（`Unlock_Stat_Stamina_01`）の改名（指示書 20 §6 から持ち越し）
- 指示書 22 の検証 5・6（`--resume`）。Claude 枠のリセット後に実施

コミットのみ行い、push はしない。
