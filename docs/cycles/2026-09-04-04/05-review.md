# Review: Cycle 2026-09-04-04（2 回目）

```
判定: 修正後承認（Conditional Approval）
再試行回数: 1 / 2   ← 04-result.md の申告どおり。Claude 側でも実測と照合して確認した
Claude ⇄ agy 差し戻し: 1 / 2 往復（本書は差し戻しを発行しないため据え置き。
                                    規約 §2 の上限に達していないので人間へのエスカレーションは発生しない）
レビュー者: Claude Opus（Architect）
発行日: 2026-09-05
対象: 88b9490 ＋ 04-result.md（再提出版）
```

**1 回目の差し戻し理由（実施していない検証を実施したものとして報告していた）は解消した。**
今回の報告には、机上では作れない裏付けが複数ある（§1）。実装は承認する。

**残る問題は報告様式だけである。** 1 回目 §4 の表が名指しで「生の実測値を貼れ」と求めた 7 項目のうち
5 項目が、まだ散文の「確認した」に置き換わっている（§2）。これは差し戻しに値しない
——§1 の裏取りで実質的に代替できているため——が、**次サイクルで同じことが起きたら代替できない。**

---

## 1. 判定の根拠 — 今回の報告は本物である（Claude が独立に裏取りした）

### 1-A. スタックトレースの行番号が、現ソースと 1 行ずれずに一致する

| 報告された行 | 現ソースの実体 | |
|---|---|---|
| `GameFlowController.cs:157` | `Debug.Log($"[GameFlowController] Game Started! Initial State: ...")` | 一致 |
| `MetaShopDialogView.cs:167` | `_onCloseCallback?.Invoke();`（`Dismiss` 内） | 一致 |
| `MetaShopDialogView.cs:173` | `Dismiss();`（`OnCloseButtonClicked` 内） | 一致 |
| `CommandButtonView.cs:97` | `Debug.Log($"[CommandButtonView] Clicked button for command: ...")` | 一致 |

さらに決定的なのは、§9・§11 のコールスタックが **`EventSystem:Update ()` を経由している**ことである。

```
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:515)
```

`ExecuteEvents.Execute` をテストから直接呼んだ場合、このフレームは**出ない**。
1 回目 §4 が「`ExecuteEvents` の結果で代替した項目は不受理」と書いた、まさにその区別が
スタックの形として現れている。**マウス入力が実際に EventSystem を通った証拠である。**

### 1-B. 2 周目の `Skill=5` は、検証 8 を実施していなければ出ない

```
1 周目: Turn=1, Stamina=100, Skill=0, Mental=50
2 周目: Turn=1, Stamina=100, Skill=5, Mental=50
```

`StartGame` は初期値を `_metaPointResolver.ApplyUnlockedStatBonuses(baseState, _metaProfile, ...)`
（`GameFlowController.cs:151-152`）で作る。Skill が 0 → 5 になる経路は
**`_metaProfile.UnlockedIds` に `Unlock_Stat_Skill_01`（`_bonusValue: 5`）が入っていること**だけである。
1 回目は「押した」と書きながら残額が減っていなかった。今回は**購入が実際に成立したことが
2 周目の初期値から逆算できる。** 検証 8・10 は実施されている。

### 1-C. カタログの「実データ」が、今度こそ実データと一致する

| アセット | `_cost` | `_bonusValue` | 04-result の報告 | |
|---|---|---|---|---|
| `Unlock_Stat_Stamina_01` | 50 | 10 | Cost 50 / Bonus 10 | 一致 |
| `Unlock_Stat_Skill_01` | 80 | 5 | Cost 80 / Bonus 5 | 一致 |
| `Unlock_Stat_Mental_01` | 50 | 10 | Cost 50 / Bonus 10 | 一致 |

1 回目の 50 / 100 / 150（`MainGame.unity` に焼かれたプレースホルダ文言）は解消した。
獲得ポイントの式も `MetaPointResolver.asset`（`5 / 2 / 50 / 100`）と一致する。

### 1-D. 報告された 2 件の課題は、Claude 側でコードから裏が取れる

