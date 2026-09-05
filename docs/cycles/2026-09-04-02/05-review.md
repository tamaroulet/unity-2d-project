# レビュー 2026-09-04-02

**合否: 即時承認**
**再試行: 0 / 2**
**差し戻し: 0 / 2 往復（人間へのエスカレーション不要）**

指示書: `docs/cycles/2026-09-04-02/17_ending_and_draft_binding.md`
コミット: `d20692b`
判定者: Claude（Architect）

## 裏取り（報告を鵜呑みにせず独立に検証）

`04-result.md` の主張を、シーン YAML とコードから独立に検証した。

| 主張 | 実測 | 一致 |
|---|---|---|
| ビルド 0 エラー | 再実行して `3 個の警告 / 0 エラー` | ○ |
| `UIViews` を Canvas 直下に常時アクティブで生成 | `MainGame.unity:1183` `m_Name: UIViews` / `m_IsActive: 1` | ○ |
| `UIViews` に `Image` を付けていない | コンポーネントは `RectTransform` + View 2 本のみ（`209142200/201/202`）。`Image` なし | ○ |
| `EndingView` は 1 個だけ（パネル側に残骸なし） | guid `d5efe6ad…` のシーン内出現数 **1** | ○ |
| `RelicDraftDialogView` は 1 個だけ | guid `69093204…` のシーン内出現数 **1** | ○ |
| `_endingDecidedChannel` が `GameFlowController` と同一アセット | 両方とも guid `92ccbf34d9fc21841bf5a6aed1d787dc`（`:1239` と `:9601`） | ○ |
| `_panelRoot` = `EndingPanel` 自身 | fileID `801651503` → `m_Name: EndingPanel` / `m_IsActive: 0` | ○ |
| `_resultText` = `EndingTitleText`（`EndingDescriptionText` ではない） | fileID `860024378` の所有 GO は `m_Name: EndingTitleText` | ○ |
| `RelicDraftDialogView._panelRoot` = パネル自身 | fileID `1801800745` → `m_Name: RelicDraftDialogPanel` / `m_IsActive: 0` | ○ |
| `_cardViews` 3 件 / `_relicAcquiredChannel` 結線済み | 3 要素 + guid `12479b91…`（`RelicAcquiredChannel.asset` と一致） | ○ |
| `EndingView.cs` / `RelicDraftDialogView.cs` を変更していない | コミットが触ったのは `UILayoutBuilder.cs` / `MainGame.unity` / `SmokeTest.cs` の 3 本のみ | ○ |
| 保護対象への変更なし | `.agents/rules/**` `.claude/**` `scripts/**` `.github/workflows/**` `manifest.json` `.mcp.json` いずれも差分ゼロ | ○ |

**報告と実測の食い違いはなかった。**

## 03 の設計判断が実際に成立しているか

`03-instruction.md` の核心は「非アクティブな GameObject に載った View は
`OnEnable()` が走らず永久に購読できない」だった。今回の形で本当に解けているかを
コードで確認した。

```
UIViews (m_IsActive: 1)
  ├ EndingView          _panelRoot → EndingPanel (m_IsActive: 0)
  └ RelicDraftDialogView _panelRoot → RelicDraftDialogPanel (m_IsActive: 0)
```

- `EndingView.OnEnable()` は起動時に走り、`_panelRoot.SetActive(false)` は
  **別 GameObject** を落とすだけなので自分は生き残る。却下案 (b) の自爆は起きていない
- `RelicDraftDialogView.OnEnable()` → `HookController()` も同様に成立する
- 両パネルが `m_IsActive: 0` のままなので、`ConfigureModalPanel` の全画面オーバーレイが
  常時画面を覆う却下案 (a) の副作用も発生しない

**採用した方針が、意図した理由どおりに効いている。**

## 受け入れ条件の判定

`03-instruction.md` は「結線されたことでも、テストが緑になったことでも判定しない。
PlayMode で実際にターン 24 を通過し、エンディング画面が画面に出ることで判定する」と
定めていた。追加された PlayMode テストのアサーションを読んだ。

```csharp
MainGame_AdvanceToTurn24_AllBossesDefeated_ShowsEndingPanel_WithZeroExceptions
```

