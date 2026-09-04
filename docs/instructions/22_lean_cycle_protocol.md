# 指示書 22: サイクル手順の省エネ化（品質を落とさずに出力を 1/3 にする）

```
ROLE: Executor
BRANCH: main
判断: 人間との直接協議。02-context.md なし
例外: 本サイクルに限り scripts/handoff.ps1 と scripts/invoke_claude_safe.ps1 は保護対象から外す
```

## 目的

直近 3 サイクルの実測。

```
88b9490 の C# 変更                          332 行
docs/instructions/19_...md                6,300 tok（Architect の出力）
docs/handoff/2026-09-04-04/05-review.md   7,900 tok（Architect の出力）
docs/handoff/2026-09-04-04/03-instruction.md  ─ 19 と内容が重複
```

**332 行のコードを直すために、Architect が 14,000 トークン以上書いている。**
しかも同じ判断を `03-instruction.md` と `docs/instructions/NN_*.md` に 2 回書いている。

一方、この 3 サイクルで実際に品質を生んだのは次の 3 件である。

| 何を見つけたか | 読んだ量 |
|---|---|
| `RestartGameChannelSO` が `00_rules.md` の禁止に当たる | 規約 1 行 |
| 既存 3 View が既に Controller を直接参照している（依頼書の前提が偽） | View 4 ファイルの 1 行ずつ |
| `04-result.md` の Cost 値がアセット実体と矛盾し、獲得ポイントが算術的にあり得ない | `.asset` 3 枚 + 計算式 1 本 |

**全部「少数の具体値の突き合わせ」で、量ではない。** 量を削っても品質は落ちない。
本サイクルはそれを手順として固定する。

---

## 実装方針

### A. 文書を 6 → 5 に。`03-instruction.md` を廃止する

`scripts/handoff.ps1` の `ask` は現在、`03-instruction.md` と `docs/instructions/NN_*.md` を
両方書かせている。**`docs/instructions/NN_*.md` の 1 枚だけにする。**

- `ask` のプロンプトから `03-instruction.md` の生成指示を外す
- 判断（採用案・却下理由）は指示書の冒頭セクションに統合する
- `docs/handoff/<日付>-<連番>/` に置くのは `01-request.md` / `02-context.md` / `04-result.md` /
  `05-review.md` の 4 枚

### B. 出力に上限を設ける

`ask` と `review` のプロンプトに、次を明記して渡す。

```
指示書は 150 行以内。レビューは 80 行以内。
主張は「結論 → 根拠 1 行（path:line）」の形で書く。同じ根拠を散文で言い直さない。
表が成立する内容は表にする。
```

### C. 機械判定を Gemini 側へ降ろす

`scripts/mechanical_check.ps1` を新規作成する。引数なしで走り、次を判定して表を標準出力へ出す。

| 項目 | 判定方法 |
|---|---|
| ビルド | `dotnet build Game/Game.sln -v q --nologo` の終了コードと警告数 |
| 変更行数 | `git diff --numstat HEAD -- '*.cs'` の合計。300 行超なら FAIL |
| 保護ファイル | `git status --porcelain` に保護対象が含まれたら FAIL |
| テスト件数 | `logs/*_results.xml` の `total` / `passed` / `failed` |
| スナップショット | `docs/snapshot/scene_bindings.txt` の差分（指示書 21 完了後のみ） |

出力形式は 1 行 1 項目、`PASS` / `FAIL` / `SKIP` を行頭に置く。**要約しない。生の数値を併記する。**

`04-result.md` の先頭に `## 機械判定` セクションを設け、この出力をそのまま貼る。

### D. レビューは毎サイクル行う。ただし機械判定 PASS の項目には触れない

**レビューの回数は減らさない。** cycle 04 の報告捏造は、レビューが無ければ通っていた。
削るのは Claude の再実行と再記述であって、判断ではない。

`review` のプロンプトに次を明記する。

```
## 機械判定 が PASS の項目は再実行も再記述もしない。「機械判定どおり」の 1 行で済ませる。
レビューが扱うのは次の 3 つだけ。
  1. FAIL の項目
  2. 「未実施」と書かれた項目
  3. 報告された値と、それが由来するアセット / 計算式との突き合わせ
```

### E. `claude -p` をやめてセッションを再利用する