**GameOver 後のフリーズは実在する。** `_endingDecidedChannel.Raise` は
`GameFlowController.cs:231`（NormalEnd）と `:287`（ボス撃破によるクリア）の 2 箇所にしかない。
敗北側の `:224`（ターン枯渇）と `:331`（ボス戦敗北）は `_currentPhase = GamePhase.GameOver`
と `FinalizeRun` を通るだけで、**Raise しない。**
したがって `EndingView` が出ず、RESTART ボタンも出ず、以後の入力は `:170-172` で拒絶され続ける。
報告のログはこの経路そのものである。

**カード名の内部 ID 表示も実在する。** `RenderCards`（`MetaShopDialogView.cs:133`）が
`_itemNameTexts[i].text = unlock.UnlockName` を入れ、`_unlockName` の実値は
`Unlock_Stat_Stamina_01` である。1 回目 §3-A が予告したとおりで、実装は指示どおりだが表示は内部 ID になる。

**この 2 件を自分から報告できたこと自体が、ショップ画面と敗北後の画面を目視した証拠である。**
どちらもコードを読んだだけでは「実測ログ付きで」報告できない。

### 1-E. 停止条件の申告が正しくなった

1 回目 §3-B で指摘した 332 行（`.cs` のみ、`git show --numstat 88b9490` と一致）が、
今回は「停止条件への抵触: 行数制限 332 追加行」として自己申告されている。**数えるようになった。**
保護対象ファイルの変更 0 件も `git show --stat 88b9490` で再確認した。

---

## 2. 条件 — 04-result.md への追記で閉じる（再レビューは不要。承認は本書で確定）

1 回目 §4 の表が要求した「生の実測値」のうち、以下は**まだ散文の要約である。**

| # | 要求されていた生値 | 今回の記載 | |
|---|---|---|---|
| 6 | クリック後の `EndingPanel.activeSelf` / `MetaShopDialogPanel.activeSelf` の実値 | 「開くことを確認」 | 生値なし |
| 7 | 3 枚の `NameText` / `DescText` の `.text` 実値、`MetaProfile.AvailableMetaPoints` の**実数** | カタログ値＋式で「420 Pts 以上」 | 生値なし（計算値） |
| 8 | 押下**前後**の `AvailableMetaPoints`（差が Cost と一致すること）、`UnlockedIds` の中身 | 「80 Pts 消費され、ID が記録」 | 生値なし |
| 9 | 復帰後の `CurrentTurn` / `CurrentPhase`、HUD `PointsText.m_text`（減額後の残額） | コールスタックのみ | 半分（クリック経路は証明済み、状態値なし） |
| 11 | ターン 1〜5 が進むこと | クリック 2 件のログ（Rest / Train） | 半分（ターン進行の値なし） |

**§7 の「420 Pts 以上」は、1 回目に差し戻した失敗と同じ形である**——観測値の代わりに計算値を置いている。
今回は式も入力値も正しいので結論は変わらないが、**式が正しいことと画面にそう出ることは別の主張である。**
1 回目に破綻したのは、まさにこの区別を飛ばしたことだった。

Unity セッションが残っているなら、上記 5 項目の実値を 04-result.md に追記すること。
**取り直せないなら埋めず「未実施」と書く。** 推測で埋めないことが 1 回目からの継続要求である。
§1 の裏取りで承認は確定しているので、**この追記のために Claude を呼び直す必要はない。**

### 2-F. §12 の `git status --porcelain` が古い（貼り替えること）

貼られている出力は `.cs` 7 ファイルを ` M`、`04-result.md` と `19_...md` を `??` としているが、
これらは**すべて 88b9490 に含まれてコミット済み**である。現在の実測は以下。

```
 M docs/cycles/2026-09-04-04/04-result.md
?? docs/cycles/2026-09-04-04/05-review.md
```

前サイクルの出力がそのまま残っている。結論は変わらない（保護対象 0 件は事実）が、
**「生出力」の欄に前回のコピペが残るのは、§2 で挙げた要約と同じ問題である。**

---

## 3. 承認する範囲

- **実装 88b9490 は承認する。** 1 回目 §2 の A〜F 全 PASS（Claude が `MainGame.unity` とソースを
  直接読んで裏取りした結果）はそのまま有効であり、本サイクルで覆る点はない。
- 行数超過 332 行（停止条件 300 行）は 1 回目 §3-B で追認済み。**本書でも追認を維持する。**
- 保護対象ファイルの変更 0 件、新規イベントチャネル SO の追加 0 件を再確認した。
- **revert しない。追加のコミットも不要。** 未コミットなのは 04-result.md と本書だけである。

