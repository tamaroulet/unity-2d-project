# 指示書 19: 周回ループの正規化と、メタポイントを「見える・使える」状態にする

```
ROLE: Executor
BRANCH: main
判断: docs/handoff/2026-09-04-03/05-review.md（塊 B は条件付き追認。条件が本書である）
前提: 指示書 18 完了済み。コミット 6ca2450 の成果物は残す（revert しない）
```

## 目的

人間の報告は **「クリアして周回しても、獲得したポイントが持ち越されない」**。

裏取りした結果、**ポイントは持ち越されている。** `_metaProfile` は
`FinalizeRun` で加算され、`StartGame()` はこれをリセットしない（`GameFlowController.cs:117-139`）。

本当の欠陥は別にある。**貯まったポイントを画面に出す経路も、使う経路も、1 本も存在しない。**

```
UILayoutBuilder.cs:166     CreateLabel(rect, "PointsText", "POINTS: 0", ...)
                           ← StatusView にバインドされていない。永久に "POINTS: 0" と出す静的ラベル
UILayoutBuilder.cs:589-592 BindSceneReferences が MetaShop をバインドしていない
MainGame.unity             MetaShopDialogView の _panelRoot / _availablePointsText /
                           _totalRunsText / _closeButton が 4 つとも {fileID: 0}
MainGame.unity             MetaShopDialogView は MetaShopDialogPanel（m_IsActive: 0）に付いている
                           ← 非アクティブなので OnEnable が一生走らない
MetaShopDialogView.cs:40   Show() を呼ぶランタイムコードが 1 箇所も無い
GameFlowController.cs:360  TryPurchaseMetaUnlock を呼ぶのはテストだけ
UILayoutBuilder.cs:311-313 ショップのカード文言が "Cost: 50 / 100 / 150" のハードコード。
                           実データは Stamina=50 / Skill=80 / Mental=50（Instances/*.asset）で食い違う
```

**プレイヤーから見ると「ポイントが 0 のまま増えない」。これが報告の正体である。**

したがって本サイクルの主題は「持ち越しの実装」ではない。**可視化と消費導線の敷設**である。

---

## 1. 先に却下する設計 — `RestartGameChannelSO` は作らない

依頼書は「`EndingView` から `_gameFlowController` を外し、`RestartGameChannelSO`（VoidChannelSO）
経由に変える」ことを求めている。**この案は採らない。** 理由は 2 つ、どちらも単独で決定的である。

### 却下理由 1: `00_rules.md` が新規イベントチャネル SO の追加を禁じている

> DI コンテナ（Zenject / VContainer）、Addressables、**新規イベントチャネル SO の追加は禁止**。

規約の明文に正面から当たる。規約を変えるならそれは実装サイクルではなく、規約改訂の議題である。

### 却下理由 2: 「View は Channel だけを触る」という前提が、この codebase の事実ではない

```
CommandButtonView.cs:17     [SerializeField] private GameFlowController _gameFlowController;
RelicDraftDialogView.cs:17  [SerializeField] private GameFlowController _gameFlowController;
StatusView.cs:28            [SerializeField] private GameFlowController _gameFlowController;
EndingView.cs:31            [SerializeField] private GameFlowController _gameFlowController;  ← 6ca2450 で追加
```

**既存の 3 View がすでに同じ形をしている。** `EndingView` だけをチャネル化すれば、
4 つの View が 2 種類の作法に割れる。それは疎結合ではなく不統一である。

この codebase が実際に採っている疎結合の作法は次の 1 つで、**新しい SO を必要としない**。

> **Controller は素の C# イベントを公開し、View が `[SerializeField]` で Controller を掴んで購読する。**
> 例: `GameFlowController.OnBossBattleOccurred` / `OnRelicDraftRequested`
> （`RelicDraftDialogView.cs:24-48`、`StatusView.cs:47-53`）

**本書はこの既存の作法だけを使う。** `EndingView._gameFlowController` は残す。

### 直すのは結合ではなく「View が流れを決めていること」

