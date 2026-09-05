# 指示書 12: UI 結線の機械的検証とテスト構造の是正（Phase 2）

```
ROLE: Executor
CONSTRAINT: NO_ARCH_CHANGE / NO_PROTECTED_FILE_EDIT / NO_ALTERNATIVE_PROPOSAL
RULES: .agents/rules/00_rules.md
BRANCH: main
```

由来: Architect（Claude）のアーキテクチャ判断（2026-09-04・人間承認済み）の Phase 2。
前提となる監査は [`docs/research/Audit-02_harness_audit.md`](file:///c:/dev/unity-2d-project/docs/research/Audit-02_harness_audit.md)。

---

## 0. 確定済みの設計方針（議論の対象ではない）

以下は人間ディレクターの承認を得た確定事項である。**再提案・異議・代替案の提示を禁止する。**

| 技術 | 判断 |
|---|---|
| VContainer / DI コンテナ | **却下**（`00_rules.md` で禁止。痛点はシーンのシリアライズであり依存解決ではない） |
| UI Toolkit / Rosalina | **Phase 4 へ先送り**。本指示書では着手しない |
| SMT 形式検証 / 純粋 ECS / RL-QA | **却下**（規模が合わない） |
| AltTester | **却下**（PlayMode SmokeTest で足りる。CI 分数を食う） |

「UI Toolkit に移行したい」等を思いついた場合、実装せず、
コミットメッセージ末尾に 1 行書いて終わりにすること。

## 1. 触れてはならないファイル

1 行でも変更したら成果物ごと `nightly-reject/<timestamp>` へ隔離される。

```
.github/workflows/**
.claude/hooks/**  .claude/settings.json
.agents/rules/**
scripts/nightly_gate.py  scripts/morning_report.py
scripts/nightly_baseline.json  scripts/auto_runner.py
Game/Packages/manifest.json  Game/Packages/packages-lock.json
```

- **新しいパッケージを追加してはならない。** `manifest.json` は保護対象である。
- **新しい `.asmdef` を作ってはならない。** 新設は violation として隔離される。
- **`.md` の新規作成は `docs/` 配下のみ。** `Game/` や `scripts/` に作業メモを置かない。

---

## 2. [ADD] UI 結線の網羅性を PlayMode で機械検証する ★最重要

### 背景

`EventDialogView` の `_titleText` / `_bodyText` / `_okButton` / `_gameFlowController` が
シーン上で未結線のまま残っている。根本原因は
「`UILayoutBuilder.cs`（547 行）のどこが結線済みでどこが未結線か、人間にも AI にも
分からない」ことであり、Inspector で 4 つ挿すだけでは同じことが再発する。

`00_rules.md`「結合の正しさは PlayMode テストでのみ証明する」に従い、
**未結線を機械的に検出できる状態**にする。この検証は Phase 4 で UI Toolkit へ
移行しても残る（UXML でも要素の解決失敗は検出したい）ため、捨てにならない。

### タスク

- [x] `Game/Assets/Tests/PlayMode/SerializedBindingTest.cs` を新規作成する。`MainGame.unity` をロードし、シーン上の全 View コンポーネント（`Game.UI` 名前空間の `MonoBehaviour`）について、`[SerializeField]` 属性が付いた全フィールドが `null` でないことを reflection で検証する `[UnityTest]` を書くこと。失敗時は「どのコンポーネントのどのフィールドが未結線か」がテスト名とメッセージから一意に分かるようにすること。 **→ 実ファイルを回収済み（2026-09-05）。あわせて指示書 21 の `Tools/Generate Scene Snapshot` が `docs/snapshot/scene_bindings.txt` に同じ情報を出す。**
- [x] 上記テストで現在検出される未結線を一覧化し、**修正はせずに** 報告する。`.unity` はテキスト編集禁止であり、Inspector での結線は人間の作業である（`00_rules.md`「役割」）。エージェントは検出までを担当する。 **→ 同上。スナップショットと `Tools/Report Unbound Serialized Fields` が一覧を出す。**

### 実装上の制約

- `Game/Assets/Tests/PlayMode/` 配下に置くこと。`Tests/` 直下（EditMode）に置くと
  `new GameObject` / `AddComponent` の lint に当たり隔離される。
- 既存の `SmokeTest.cs` と同じ流儀（`SceneManager.LoadScene` + `yield return null`）に揃えること。
- 未結線が見つかってもテストを弱めないこと（`[Ignore]` や `Assert.Pass` は隔離対象）。
  赤は情報である。

---

## 3. [FIX] EditMode 純粋性違反の是正（案B）

### 背景と確定方針

`00_rules.md`「テスト」は EditMode を純粋計算に限っている。実測で 4 ファイルが違反していた。
人間の承認により **案B** を採用した。

| ファイル | 判断 |
|---|---|
| `GameFlowControllerTests.cs` | **例外として許可**（`00_rules.md` に明文化済み） |
| `GameFlowControllerRelicTests.cs` | **例外として許可**（同上） |
| `GameMonteCarloSimulationTests.cs` | **例外として許可**（同上） |
| `RelicDraftDialogViewTests.cs`（未追跡） | **破棄して PlayMode で作り直す** |

`RelicDraftDialogViewTests.cs` を破棄する理由は 2 つ。
`new GameObject` + `AddComponent<RelicDraftDialogView>` が 10 箇所あり View の
組み立てテストになっていること、および reflection で `RelicSO` の private フィールドを
直接書き換えており**実装のフィールド名を変えた瞬間に静かに壊れる**こと。

### タスク

- [x] 未追跡ファイル `Game/Assets/Tests/RelicDraftDialogViewTests.cs` と `Game/Assets/Tests/RelicDraftDialogViewTests.cs.meta` を削除する（`git` の追跡対象ではないため `rm` でよい）。
- [x] `Game/Assets/Tests/PlayMode/RelicDraftFlowTest.cs` を新規作成し、削除したテストが担保していた 3 点を PlayMode で書き直す。検証内容は「ボス撃破後に RelicDraft パネルが表示される」「カード選択で `RelicAcquiredChannelSO` が発火する」「未所持レリックのみが候補に出る」。**`AddComponent` でモックを組まず、実シーン `MainGame.unity` の実コンポーネントを操作すること。** **【Architect 注記 2026-09-05】PlayMode で実行し緑を確認（2026-09-05）**
- [x] `Game/Assets/Tests/` 直下のテストファイルを目視で確認し、`new GameObject` / `AddComponent` を使っているものが `00_rules.md` の例外 3 枚以外に無いことを確認する。あれば**修正せず報告する**（設計判断が必要なため）。

---

## 4. [FIX] auto/wip からのゲームコード部分回収

### 背景 ★重要

`auto/wip` には 8 コミット・2,805 行の差分がある。しかしその中には
`scripts/nightly_gate.py` / `auto_runner.py` / `nightly_baseline.json` /
`.agents/rules/00_rules.md` の**古い版**が含まれている。

> **`git merge auto/wip` を実行してはならない。**
> `main` の Audit-02 Part A 修正（ベースライン自己ロック解除・誤検知除去）が
> すべて巻き戻る。

ゲームコードのみを cherry-pick で回収する。

### タスク

- [x] `git cherry-pick 2583e28` を実行し、GameFlowController の RelicDraft 配線（+91 行）と `RelicDraftDialogView.cs`（+43 行）を `main` へ回収する。コンフリクトが出た場合、**保護対象ファイル側は必ず `main` の内容を採用する**（`git checkout --ours <path>`）。
- [x] cherry-pick に `RelicDraftDialogViewTests.cs` が含まれていた場合、そのファイルだけを `git rm --cached` で外してから commit する（§3 で破棄する対象のため）。
- [x] `d433269`（UILayoutBuilder + MainGame.unity のスプライト割当）については、**cherry-pick せず、対象範囲と差分行数のみ報告する**。`MainGame.unity` の差分が 2,527 行あり、`00_rules.md` により人間の目視確認が必要なため、実施可否は人間が判断する。
- [x] `805b604` / `2db418f` / `7552a2b` / `5628d55` は回収しない。`main` の方が新しいか、wip コミットであるため。

---

## 5. 検証（実コマンドの出力を報告に貼る）

| # | コマンド | 期待 |
|---|---|---|
| 1 | `ls Game/Assets/Tests/RelicDraftDialogViewTests.cs` | 存在しない（No such file） |
| 2 | `ls Game/Assets/Tests/PlayMode/` | `SmokeTest.cs` / `SerializedBindingTest.cs` / `RelicDraftFlowTest.cs` の 3 本 |
| 3 | `git log --oneline -3` | `2583e28` 由来の cherry-pick が積まれている |
| 4 | 下記 Python | `違反 0 件` |
| 5 | `git status --porcelain` | **保護対象ファイルが 1 件も現れない** ★必須 |
| 6 | `git diff --stat main@{1}..HEAD` | 300 行未満（`00_rules.md` 停止条件） |

検証 4 のコマンド（EditMode 純粋性の自己チェック）:

```bash
python -c "
import sys,pathlib; sys.path.insert(0,'scripts')
import nightly_gate as g
SEP=chr(92); bad=[]
for p in sorted(pathlib.Path('Game/Assets/Tests').rglob('*.cs')):
    path=str(p).replace(SEP,'/')
    if path.startswith(g.TEST_PREFIX+'PlayMode/') or path in g.EDITMODE_PURITY_ALLOWLIST: continue
    t=p.read_text(encoding='utf-8')
    if any(pat.search(t) for pat,_ in g.EDITMODE_PURITY_RULES): bad.append(path)
print('違反', len(bad), '件'); [print(' -',b) for b in bad]
"
```

**Unity テストの実行は Unity エディタを閉じた状態で行うこと。**
開いていると Library 排他ロックでバッチモードが必ず失敗する。

---

## 6. 停止条件

以下に該当したら手を止めて報告する（`00_rules.md`「停止条件」）。

- 保護対象ファイル（§1）を変更する必要が生じた
- cherry-pick のコンフリクトが保護対象ファイル以外で発生し、解決に設計判断が必要になった
- 同じエラーの修正を 2 回試みて直らない
- `git diff --stat` が 300 行を超えた
- `.unity` / `.prefab` / `.asset` を編集しないと先へ進めなくなった（人間の作業である）

## 7. 完了報告

`docs/workflow/TRIAD_PROTOCOL.md` §2-④ の形式に従う。

```
指示書: docs/cycles/_unpaired/12_binding_and_test_structure.md
実施: Tasklist 2/3/4 完了（または N で停止）
--- コマンド出力 ---
（§5 の検証 1〜6 の生の出力をそのまま貼る）
---
未結線として検出された [SerializeField] の一覧:
（§2 の 2 番目のタスクの結果）
---
停止条件への抵触: なし / あり（内容）
```

自然言語の進捗宣言は報告として無効。**コマンド出力とコミットハッシュのみが進捗である。**

コミットのみ行い、**push はしない**。

---

## 8. スコープ外

| 項目 | 理由 |
|---|---|
| Inspector での結線作業 | 人間の役割（`00_rules.md`「役割」）。エージェントは検出まで |
| `MainGame.unity` の編集 | テキスト編集禁止。`d433269` の回収可否も人間が判断 |
| `UILayoutBuilder.cs` のリファクタ | Phase 4（UI Toolkit 移行）で構造ごと見直す。今触ると二度手間 |
| UI Toolkit / VContainer の導入 | §0 のとおり確定で却下・先送り |
| テストのフォルダ細分化 | まず純粋性違反を解消する。分類はその後 |