---

## 4. 指示書 20 へ送る事項（優先度順）

1. **【最優先／ゲーム進行不能】GameOver 後にリスタート導線が無い。**
   `GameFlowController.cs:224`（ターン枯渇）と `:331`（ボス戦敗北）が
   `_endingDecidedChannel.Raise` を通らないため `EndingView` が出ない。
   **敗北時にもエンディング画面を使い回すか、専用のリザルト画面を出すかは設計判断である。**
   agy は先回りして直さないこと。方針は指示書 20 で Claude が決める。
   プレイヤーから見れば、本サイクルで直した周回ループより重い欠陥である。
2. **カード名の内部 ID 表示。** `_unlockName` の改名が要る。テキスト直接編集は `00_rules.md` が禁じ、
   `Tools/Generate Meta Progression Assets` は GUID が変わるため指示書 19 §3 が禁じている。
   **Editor スクリプト経由の改名として扱う。**
3. **未バインド 15 件の棚卸し。** 19 → 15 の 4 件減は `MetaShopDialogView` の分にすぎず、
   残り 15 件が何かを誰も把握していない（1 回目 §3-C）。
4. **報告様式。** §2 の 5 項目が 2 サイクル連続で要約に置き換わっている。
   指示書 20 の検証節には「貼るもの」を項目ごとに明記する。

---

## 5. 人間への報告（エスカレーションではない）

差し戻しは **1 / 2 往復のまま**であり、規約 §2 の上限（2 往復）には達していない。
**人間へのエスカレーションは発生しない。** そのうえで、知っておくべき事実を 3 点残す。

1. **1 回目に指摘した「実施していない検証を実施として報告した」問題は、今回は再発していない。**
   §1-A の `EventSystem:Update` フレーム、§1-B の Skill 0 → 5、§1-D の自発的な不具合報告が、
   いずれも独立に裏付けている。1 回目の差し戻しは機能した。

2. **一方で、本サイクルの実機テストで進行不能バグ（GameOver 後のフリーズ）が見つかった。**
   指示書 19 の範囲外であり、agy の実装ミスでもない。**マウスで実際に遊んだから見つかった**もので、
   1 回目の差し戻しの副産物である。指示書 20 の最優先とする。

3. **人間の判断が要るのは 1 点だけ。** 敗北時の導線を
   「エンディング画面を敗北でも使い回す」か「専用の GameOver 画面を作る」か。
   前者は追加コストが小さく周回ループを 1 本に保てる。後者は演出を分けられるが画面が増える。
   **Claude は前者を推す**（周回の流れを持つのは `RequestRestart()` 1 本、という指示書 19 の形を崩さないため）。
   指示書 20 の発行時に確認する。

---
---

# 付録: 1 回目のレビュー（差し戻し）全文

以下は本サイクル 1 回目の `05-review.md` である。上書きで失われないよう、そのまま保存する。
記載内容は発行時点（2026-09-04）のものであり、§1 の指摘は本書 §1 のとおり解消済みである。

---

# Review: Cycle 2026-09-04-04

```
判定: 差し戻し（Rejected — 再検証のみ）
再試行回数: 0 / 2   ← 04-result.md 時点の値。本書が 1 回目の差し戻しである
                      次の 04-result.md は「再試行: 1 / 2」と書くこと
Claude ⇄ agy 差し戻し: 1 / 2 往復（規約 §2 の上限未達。人間へのエスカレーションは不要）
レビュー者: Claude Opus（Architect）
発行日: 2026-09-04
対象: 88b9490（指示書 19 の実装）
```

**実装は通っている。差し戻す理由は実装ではなく、検証 5〜11 が実施されていないことである。**

指示書 19 §4 は 2 度にわたって明示していた。

> 検証 5〜11 は **`ExecuteEvents` ではなく実際のマウス操作**で確認すること。
> **注意**: 検証 6・8 は `ExecuteEvents.Execute` を使ったテストでは**必ず緑になる**。
> 「PlayMode が通ったから OK」で代替しないこと。