`EndingView.OnRestartClicked` の問題は参照を持っていることではない。
**「次に何が起きるか」を View が決めている**（`EndingView.cs:79-90` が直接 `StartGame()` を呼ぶ）ことである。
周回の流れは `GameFlowController` の責務なので、View は意図だけを渡す形に落とす。§2-B。

---

## 2. 実装方針（この形で直すこと。自分で別案に変えないこと）

### A. `GameFlowController` — 周回の流れと、プロフィール変化の通知を持たせる

`Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`

1. イベントを 2 本追加する。**新しい SO は作らない。素の C# イベントである。**

```csharp
/// <summary>メタプロフィールが変化したときの通知（HUD 更新用）。</summary>
public event System.Action<MetaProfileState> OnMetaProfileChanged;

/// <summary>周回終了後のショップ提示要求 (Profile, OnShopClosed)。購読者が居なければ即座に次周回へ。</summary>
public event System.Action<MetaProfileState, System.Action> OnMetaShopRequested;
```

2. `RequestRestart()` を追加する。**周回の流れを決めるのはここだけ。**

```csharp
public void RequestRestart()
{
    if (OnMetaShopRequested != null)
    {
        OnMetaShopRequested.Invoke(_metaProfile, StartGame);
        return;
    }
    StartGame();
}
```

3. `FinalizeRun`（`:348-355`）を二重加算に対して閉じる。
   `private bool _runFinalized;` を足し、冒頭で `if (_runFinalized) return;`、末尾で `true` にする。
   加算後に `OnMetaProfileChanged?.Invoke(_metaProfile);` を呼ぶ。

4. `StartGame()`（`:117-139`）の先頭で `_runFinalized = false;` に戻し、
   `LastEncounteredBoss` / `LastBossBattleResult` も落とす。
   `_metaProfile` には**触らない**（周回を跨いで保持するのが正しい）。
   末尾で `OnMetaProfileChanged?.Invoke(_metaProfile);` を呼ぶ。

5. `TryPurchaseMetaUnlock`（`:360-373`）の成功時に `OnMetaProfileChanged?.Invoke(_metaProfile);` を呼ぶ。

### B. `EndingView` — 意図を渡すだけにする

`Game/Assets/UI/Scripts/EndingView.cs`

- `:28-29` の `【規約違反注記】` コメント 2 行を**削除する**（本文は `05-review.md` に移した）。
- `OnRestartClicked`（`:79-90`）の `_gameFlowController.StartGame()` を
  **`_gameFlowController.RequestRestart()` に差し替える**。パネルを閉じる処理はそのまま残す。
  **`EndingPanel` を先に閉じること**。`MetaShopDialogPanel` より後発 sibling ＝手前に居るため、
  閉じないとショップのボタンに Raycast が届かない（`UILayoutBuilder.cs:107-108` の生成順）。
- `_restartButton` / `_gameFlowController` フィールドは**残す**。`:139-146` の英語 switch も**残す**。
- `Bind(EndingDecidedChannelSO)`（`:97-104`）に `_restartButton` の購読を足し、PlayMode から結線できるようにする。

### C. `MetaShopDialogView` — 実際に買える画面にする

`Game/Assets/UI/Scripts/MetaShopDialogView.cs`

1. **コンポーネントを `UIViews` へ移す。** 現状は `m_IsActive: 0` のパネル本体に付いているため
   `OnEnable` が走らず、購読が一生成立しない。**指示書 18 でボスダイアログを直したのと同じ形である。**
   `_panelRoot` には `MetaShopDialogPanel`（パネル自身）を張る。子の `PanelRoot` ではない。

2. フィールドを追加する。

```csharp
[SerializeField] private GameFlowController _gameFlowController;
[SerializeField] private MetaUnlockCatalogSO _unlockCatalog;
[SerializeField] private Button[] _itemButtons = new Button[3];
[SerializeField] private TextMeshProUGUI[] _itemNameTexts = new TextMeshProUGUI[3];
[SerializeField] private TextMeshProUGUI[] _itemDescTexts = new TextMeshProUGUI[3];
```

