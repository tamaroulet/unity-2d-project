# 指示書 25: 人間不在中に進めてよい作業一覧

```
ROLE: Executor
BRANCH: main
判断: 人間との直接協議（外出中の無人実行）
性質: 新規の設計判断は含まない。既に裁定済みの作業を順に消化するだけの束である
前提: main = 1bdcbc9。作業ツリーはクリーン。機械判定は全 PASS（Changed lines を除く）
```

## 最重要 — 人間が居ないときの原則

**判断が必要になったら、実装せずに止めて記録する。** これが本書で一番大事な行である。

- 指示書に書かれていない設計判断が必要になったら、**その場で手を止める**
- 同じエラーの修正を 2 回試みて直らなかったら、**その場で手を止める**
- 「たぶんこうだろう」で進めない。**推測で埋めた値を実測として報告しない**
- 止まったときは、後続のタスクへ飛ばず、**そこで本書の消化を終える**

止まった内容は `docs/handoff/2026-09-05-05/04-result.md` に、
**観測した事実だけ**を書く。原因の推測は書かない。

---

## タスク（この順で消化すること）

### 1. `--resume` の動作確認（指示書 22 検証 5・6）

Claude セッション枠はリセット済みのはずである。最初にこれをやる。
通れば以降の Claude 消費が下がるため、先にやるほど得になる。

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
Remove-Item logs/claude_session_id.txt
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
```

- [ ] 1 回目で `logs/claude_session_id.txt` が作られること
- [ ] 2 回目が `--resume` で走ること
- [ ] 3 回目（ID 削除後）がフォールバックで**止まらずに**走ること
- [ ] 3 回分の標準出力をそのまま `docs/handoff/2026-09-05-03/04-result.md` に追記してコミット

**枠が再び 85% を超えて遮断されたら、それはそれで正常である。** `-Force` は使わない。
`[BLOCKED]` の出力をそのまま貼って、このタスクは「枠待ち」として記録し、2 へ進む。

### 2. 機械判定の欠陥 2 件を直す（`scripts/mechanical_check.ps1` のみ）

`docs/handoff/2026-09-05-03/05-review.md` §3-A / §3-B の裁定に従う。

- [ ] **追加行のみを 300 行の判定対象にする。** 削除行は併記に留める。
      理由: 指示書 23 は撤去を命じたのに `FAIL Changed lines: 433 (added 54 / deleted 379)` になった。
      指示どおりに消したら FAIL になるのは判定側の誤りである
- [ ] **`Snapshot` に差分があるときは `PASS` ではなく `SKIP`** とし、行数と内訳を併記する。
      理由: 現在「差分あり」でも `PASS` と出て読み手を誤らせる
- [ ] 修正後に 2 回連続実行して同じ出力になることを確認し、生出力を貼る

### 3. 回収した 2 つの PlayMode テストを実行する

`1bdcbc9` で `RelicDraftFlowTest.cs` と `SerializedBindingTest.cs` を main に回収した。
コンパイルは通っているが、**まだ 1 度も実行していない。**

- [ ] PlayMode テストを実行し、件数と結果の生出力を貼る
- [ ] `RelicDraftFlowTest` が緑なら、`docs/instructions/12_binding_and_test_structure.md` の
      98 行目の箱を `- [x]` にして注記を更新する
- [ ] `SerializedBindingTest` が赤い場合、**それは想定内である。**
      未結線が 15 件残っているので落ちて当然。**直さないこと。**
      落ちたフィールドの一覧をそのまま貼る（指示書 12 項目 64 が求めていたのはこれである）
- [ ] `RelicDraftFlowTest` が赤い場合は、**1 回だけ**原因を調べてよい。
      直らなければ箱は開けたまま、観測結果を貼って次へ

### 4. `pm1_opus_review.py` の検証 9 を追記する

`docs/handoff/2026-09-05-04/05-review.md` §4-A の指摘。

- [ ] 枠が 85% を超えている状態で `python scripts/pm1_opus_review.py` を実行し、
      **何もせず終了すること**の生出力を `docs/handoff/2026-09-05-04/04-result.md` に追記
- [ ] 枠が 85% 未満で試せない場合は「未実施（枠が閾値未満のため再現できず）」と書く。
      **枠を意図的に使って再現しようとしないこと**

### 5. 指示書 24 の実装（コードを書くところまで）

`docs/instructions/24_metaprofile_persistence.md` の §1-A〜E をそのまま実装する。
**設計は全部あちらに書いてある。判断を足さないこと。**

- [ ] §1-B `MetaProfileDto.cs` を新規作成
- [ ] §1-C `MetaProfileStore.cs` を新規作成
- [ ] §1-D `GameFlowController` の 3 箇所（`Awake` / `FinalizeRun` / `TryPurchaseMetaUnlock`）
- [ ] §1-E `Tools/Clear Meta Profile` を Editor に追加
- [ ] §3 検証 2・3 の EditMode テスト 2 件を追加（DTO 往復、壊れた JSON / 版違い）
- [ ] §3 検証 1（`mechanical_check.ps1`）、検証 9（スナップショット差分 0）、検証 10 を実施

### ここで止まること

**検証 4〜8 は人間の目視とマウス操作が要る。手を出さない。**

- 検証 4〜7（Play しての実機確認）
- 検証 8（WebGL ビルドとブラウザでのリロード確認）

`04-result.md` に「4〜8 は人間待ち」と明記して、そこで本書の消化を終える。

---

## やってはいけないこと

```
docs/instructions/*.md（本書と 12 の箱の更新を除く）  README.md  docs/spec/**
docs/decisions/**  .agents/rules/00_rules.md  .claude/hooks/guard.js
.github/workflows/**  scripts/handoff.ps1  scripts/invoke_claude_safe.ps1
scripts/nightly_gate.py  scripts/auto_runner.py  Game/Packages/manifest.json  .mcp.json
```

- **`docs/handoff/*/05-review.md` を書かない。** レビューは Claude の役割である
  （cycle 2026-09-04-03 で一度違反している）
- `.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。**`.unity` を読まない**
- `git stash drop` / `git branch -D` / `git reset --hard` を実行しない。
  `auto/wip` と `stash@{0}` はそのまま残す
- `git push` しない
- `-Force` で枠の遮断を突破しない
- スケジュールタスクを有効化しない（`UnityProject_AutoRunner` は意図的に無効である）
- 新規 `EventChannelSO` / 新規 View / 新規 asmdef を作らない
- `EventResolverSO` の発火条件に手を出さない（指示書 26 で扱う。調査もしない）
- テストを通すためにプロダクションコードへ分岐や自己修復を足さない

---

## 報告

`docs/handoff/2026-09-05-05/04-result.md` に 1 本にまとめる。
先頭に `## 機械判定`（`mechanical_check.ps1` の生出力）を置く。

```
指示書: docs/instructions/25_unattended_batch.md
消化: N / 5

## 機械判定
（生出力）

## タスク 1〜5 の結果
（各タスクの生出力。要約しない）

## 止まった箇所
なし / あり（どのタスクの何行目で、何を観測して止めたか。原因の推測は書かない）

## 人間待ちとして残したもの
（指示書 24 の検証 4〜8 など）
```

タスクごとにコミットする。**push はしない。**
1 コミットが 300 行（追加行）を超えたら、そこで分割する。