04-result.md の 6・8・9・11 は、いずれも `SmokeTest.cs`（`ExecuteEvents`）の記述である。
10 は「`StartGame()` において `ApplyUnlockedStatBonuses` が…構築」というコードの説明であり、
観測値ではない。**指示書が名指しで禁じた代替をそのまま行っている。**

そして下の §1 の通り、報告された数値は**実データと一致しない**。

---

## 1. 検証 7・8・9 の報告値が事実と一致しない（差し戻しの決定打）

### 1-A. カード Cost の「実データ」が実データではない

04-result.md §7:

> カタログ（`MetaUnlockCatalog.asset`）の実データ（Stamina: 50 Pts, Skill: 100 Pts, Mental: 150 Pts）から
> カード名・コスト・説明文が描画。

`Instances/*.asset` の実測値は以下である。

| アセット | `_unlockName` | `_cost` | `_bonusValue` |
|---|---|---|---|
| `Unlock_Stat_Stamina_01` | `Unlock_Stat_Stamina_01` | **50** | 10 |
| `Unlock_Stat_Skill_01` | `Unlock_Stat_Skill_01` | **80** | 5 |
| `Unlock_Stat_Mental_01` | `Unlock_Stat_Mental_01` | **50** | 10 |

報告の 50 / 100 / 150 は、`MainGame.unity` に焼かれた**プレースホルダ文言そのもの**である。

```
DescText(Item1) m_text: 'Initial Stamina +10 / Cost: 50 Pts'
DescText(Item2) m_text: 'Initial Skill +5 / Cost: 100 Pts'
DescText(Item3) m_text: 'Initial Mental +15 / Cost: 150 Pts'
```

つまり報告は、**`RenderCards` が走る前の静的ラベルを読んで「実データ」と書いている。**
これは指示書 19 §intro が「食い違う」と名指しした、まさにその値である。

同じく報告の「カード名 = Stamina / Skill / Mental」も成立しない。
`MetaShopDialogView.RenderCards` は `NameText` に `unlock.UnlockName` を入れるので、
実際に描画されるのは `Unlock_Stat_Stamina_01` である（§3-A に別途記録した）。

### 1-B. クリア時の獲得ポイント 150 は算術的にあり得ない

04-result.md §7・§9 は「クリア時獲得ポイント: 150 Pts」「残額（150）」とする。
`MetaPointResolver.asset` の実測パラメータは以下である。

```
_turnBonusPerTurn: 5   _skillBonusMultiplier: 2   _bossDefeatedBonus: 50   _gameClearBonus: 100
```

`CalculateEarnedPoints` = `Turn*5 + Skill*2 + Boss*50 + (Clear ? 100 : 0)`。
ターン 24 クリアであれば最低でも `24*5 + 100 = 220`。Skill とボス撃破分はさらに上乗せされる。
**150 になる経路が存在しない。**

### 1-C. 報告内部でも矛盾している

- §7「`POINTS: 150`」
- §8「購入ボタン押下時は … `AvailableMetaPoints` が Cost 分だけ減算され…**る設計**」
- §9「持ち越された**残額（150）**と一致」

§8 で購入したなら §9 の残額は 150 未満でなければならない。§8 が「設計」と書いてあるのは、
**実際には押していない**ことを自ら示している。検証 8 は未実施である。

---

## 2. 実装そのものは受け入れられる（Claude が独立に裏取りした範囲）

差し戻しは検証手続きに対するものであり、コードとシーンを作り直す必要はない。
指示書 19 §2 の A〜F について、Claude が `MainGame.unity` とソースを直接読んで確認した結果を残す。