3. `OnEnable` / `OnDisable` / `Bind(GameFlowController)` を `RelicDraftDialogView.cs:24-61` と同じ形で書き、
   `_gameFlowController.OnMetaShopRequested` を購読する。ハンドラは `Show(profile, onClose)`。

4. `Show` の中で 3 枚のカードを `_unlockCatalog.Unlocks` から描く。**文言のハードコードは禁止。**

```
NameText : unlock.UnlockName
DescText : "+{BonusValue}" と "Cost: {Cost} Pts" の 2 行
Button   : interactable = 未購入 かつ profile.AvailableMetaPoints >= unlock.Cost
           押下 → _gameFlowController.TryPurchaseMetaUnlock(unlock)
                → UpdateProfile(_gameFlowController.MetaProfile) → カード再描画
```

`Unlocks` が 3 件未満なら余ったカードは `SetActive(false)`。3 を超える分は表示しない。

5. `Dismiss()`（`:69-78`）は今のままでよい。`_onCloseCallback` が `StartGame` を呼ぶ。
   `CLOSE / NEXT RUN` ボタンがそのトリガである。

### D. `StatusView` — HUD の `POINTS:` を生かす

`Game/Assets/UI/Scripts/StatusView.cs`

- `[SerializeField] private TextMeshProUGUI _metaPointsText;` を追加。
- `HookFlowControllerEvents`（`:47-53`）に `OnMetaProfileChanged` の購読を足し、
  `OnDisable`（`:76-86`）で外す。ハンドラは `POINTS: {AvailableMetaPoints}` を書き込む。
- 確認用に `public MetaProfileState LastDisplayedProfile { get; private set; }` を公開する
  （`LastDisplayedState` と同じ理由。TMP 未インポート環境では `.text` が空を返すため）。

### E. `UILayoutBuilder` — 上の 4 つをシーンへ結線する

`Game/Assets/Editor/UILayoutBuilder.cs`

1. `SetupMetaShopDialogPanel`（`:300-317`）
   - `MetaShopDialogView` を**パネルではなく `UIViews` に付ける**（`SetupUIViews` 側で足す）。
     パネル側に残っている `MetaShopDialogView` は `DestroyImmediate` で落とす。
   - `CreateShopItemCard`（`:515-518`）が `CreateRelicCard` を呼んでいるため、
     ショップのカードに `RelicCardView` が乗ってしまっている。**乗せない形に変える。**
     カードの子の名前は `NameText` / `DescText` / `SelectButton` のまま（`:495-498` と同じ）にして、
     既存の `RelicCardView` があれば `DestroyImmediate` する。
   - カード文言のハードコード（`:311-313`）は**プレースホルダで良い**。実値は実行時に `Show` が入れる。
   - `_availablePointsText` / `_totalRunsText` の受け皿が無いので、`PointsText` / `RunsText` の
     2 ラベルを `PanelRoot` 直下に作る。
2. `BindSceneReferences`（`:589-592`）に `BindMetaShopDialogSceneReferences(canvasTr, controller)` を足す。
   中身は `BindBossBattleDialogSceneReferences`（`:595-633`）をなぞる。
   `_panelRoot` ← `MetaShopDialogPanel` 自身、`_closeButton` ← `PanelRoot/CloseShopButton`、
   `_availablePointsText` / `_totalRunsText` ← 上で作った 2 ラベル、
   `_unlockCatalog` ← `Assets/Features/MetaProgression/Instances/MetaUnlockCatalog.asset`、
   `_gameFlowController` ← `controller`、
   3 枚のカードの `NameText` / `DescText` / `SelectButton` を配列プロパティへ。
3. `BindStatusViewSceneReferences`（`:677-`）で `StatusPanel/PointsText` の `TextMeshProUGUI` を
   `_metaPointsText` に結線する。`:166` の `CreateLabel` は戻り値を捨てているので、受け取る形に直すこと。

`MetaProgressionSceneBinder.cs:43-76` は `MetaShopDialogPanel` を**パネル側に**作る古い経路である。
本書では**触らない。実行もしない。** `UILayoutBuilder` を正とする。