- 実クリック（`ExecuteEvents.pointerClickHandler`）でターン 1 → 24 を進行
- `Assert.AreEqual(4, bossCount)` — ボス戦 4 回が**実際に画面に出た**こと
- `Assert.AreEqual(3, draftCount)` — ターン 6/12/18 のドラフトが**実際に画面に出た**こと
- `Assert.IsTrue(endingView.IsPanelActive)` — `IsPanelActive` は `_panelRoot.activeSelf` なので、
  結線の有無ではなく**パネルが開いたこと**を見ている
- `Assert.IsFalse(string.IsNullOrEmpty(endingView.DisplayedResult))`
- `Assert.IsEmpty(_capturedFailures)`

`draftCount == 3` の等値アサーションが、**偽陰性の封じ込めとして効いている。**
`GameFlowController.cs:284-292` を確認したところ、`OnRelicDraftRequested` に購読者がいれば
フォールバック `OnRelicAcquired(fallbackRelicId)` は `else` 側なので走らない。
つまり「UI が出ないまま素通りした」場合は `draftCount` が 3 に満たず落ちる。
**03 が警告した偽陰性を、テスト自身が検出できる形になっている。**

`GamePhase.GameOver` でループを抜けた場合も、直後の
`Assert.AreEqual(GamePhase.GameClear, flow.CurrentPhase)` で必ず落ちる。取りこぼしはない。

テストの弱体化（`[Ignore]` / `[Explicit]` / `Assert.Pass` / `ignoreFailingMessages`）は
`SmokeTest.cs` に 1 件も無い。

## 指摘（承認を妨げない。次サイクル以降の申し送り）

### 1. ビルダの冪等性に移行漏れがある（軽微・潜在）

`SetupEndingPanel` / `SetupRelicDraftDialogPanel` は View の**付与をやめた**が、
**既存パネルに残っている View を剥がす処理が無い**。今のシーンは各 1 個で綺麗
（実測で確認済み）だが、この変更**以前**の形のシーンに対して
`Tools/Setup Complete UI Layout` を掛けると、パネル側の未結線 View と
`UIViews` 側の結線済み View が二重に残る。

パネル側は非アクティブなので進行を壊しはしないが、`Report Unbound Serialized Fields` が
7 件増えて調査を惑わせる。次に同種の「View をホストへ移す」作業をするときに、
旧配置の掃除をビルダ側へ入れること。

### 2. 検証 6 は「目視」ではなく構造 + テストで答えている（軽微・結論は妥当）

指示書は検証 6 に「目視した結果を書くこと」と明記したが、報告は
シーン構造の事実とレイキャストの PlayMode アサーションで代替している。

ただし問われていたリスク（オーバーレイが画面を覆う）は、
パネルが `m_IsActive: 0` のままであること・`UIViews` に `Image` が無いことを
こちらでも実測したうえで**構造的に消えている**。証拠としてはむしろ再現可能で強い。
形式は逸脱だが結論は妥当なので、差し戻しはしない。

### 3. Console ログを `...` で省略している（規約 §3 の逸脱・軽微）

規約 §3 は「実測値を要約しない」と定めている。検証 4 のログ抜粋が
`...` で畳まれていた。今回は判定に必要な情報が他の証拠で足りていたが、
省略すると本来はここで手が止まる。次回は生のまま貼ること。

### 4. 未結線件数の表現（事実誤りではないが誤読を招く）

「`RelicDraftDialogView` の 4 件もすべてバインド済み」という書き方は、
それらが 22 件の内訳だったように読める。実際は 4 件とも従来から結線済みで、
22 → 19 の減少分は `EndingView` の 3 件のみである。算術は整合している。

## 残っているもの（今回の対象外・記録として残す）

- **地雷C（敗北時 GameOver UI）**: 03 の判断どおり対象外。結線漏れではなく未実装機能で、
  「何を表示してどこへ戻すか」の設計判断が要る。**次サイクルの筆頭候補**
- **未結線 19 件**: `EventDialogView` 7 / `MetaShopDialogView` 4 / `StatusView` 3 /
  `CommandButtonView` 3 / `BossBattleDialogView` 2。
  `MetaShopDialogView` は発火元未定、`EventDialogView` は UI Toolkit パイロットとの
  二重管理が未決着。いずれも設計判断が先で、結線作業ではない

## 結論

**即時承認。** 人間の原文「ターン12/18/24も同じ地雷が無いか確認したい」に対し、
確認だけでなく**ターン 24 完走が回帰テストとして固定された**。
勝ち筋の進行不能は解消済みで、残るのは敗北時 UI（地雷C）である。

コミット済み・push なし。指示どおり。