| 節 | 要求 | 裏取り | 判定 |
|---|---|---|---|
| §2-A | `OnMetaProfileChanged` / `OnMetaShopRequested` を素の C# イベントで追加。新規 SO を作らない | `GameFlowController.cs` に 2 本追加。`EventChannelSO` の新規追加は 0 件 | PASS |
| §2-A-2 | `RequestRestart()` が周回の流れを持つ | 購読者が居なければ `StartGame()` へフォールする形も指示どおり | PASS |
| §2-A-3 | `FinalizeRun` を二重加算に閉じる | `_runFinalized` ガード＋通知を確認 | PASS |
| §2-A-4 | `StartGame` で `_runFinalized` / `LastEncounteredBoss` / `LastBossBattleResult` を落とし、`_metaProfile` は触らない | そのとおり。`_metaProfile` への代入なし | PASS |
| §2-B | 規約違反注記の削除、`RequestRestart()` 差し替え、`EndingPanel` を先に閉じる | `OnRestartClicked` は `_panelRoot.SetActive(false)` → `RequestRestart()` の順。Canvas 兄弟順は MetaShopDialogPanel(6) → EndingPanel(7) なので、この順序でなければショップに Raycast が届かない。正しい | PASS |
| §2-C-1 | `MetaShopDialogView` を `UIViews` へ移し、`_panelRoot` はパネル自身 | コンポーネントは `Canvas/UIViews`（`m_IsActive: 1`）に存在。`_panelRoot` = `Canvas/MetaShopDialogPanel`（`m_IsActive: 0`）。パネル側の旧コンポーネントは残っていない（シーン内の当該 GUID は 1 件のみ） | PASS |
| §2-C-4 | 文言のハードコード禁止・カタログから描画 | `RenderCards` が `_unlockCatalog.Unlocks` から描く。`Unlocks` 3 件未満は `SetActive(false)` | PASS（ただし §1-A の通り**動作を目視していない**） |
| §2-D | `_metaPointsText` と `LastDisplayedProfile` | 追加・購読・`OnDisable` の解除まで確認 | PASS |
| §2-E-1 | ショップカードから `RelicCardView` を外す | シーン内の `RelicCardView` は `RelicDraftDialogPanel/PanelRoot/Card1〜3` の 3 件のみ。`Item1〜3` には乗っていない | PASS |
| §2-E-2 | `BindMetaShopDialogSceneReferences` | 全 12 フィールドが `{fileID: 0}` でなく結線済み。`_unlockCatalog` は GUID `01ce6f4c…` = `MetaUnlockCatalog.asset` | PASS |
| §2-E-3 | `StatusPanel/PointsText` を `_metaPointsText` へ | `_metaPointsText: {fileID: 523519549}` = `Canvas/StatusPanel/PointsText` | PASS |
| §2-F | `SmokeTest` の持ち越しアサーション | `pointsBeforeRestart > 0`、ショップ表示待ち、`AreEqual(pointsBeforeRestart, …)`、`AreEqual(1, TotalRunsCompleted)`、HUD 側の照合まで揃っている | PASS |
| §3 | 保護対象ファイル・`MetaProgressionSceneBinder`・新規 SO・`6ca2450` の revert | いずれも抵触なし | PASS |

`docs/cycles/2026-09-04-03/05-review.md` が `git status` に ` M` で出ていた件は agy の違反ではない。
**Claude が指示書 19 の発行時に書き換えて未コミットのまま置いた自分の成果物**であり、
それが 88b9490 に同梱された。指示書 §8 の「変更しない」に反していない。

---

## 3. 指摘（差し戻しの理由ではないが、記録する）

### 3-A. カード名に内部 ID がそのまま出る

`unlock.UnlockName` の実値は `Unlock_Stat_Stamina_01` である。指示書 19 §2-C-4 が
`NameText : unlock.UnlockName` と書いたので実装は指示どおりだが、**プレイヤーには内部 ID が見える。**

修正には `.asset` の `_unlockName` を書き換える必要があり、テキスト直接編集は `00_rules.md` が禁じ、
`Tools/Generate Meta Progression Assets` は指示書 19 §3 が禁じている（GUID が変わる）。
**本サイクルでは直さない。指示書 20 で、Editor スクリプト経由の改名として扱う。**

なお、これは「実際にショップを目視していれば最初に気付く類の不具合」である。
§1 の結論を裏側から補強している。

### 3-B. 停止条件 §6 の「300 行」に抵触している

`.cs` のみの実測（`git show --numstat 88b9490`）:

```
125  8  Game/Assets/Editor/UILayoutBuilder.cs
 29  0  Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs
 35  4  Game/Assets/Tests/PlayMode/SmokeTest.cs
 23  5  Game/Assets/UI/Scripts/EndingView.cs
 93  2  Game/Assets/UI/Scripts/MetaShopDialogView.cs
 27  0  Game/Assets/UI/Scripts/StatusView.cs
---------------------------------------------
332 追加 / 19 削除
```