### F. `SmokeTest` — 持ち越しをアサーションで押さえる

`Game/Assets/Tests/PlayMode/SmokeTest.cs`（`:402-421` の続きに足す）

1. RESTART クリックの**前**に `flow.MetaProfile.AvailableMetaPoints` を控える（`pointsBeforeRestart`）。
2. クリア時点で `pointsBeforeRestart > 0` を確認する（`FinalizeRun` が走っている証拠）。
3. RESTART クリック後、`MetaShopDialogView.IsPanelActive == true` になることを待つ。
   `AssertRaycastReachesButton` でショップの `CloseShopButton` に Raycast が届くことを確認する。
4. `CLOSE / NEXT RUN` をクリックし、既存の
   `!endingView.IsPanelActive && flow.CurrentPhase == WaitingInput && CurrentTurn == 1` の待機へ繋ぐ。
5. **持ち越しの本体**: リスタート後に
   `Assert.AreEqual(pointsBeforeRestart, flow.MetaProfile.AvailableMetaPoints)` と
   `Assert.AreEqual(1, flow.MetaProfile.TotalRunsCompleted)`（`FinalizeRun` が 1 回だけ走った証拠）。
6. HUD 側: `statusView.LastDisplayedProfile.AvailableMetaPoints == pointsBeforeRestart`。

---

