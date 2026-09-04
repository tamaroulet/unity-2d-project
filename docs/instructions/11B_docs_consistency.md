# 指示書 11-B: 保護領域外の整合性回復（Gemini 担当）

```
ROLE: Executor
CONSTRAINT: NO_ARCH_CHANGE / NO_PROTECTED_FILE_EDIT
RULES: .agents/rules/00_rules.md
BRANCH: main
```

由来: [`docs/research/Audit-02_harness_audit.md`](file:///c:/dev/unity-2d-project/docs/research/Audit-02_harness_audit.md)
（自律開発ハーネス全体監査）の Part B。Part A（ハーネス本体の修正）は実施済み。

---

## 0. 前提（作業開始前に必ず読む）

- 作業ブランチは **`main`**。開始前に `git branch --show-current` で確認する。
  `auto/wip` には未合流のコミットが 8 本あるが、本指示書では触らない。
- **ハーネス修正（Part A）は既に `main` に入っている**（`3d7f251` / `bf4c7d5`）。
  `scripts/nightly_gate.py` / `scripts/auto_runner.py` /
  `scripts/nightly_baseline.json` / `scripts/register_scheduled_task.ps1` /
  `.gitignore` は修正済みなので、**再度直さない**。
- `docs/log.md` への Audit-02 の記録も**既に書かれている**。重複して書かない。
- 本指示書の変更対象に保護対象ファイルは 1 つも含まれない。

## 1. 触れてはならないファイル

1 行でも変更したら成果物ごと `nightly-reject/<timestamp>` へ隔離される。

```
.github/workflows/**
.claude/hooks/**  .claude/settings.json
.agents/rules/**
scripts/nightly_gate.py
scripts/morning_report.py
scripts/nightly_baseline.json
scripts/auto_runner.py
```

上記に修正が必要だと判断した場合、**自分で直さず作業を止めて報告する。**

---

## 2. [FIX] docs/workflow/ONBOARDING.md を実態に合わせる

Part A のハーネス修正により、以下の記述が現在の挙動と食い違っている。
初見の AI がこれを読むと誤った前提で動くため訂正する。

- [x] `docs/workflow/ONBOARDING.md` 56 行目「**EditMode テスト（127件）**」を実測値に訂正する。現在の実測は `total 116 / passed 97 / skipped 19`（skipped 19 は `[Explicit]` による恒久 skip で `scripts/nightly_baseline.json` の `EditMode_known_skipped` が許容している）。件数は将来また変わるため、数値の後ろに「（実測値。下限は `scripts/nightly_baseline.json` が持つ）」と補記すること。
- [x] `docs/workflow/ONBOARDING.md` 75 行目「毎日 01:00〜06:00 の夜間にのみ **30 分間隔で稼働**」を訂正する。正しくは「毎日 01:00 に 1 回だけ起動し、`auto_runner.py` が内部で `AUTO_RUN_END_HOUR`（既定 6）までループする」。Task Scheduler 側の反復と `RestartCount` は多重起動の原因だったため Part A で撤去済み。
- [x] `docs/workflow/ONBOARDING.md` 77 行目「全成果物を `nightly-reject/<timestamp>` ブランチへ保全した上で **`main` を自動ロールバック**する」を訂正する。実際の作業ブランチは `auto/wip` であり、巻き戻す対象も `auto/wip` である。`main` は汚さない。
- [x] `docs/workflow/ONBOARDING.md` 78 行目「開いていると Library 排他ロックにより `UNVERIFIED` で **全巻き戻し**になる」を訂正する。Part A で挙動を変更し、現在は**巻き戻さず作業ブランチ上に保留**して `auto_runner` が中断する。「エディタを閉じてから再開すればそのまま再検査できる」旨に書き換えること。

## 3. [FIX] docs/workflow/TRIAD_PROTOCOL.md §4 の前提を訂正

- [x] `docs/workflow/TRIAD_PROTOCOL.md` の §4「Fix Gate Protocol との関係」を「安全ハーネスとの関係」に改める。Fix Gate Protocol は commit `f71745a` で散文規約としては廃止され、`scripts/nightly_gate.py` の判定コード（ポリシー検査・テスト判定・連続 REJECT 停止）へ移された。本文は「規約 §2 の ①② が上流確認、③④ が実装、ハーネスの verdict が下流検証にあたる」という対応で書き直すこと。**散文の Gate 1〜4 を復活させてはならない**（`00_rules.md`「手続きは書かない／文章で守らせるのではない」に反する）。

## 4. [FIX] docs/research/workflow_research.md の破損修復

このファイルは編集途中で壊れている。

- [x] `docs/research/workflow_research.md` §3.1 のテーブルの分断を修復する。`| 設計と実装の分離 |` の次に空行が入っており、`| 検証ゲート |` と `| 敵対的レビュー |` が別テーブルになっている。空行を除去して 1 つのテーブルに戻すこと。
- [x] `docs/research/workflow_research.md` §3.2 の「6 段階ワークフローは…」を「旧ルール（現 `docs/archive/rules_v1_34kb/`）は新規機能実装を前提としており」に書き換える。6 段階ワークフローは廃止済みで現存しない。
- [x] `docs/research/workflow_research.md` §4 に実際の対処を追記する。現在 1 行しかない。Audit-02（`docs/research/Audit-02_harness_audit.md`）でゲートの誤検知が却下の主因と判明し、散文規約ではなく判定コード側を修正した経緯を書き、同レポートへリンクすること。
- [x] `docs/research/workflow_research.md` の §5「情報源」を復活させる。現在この節ごと削除されている。復活させたうえで §2 の各主張の根拠となる URL を実際に列挙すること。**URL を提示できない主張は、その主張ごと §2 から削除する**（`00_rules.md`「外部一次情報の最重視」「推測や捏造でコードを書かない」）。

## 5. [FIX] docs/STATUS.md のコミットハッシュ欠落

進行状況テーブルの 6 箇所（24 / 29 / 30 / 31 / 32 / 36 行目）でコミット列が
`HEAD` になっている。`HEAD` は時間とともに指す先が変わるため記録として機能しない。

- [x] `docs/STATUS.md` の進行状況テーブルにある 6 箇所の `` `HEAD` `` を、`git log --oneline --all` で特定した実コミットの 7 桁 SHA に置換する。特定できない Step は `HEAD` ではなく `-` を入れ、特定できなかったことを明示する。**推測で SHA を書かないこと。**

## 6. [FIX] scripts/*.ps1 に UTF-8 BOM を付与

`scripts/*.ps1` は BOM 無し UTF-8 で保存されている。Windows PowerShell 5.1 は
BOM が無い場合に cp932 として読むため、日本語コメントが文字化けする。
実際 `register_scheduled_task.ps1` では、日本語コメントを `param()` の前に
追加した際に化けたバイト列が構文を壊し parse error になった（同ファイルは対処済み）。

対象は BOM 無しの 7 本:
`copy_webgl_to_docs.ps1` / `get_all_quotas.ps1` / `get_claude_quota.ps1` /
`get_gemini_quota.ps1` / `invoke_claude_safe.ps1` /
`register_morning_report_task.ps1` / `run_claude_consult.ps1`

> **警告**: ファイルを開いて書き直してはならない。**先頭 3 バイトを付け足すだけ**にする。
> 特に `invoke_claude_safe.ps1` は `--dangerously-skip-permissions` を含むため、
> 全文を書き直すと diff 上その行が「新規追加」と見なされ、ハーネスの
> `ABUSE_RULES` に当たって成果物ごと隔離される。

- [x] `scripts/*.ps1` の 7 本に UTF-8 BOM を付与する。**ファイルを開いて書き直さず、下記コマンドで先頭 3 バイトを付け足すだけにすること**（`invoke_claude_safe.ps1` を全文書き直すと `--dangerously-skip-permissions` が新規追加行と判定され隔離される）。

```bash
python -c "
import pathlib
for p in sorted(pathlib.Path('scripts').glob('*.ps1')):
    b = p.read_bytes()
    if not b.startswith(b'\xef\xbb\xbf'):
        p.write_bytes(b'\xef\xbb\xbf' + b)
        print('BOM added:', p.name)
"
```

---

## 7. 検証（実コマンドの出力を報告に貼る）

| # | コマンド | 期待される出力 |
|---|---|---|
| 1 | `grep -c '\`HEAD\`' docs/STATUS.md` | `0` |
| 2 | `grep -c '127件' docs/workflow/ONBOARDING.md` | `0` |
| 3 | `grep -c '^## 5' docs/research/workflow_research.md` | `1` 以上 |
| 4 | `grep -c 'http' docs/research/workflow_research.md` | `1` 以上 |
| 5 | 下記 PowerShell | 全行 `BOM=True  parseErrors=0` |
| 6 | `git status --porcelain` | **保護対象ファイルが 1 件も現れない** ★必須 |
| 7 | `git diff --stat` | 合計 3,000 行未満 |

検証 5 のコマンド:

```powershell
Get-ChildItem scripts\*.ps1 | ForEach-Object {
  $e=$null
  $null=[System.Management.Automation.Language.Parser]::ParseFile($_.FullName,[ref]$null,[ref]$e)
  $b=[System.IO.File]::ReadAllBytes($_.FullName)
  $bom=($b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
  "{0,-38} BOM={1,-5} parseErrors={2}" -f $_.Name,$bom,$e.Count
}
```

## 8. 停止条件

以下に該当したら手を止めて報告する（`00_rules.md`「停止条件」）。

- 保護対象ファイル（§1）を変更する必要が生じた
- 同じエラーの修正を 2 回試みて直らない
- 指示書に書かれていない設計判断が必要になった
- `git diff --stat` が 300 行を超えた

## 9. 完了報告

`docs/workflow/TRIAD_PROTOCOL.md` §2-④ の形式に従う。
自然言語の進捗宣言（「うまくいきました」）は報告として無効。

```
指示書: docs/instructions/11B_docs_consistency.md
実施: 2/3/4/5/6 完了（または N で停止）
--- コマンド出力 ---
（§7 の検証 1〜7 の生の出力をそのまま貼る）
---
停止条件への抵触: なし / あり（内容）
```

コミットのみ行い、**push はしない**。

---

## 10. スコープ外（やらないこと）

| 項目 | 理由 |
|---|---|
| `docs/instructions/` から `git push origin main` を除去する | Part A の C-2 修正で `.md` は `ABUSE_RULES` の検査対象外になった。かつ実測 0 件で対象が存在しない |
| `docs/log.md` への Audit-02 の記録 | Part A で記録済み。重複させない |
| `.agents/rules/00_rules.md` への手続き（Gate 1〜4 等）の追記 | 同ファイル冒頭の「手続きは書かない／文章で守らせるのではない」に反する |
| `docs/nightly/*.md` の編集 | `scripts/morning_report.py` の生成物。手で触らない |
| 隔離ブランチ `nightly-reject/*` の回収・削除 | 人間の判断事項 |