`scripts/invoke_claude_safe.ps1:22`

```powershell
$null | & claude --dangerously-skip-permissions --model $Model -p $Prompt
```

これは毎回**新規セッション**で、プロンプトキャッシュが一度も効いていない。
`00_rules.md`（`trigger: always_on`）と `Game/AGENTS.md` だけで、
サイクルごとに 5,000 トークン弱を払い直している。

`--resume <session-id>` で同一セッションを引き継ぐ形にする。

- セッション ID は `logs/claude_session_id.txt` に保存する
- ファイルが無い、または `--resume` が失敗したときは `-p` で新規に立て、
  新しい ID を保存する（フォールバック必須。ここで止まらないこと）
- 既存の 85% 遮断（`:17-20`）は**そのまま残す**

### F. モデル指定の三重不一致を解消する

現状が割れている。**`opus` に揃える**（`00_rules.md` の移行条件 4 が未達のため）。

```
docs/STATUS.md                 「Architect 移行日: 2026-09-05」の行を削除
scripts/invoke_claude_safe.ps1:8   "sonnet" → "opus"
scripts/handoff.ps1:33             'opus' のまま（ステージ済みの差分をそのままコミット）
```

`scripts/handoff.ps1` の先頭に付いた BOM は、動作に影響しないため**そのままでよい**。

---

## やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/instructions/*.md（本書以外）  README.md  docs/spec/**  docs/decisions/**
.agents/rules/00_rules.md  .claude/hooks/guard.js  .github/workflows/**
scripts/nightly_gate.py  scripts/auto_runner.py  Game/Packages/manifest.json  .mcp.json
```

- **レビューの回数を減らさない。** 品質はここから出ている
- `05-review.md` を Gemini が書かない（`00_rules.md` 役割。cycle 03 で一度違反している）
- 機械判定の結果を Gemini が言い換えない。スクリプトの生出力をそのまま貼る
- `Game/Assets/` 配下の C# を 1 行も変更しない。本サイクルは手順だけである
- 過去の `03-instruction.md` を削除しない（記録として残す。廃止するのは今後の生成）
- `-p` の経路を消さない（`--resume` 失敗時のフォールバックとして残す）

---

## 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー（C# を触らないので変化なし） |
| 2 | `scripts/mechanical_check.ps1` を実行 | 表が出る。**生出力をそのまま貼る** |
| 3 | 同じ操作をもう 1 回実行 | 同じ出力になること（判定が実行ごとにぶれない） |
| 4 | `handoff.ps1 new` をダミー引数で実行 | `03-instruction.md` が**生成されないこと**。生成されたファイル一覧を貼る |
| 5 | `invoke_claude_safe.ps1` を短いプロンプトで 2 回実行 | 1 回目で `logs/claude_session_id.txt` が作られ、2 回目が `--resume` で走ること。標準出力を貼る |
| 6 | 同上、`logs/claude_session_id.txt` を消してから実行 | フォールバックで `-p` が走り、ID が作り直されること |
| 7 | `grep -n "Model = " scripts/*.ps1` と `grep -n "移行日" docs/STATUS.md` | 両方 `opus` に揃い、移行日の行が消えていること |
| 8 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

検証 5・6 は本サイクルの本体である。**失敗時に止まらないことまで確認すること。**

---

## 停止条件

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- `--resume` のセッション ID の取得方法が `claude` の出力から特定できない
  （**推測で実装しない。手を止めて人間に報告する**）
- `mechanical_check.ps1` の判定が実行ごとにぶれる
- 保護ファイルの変更が必要になった

---

## 報告

`docs/handoff/2026-09-05-03/04-result.md` に規約 §5 の様式で書く。
先頭に `## 機械判定`（C で作ったスクリプトの生出力）を置くこと。**本サイクルが初回の適用になる。**

コミットのみ行い、push はしない。

---

## Architect 側で別途行う規約改訂（Gemini は触らない）

本書が緑になった時点で、`00_rules.md` に次を追記する。私が行う。

- 1 サイクルの成果文書は 4 枚（`01` / `02` / `04` / `05`）。`03-instruction.md` は廃止
- 指示書 150 行、レビュー 80 行の上限
- レビューは機械判定 FAIL・未実施・値の突き合わせのみを扱う