## 3. やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/instructions/*.md（本書以外）  README.md  docs/spec/**  docs/decisions/**
.agents/rules/00_rules.md  .claude/hooks/guard.js  .github/workflows/**
scripts/handoff.ps1  scripts/nightly_gate.py  scripts/auto_runner.py
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。
シーンとアセットの変更は `Game/Assets/Editor/` の Editor スクリプト経由で行う（`00_rules.md`）。

- **`RestartGameChannelSO` を含む、あらゆる新規 `EventChannelSO` を作らない**（§1）
- **`EndingView._gameFlowController` を削除しない**（§1・却下理由 2）
- `6ca2450` を revert しない。`_restartButton` と英語 switch はそのまま使う
- `Tools/Generate Meta Progression Assets` / `Generate Relic Assets` を**実行しない**（GUID が変わって全参照が切れる）
- `MetaProgressionSceneBinder` を実行しない・変更しない
- `_metaProfile` の PlayerPrefs 保存は**本サイクルではやらない**（§5 に分離した）
- `ResolveUnlockedRelics` のドラフト反映は**本サイクルではやらない**（§5）
- `MetaPointResolverSO` / `MetaProfileState` / `MetaUnlockSO` のロジックは変更しない（正しく動いている）
- `ConfigureModalPanel` のオーバーレイ `raycastTarget` を変えない（指示書 18 と同じ）
- テストを通すためにプロダクションコードへ分岐や自己修復を足さない

---

## 4. 検証

**テストが緑になっただけでは完了としない。実際にマウスで 1 周回してポイントが増えることで判定する。**

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー（警告 3 件は既存の DLL 版競合。増えていないこと） |
| 2 | `Tools/Report Non-ASCII Player-Facing Text` | **0 件** |
| 3 | `Tools/Report Unbound Serialized Fields` | 実行前 19 件から **減っていること**。残件数を報告する |
| 4 | EditMode / PlayMode テスト | 実行前（EditMode 115 / 96 passed・19 skipped、PlayMode 3 / 3 passed）と同じか改善。件数を明記 |
| 5 | **ターン 1 の HUD** | 右上が `POINTS: 0`（初回）で、静的ラベルではなく `StatusView` 経由で出ていること |
| 6 | **ターン 24 クリア → RESTART** | エンディングが閉じ、**ショップが開く**こと |
| 7 | 同上のショップ | `POINTS:` に 0 でない実数が出ており、3 枚のカードに実データの Cost が出ていること |
| 8 | 同上のショップ | ポイントが足りるカードの **`SELECT` をマウスで押せて**、ポイントが Cost の分だけ減ること |
| 9 | `CLOSE / NEXT RUN` を押す | ターン 1 の入力待ちへ戻り、**HUD の `POINTS:` が残額を表示している**こと |
| 10 | 2 周目の初期ステータス | 8 で買ったアンロックの分だけ Stamina / Skill / Mental が 1 周目より高いこと |
| 11 | 2 周目のターン 1〜5 | オーバーレイが画面を覆っていない。コマンドボタンがマウスで押せること |
| 12 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 5〜11 は **`ExecuteEvents` ではなく実際のマウス操作**で確認すること。
Unity を開いて Play するか、Unity-MCP で操作するか、**どちらを使ったか明記する。**

> **注意**: 検証 6・8 は `ExecuteEvents.Execute` を使ったテストでは**必ず緑になる**。
> 「PlayMode が通ったから OK」で代替しないこと。§2-C-1 の「非アクティブなので購読できない」罠は、
> `Bind()` を明示的に呼ぶテストでは再現しない。

---

## 5. 本サイクルでやらないこと（次サイクルへ分離した設計課題）

依頼書の要求のうち、次の 2 件は本書のスコープから外した。**忘れて落としたのではない。**

1. **`_metaProfile` の永続化（PlayerPrefs 等）。**
   現状 `_metaProfile` はメモリ上のみで、ブラウザをリロードすると消える。
   ただし人間が報告した不具合は「同一セッション内での周回」であり、そこは今のままでも持ち越せている。
   セーブ形式・データ移行・WebGL の IndexedDB フラッシュは独立した設計判断なので **指示書 20** で扱う。
2. **`ResolveUnlockedRelics` のドラフト反映。**
   `MetaPointResolverSO.cs:189-223` は実装済みだが呼ばれておらず、`UnlockRelic` 種のアンロックが
   ドラフトプールに入らない。現状カタログに `UnlockRelic` のデータが 1 件も無い
   （`Instances/` は Stat 系 3 件のみ）ため、今つないでも効果が見えない。**指示書 20** で扱う。

本書の変更規模は **250 行前後**を見込んでいる。300 行に達したら §6 の停止条件に従うこと。

---

## 6. 停止条件

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- 同じ修正を 2 回試して直らない
- `.unity` / `.asset` を直接編集しないと進めなくなった
- `MetaShopDialogView` を `UIViews` へ移した後、`_panelRoot` を結線できない
- ショップのカードから `RelicCardView` を外すと他の画面が壊れる
- 新しい `EventChannelSO` を作らないと実装できないと判断した（**その時点で手を止めて人間に報告する**）
- 保護対象ファイルの変更が必要になった

---

## 7. 報告

`docs/handoff/2026-09-04-04/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/instructions/19_ending_loop_and_meta_carryover.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## シーン再生成の有無
UILayoutBuilder を実行した / していない
（実行した場合、MainGame.unity の差分行数と、fileID の振り直しが起きたかを明記する）

## 検証
（上の 1〜12 の生出力を全部。特に 5〜11 は Console のログをそのまま）

## 結線できなかったフィールド
（あれば名指しで。無ければ「なし」）

## 停止条件への抵触
なし / あり（内容）
```

完了したタスクは、**その場で `- [x]` に更新してからコミットする**（規約 §0）。
コミットのみ行い、push はしない。

---

## 8. 併せて片付けるガバナンス作業

コード変更と同じコミットに含めてよい。

- `Game/Assets/UI/Scripts/EndingView.cs:28-29` の `【規約違反注記】` コメントを削除（§2-B）
- `docs/tasks/PENDING_PUSH_AFTER_CLAUDE_RESET.md` を**削除**する。
  記録は `docs/handoff/2026-09-04-03/05-review.md` §3 に移した。`docs/tasks/` は規定外のディレクトリである
- 同種の `【規約違反注記】` が他のファイルに残っていれば同様に削除する
  （`git show b8874a7 --stat` で対象を確認すること）
- `docs/handoff/2026-09-04-03/05-review.md` は**変更しない**（Claude が発行した正規レビューである）