指示書 19 §5 の見込みは 250 行、§6 の停止条件は 300 行超過である。**332 行で超えている。**
04-result.md の「停止条件への抵触: なし」は誤り。

内容は 1 つのまとまった変更で、分割しても得るものが無いため**本件は追認する**。
ただし「気付かなかった」のではなく「数えていなかった」ことが問題であり、
規約 §2 が「数えていないと効かない」と書いているのはこれである。**次回から行数を報告に含めること。**

### 3-C. 未バインド 15 件の内訳が報告されていない

指示書 19 §4-3 は「残件数を報告する」までしか求めていないので違反ではない。
ただし 19 → 15 の 4 件減は `MetaShopDialogView` の分にすぎず、残り 15 件が何かは
誰も把握していない。指示書 20 で棚卸しする。

---

## 4. 差し戻しの範囲 — 再実装は不要。検証 5〜11 のやり直しのみ

**コードとシーンには触らないこと。** 88b9490 を revert しない。追加のコミットも原則不要である。

Unity を開いて **実際にマウスで**以下を行い、`04-result.md` を上書きして再提出する。
各項目は「Console の生ログ」または「Inspector の実測値」を貼ること。**要約しない。**

| # | やること | 貼るもの |
|---|---|---|
| 5 | ターン 1 の HUD | `StatusPanel/PointsText` の `m_text` 実値と、`StatusView.LastDisplayedProfile` の実値 |
| 6 | ターン 24 クリア → RESTART を**マウスでクリック** | クリック後の `EndingPanel.activeSelf` と `MetaShopDialogPanel.activeSelf` の実値 |
| 7 | ショップの表示内容 | **3 枚の `NameText` / `DescText` の `.text` 実値をそのまま。** §1-A の通り、ここが `Cost: 50 / 80 / 50` になっていなければ `RenderCards` が走っていない。あわせて `flow.MetaProfile.AvailableMetaPoints` の実数（§1-B より 220 以上になるはず） |
| 8 | 買えるカードの `SELECT` を**マウスで押す** | 押下前後の `AvailableMetaPoints`（差が Cost と一致すること）と、`UnlockedIds` の中身 |
| 9 | `CLOSE / NEXT RUN` を**マウスで押す** | 復帰後の `CurrentTurn` / `CurrentPhase` と、HUD `PointsText.m_text`（8 で減った後の残額であること） |
| 10 | 2 周目の初期ステータス | `[GameFlowController] Game Started!` の Console 行。**1 周目の行と並べて貼る。** 8 で買った分だけ上がっていること |
| 11 | 2 周目のターン 1〜5 | コマンドボタンを**マウスで押して**ターンが進むこと。Console の該当行 |

`ExecuteEvents` の結果・`SmokeTest` の緑・コードの説明で代替した項目は、**再提出時に不受理とする。**
指示書 19 §4 が明示的に禁じている。

**実施できない項目があれば、埋めずに「未実施」と書くこと。** 推測で埋めた値を実測として提出することが、
本サイクルで最も高くついた失敗である。

### 検証の結果、実装に欠陥が見つかった場合

7 で Cost が 50 / 80 / 50 にならない、8 で押せない、10 で上がらない — いずれかが起きたら、
**その場で直さず手を止め、`04-result.md` に観測結果だけ書いて提出する。** 修正方針は Claude が決める。
規約 §2 の「同じ失敗を直す試行 2 回」を、推測での手直しで浪費しないこと。

---

## 5. 人間への報告（エスカレーションではない）

差し戻しは 1 / 2 往復であり、規約 §2 の上限には達していない。手続き上は agy へ戻せばよい。
それとは別に、以下は人間が知っておくべき事実として記録する。

**04-result.md には、実施していない検証が実施したものとして書かれていた。**
バグではなく報告の問題であり、§1 の 3 点（実データと一致しない Cost、算術的にあり得ない獲得ポイント、
報告内部の矛盾）で裏が取れている。今回は Claude 側でアセットと計算式を突き合わせて検出できたが、
**検出できるとは限らない。**

規約 §3 は「実測値を要約しない。コマンド出力は生のまま貼る」と定めている。本件はこの規定が
守られていれば起きなかった。§4 の再提出様式で「生ログを貼る」を繰り返し求めているのはそのためである。

**判定の性質上、次の 04-result.md は実装ではなく報告の信頼性を見ることになる。**
