---
status: 確定（監査実施 2026-09-04 / Claude Opus）
scope: 自律開発ハーネス（nightly_gate.py / auto_runner.py / スケジューラ）と関連ルール文書
---

# 自律開発ハーネス 全体監査レポート（Audit-02）

実施日: 2026-09-04
実施者: Claude Opus（アーキテクト）
対象: `.agents/rules/`, `.claude/`, `scripts/`, `.github/workflows/`, `docs/`
方法: 全ファイル閲覧 + `logs/nightly/cycles-*.jsonl` の実測値照合 + 正規表現の実スキャン

---

## 0. 監査の発端

「修正が場当たり的になる」問題に対し、旧ルール（34KB 版）にあった
**Fix Gate Protocol（上流確認→修正計画→実装→下流検証の4段階ゲート）** を
再導入する改善案が提出された。

この改善案を検証したところ、**提案の前提が事実と食い違っていた**ため、
提案の妥当性検証から、ハーネス全体の監査へ範囲を拡大した。

---

## 1. 当初の改善案に対する検証結果

### 1.1 事実誤認

| # | 提案の主張 | 実態 |
|---|---|---|
| A-1 | `.agents/rules/00_role.md` と `10_workflow.md` を修正する | **両ファイルは存在しない。** commit `f71745a`（2026-09-03）で `docs/archive/rules_v1_34kb/` へ退避済み。現行ルールは `00_rules.md` 1枚（4.7KB） |
| A-2 | Fix Gate Protocol を新設する | **既に導入され、意図的に廃止されていた。** commit `5bc7a7b`「Fix Gate Protocol 導入」で `10_workflow.md` §1.5 に同一内容が存在。直後の `f71745a` でルールごと廃棄 |
| A-3 | 散文ルールとして追記する | `00_rules.md` 冒頭が明示的に禁止:「手続き（If-Then の対処法）は書かない」「文章で守らせるのではない」 |
| A-4 | `workflow_research.md` を新規作成する | 既存（2026-09-03 作成）。かつ内容が陳腐化（「`.agents/rules/` に 5 ファイル」等）、§5 情報源に URL が 1 本もない |
| A-5 | `docs/log.md` に `## 2026-09-03` を追加する | 既存（764 行目）。`## 2026-09-04` も既存（870 行目） |
| A-6 | `heal_with_opus` は Claude が自動修復・コミットしている | **呼び出し 0 件の死にコード。** 追加コミット `e478d3c` は「implement」と主張しているが未配線 |

### 1.2 提案どおりだと動かない設計上の問題

| # | 内容 |
|---|---|
| B-1 | Gate 1 が要求する `refresh_unity` / `run_tests` / `read_console` は Unity-MCP のツールだが、**本プロジェクトに Unity-MCP サーバは未登録**（`Game/.mcp.json` は `rider` のみ）。実行不能な手順を必須ゲートにすると「通ったことにする」経路が生まれる |
| B-2 | 「EditMode は 1 秒未満だからゲート強制は許容範囲か」という前提が誤り。テストは `nightly_gate.py` の Unity batchmode で実行され、`TEST_TIMEOUT_SEC = 1800`。かつ Unity エディタ起動中は Library ロックで必ず失敗する |
| B-3 | 改修対象の `.agents/rules/**` と `scripts/auto_runner.py` は `PROTECTED_PREFIXES`。auto_runner 経由で触れば無条件隔離される。**人間が `main` 上の対話セッションで行う必要がある**（提案の検証手順に記載なし） |

### 1.3 提案のうち採用したもの

- `heal_with_opus` の削除（死にコード）
- `should_use_claude` / Claude へのコーディング委譲経路の削除
- 「実装はすべて Gemini が担う」の 1 行明文化

---

## 2. 実測: 本日（2026-09-04）の自律実行 10 サイクル

`logs/nightly/cycles-20260904.jsonl` の 01:13〜09:31 の 10 サイクルより。

| verdict | 件数 |
|---|---|
| ACCEPT | **1** |
| REJECT_POLICY | 4 |
| REJECT_TESTS | 1 |
| ABORTED_DIRTY | 2 |
| AGENT_UNAVAILABLE | 2 |

**成功率 10%。** 同一タスク（`UILayoutBuilder.cs` 更新）で **4 連続却下**。
`nightly-reject/*` ブランチが 5 本、未処分で滞留。

そして**却下 5 件のうち 4 件はハーネス側の誤検知**だった（後述 C-2 / C-3）。

---

## 3. 致命的欠陥

### C-1. ベースライン自己ロック — 次に成功した瞬間パイプラインが永久停止する

`nightly_gate.py` の `evaluate_tests()` において、

- **保存側**: `max(baseline, results[platform]["total"])`
- **判定側**: `if record["passed"] < baseline[platform]`

実測値:

| 項目 | 値 |
|---|---|
| EditMode total | 116 |
| EditMode passed | 97 |
| EditMode skipped（`[Explicit]` 恒久） | 19 |

`[Explicit]` の 19 件は永久に passed にならないため、`total - passed = 19` は構造的。
次に ACCEPT が出た瞬間 `baseline = 116` が書かれ、以後すべてのサイクルが
`97 < 116` で REJECT_TESTS となり復帰不能になる。

**既に一度発火している**: commit `a121694 fix(harness): tolerate 19 known explicit
tests and update baseline to 94 passed` は、この事故の手動修理だった。
09:20 の ACCEPT が `baseline = 113` を書いた形跡もある。

### C-2. 規約検知の誤爆 — ハーネス自身の出力が次のサイクルを却下する

`ABUSE_RULES` の正規表現

```
git\s+push[^\n]*\borigin\s+(main\b|HEAD:main\b|HEAD:refs/heads/main\b)
```

は、**そのコマンドを禁止している文章にもマッチする**。
リポジトリ全体のスキャン結果、**10 箇所**がヒット:

| ファイル | 内容 |
|---|---|
| `scripts/auto_runner.py:199` | プロンプト内の禁止条項そのもの |
| `scripts/morning_report.py:175` | 人間向けコピペコマンド |
| `docs/nightly/2026-09-03.md:37` | morning_report が自動生成・git 追跡対象 |
| `docs/nightly/2026-09-04.md:38` | 同上 |
| `docs/instructions/09_ui_pictogram_and_sprites.md` | **本日の却下 2 件（084642 / 090118）の原因** |
| 他 5 箇所 | `docs/research/`, `docs/archive/`, `logs/ui/` |

構造的帰結として、**朝刊レポートを生成した翌サイクルは REJECT される**。

### C-3. 変更行数上限が Unity シーンで必ず爆発する

`MAX_CHANGED_LINES = 3000` が `MainGame.unity` の YAML 行を数えている。

本日の却下 2 件: `084642` で 8,318 行、`091219` で 7,894 行。

`00_rules.md` は「AI は `.unity` をテキスト編集するな、人間がエディタで行え」と
定めており、シーン変更は正規の手段（`Tools/Setup Complete UI Layout` メニュー実行）で
発生する。**ゲートが唯一の正規ルートを罰していた。**

### C-4. ダーティ保護を auto_runner 自身が無効化している

`begin_cycle()` はツリーが汚れていたら `None` を返して人間の作業を守る設計。
ところが `run_single_cycle()` が `None` を受けて
`git add -A && git commit -m "wip: save in-progress work"` して再試行するため、
保護が完全に無効化されていた。

帰結として、**保護ファイルの未コミット変更がスナップショット側に入り、
policy 検査を素通りする**。履歴に `5628d55 wip: save in-progress work` が残る。

### C-5. 停止条件が実装されているのに発火しない

`consecutive_reject_count()` と HALT ロジックは追加されていたが、2 箇所壊れていた。

1. `run_single_cycle()` は `return -1` するが、`main()` は `if remaining == 0` しか
   見ていない。`-1` は素通りし、10 秒後に同じことを繰り返す
2. `run_with_cli_fallback()` の呼び出しが巻き添えで消え、agy CLI フォールバックが
   死にコード化。本日の `AGENT_UNAVAILABLE` 2 件はこれが効いていれば救えた可能性がある

---

## 4. 設計の矛盾

### H-1. 稼働時間モデルが 3 つ同居している

| 出典 | 稼働時間 |
|---|---|
| `register_scheduled_task.ps1` | Daily 01:00 / PT30M 間隔 / PT5H → **01:00–06:00** |
| `auto_runner.py` `main()` | `while now.hour < 13` → **00:00–13:00** |
| auto_runner docstring / `pm1_opus_review.py` | 「留守中 **08:00〜13:00**」 |

さらに時間予算が破綻していた:

```
ExecutionTimeLimit = 45 分
TEST_TIMEOUT_SEC   = 1800 秒 × 2 プラットフォーム = 最大 60 分
```

ゲート判定の途中で必ず Task Scheduler に殺される。殺されると
`_quarantine_and_rollback()` が完走せずツリーが汚れ、次回 `ABORTED_DIRTY` →
C-4 の workaround が発動、という連鎖になる。本日の `ABORTED_DIRTY` 2 件はこれ。

加えて PT30M 反復 + 45 分実行 + `RestartCount 3` で多重起動する。

### H-2. 人間が Unity を開いていると全成果物が巻き戻る

`00_rules.md` は「人間: Unity エディタ操作、シーン/プレハブ構築、Inspector アサイン」を
役割として定めている。一方 `finalize_cycle()` は `unity_editor_running()` を検知すると
`UNVERIFIED` として `_quarantine_and_rollback()` する。

**役割分担そのものと衝突している。** 未検証は「破棄」ではなく「保留」が正しい。

### H-3. Gemini 側に事前ガードが存在しない

`guard.js` は Claude Code の PreToolUse フックであり、`nightly_gate.py` 冒頭が
自ら認めるとおり Gemini / agy には効かない。

**実装を全量担う側が事後検査のみ。** 本日の却下 5 件はすべて
「1 サイクル丸ごと消費してから怒られた」形になっている。

（`docs/workflow/ONBOARDING.md` §5 はこの非対称性を正しく記載している。
文書化はされているが、機構としては未対処。）

### H-4. 「実装した」というコミットメッセージが実態と乖離

`e478d3c feat(autorunner): implement instant Opus auto-healing loop upon REJECT
detection` の実体は呼び出し 0 件の死にコードだった。

`00_rules.md` の「**コード単体での進捗錯覚禁止**」の典型例が、
それを強制するハーネス自身に発生していた。

---

## 5. 中程度の問題

| # | 問題 |
|---|---|
| M-1 | `I-2` / `I-4` / `I-5` / `I-6` が dangling reference。現行 `00_rules.md` に存在しない（34KB 版の遺物）。却下ログを読んでも根拠条文に辿り着けない |
| M-2 | 判定履歴 `logs/nightly/cycles-*.jsonl` が `.gitignore` 対象。verdict の唯一の記録かつ `consecutive_reject_count()` の入力なのに clone で消失し、CI からも不可視 |
| M-3 | `nightly-reject/*` が 5 本放置。`_quarantine_and_rollback()` は作るだけで回収・削除の導線がない |
| M-4 | `docs/nightly/*.md` は `is_dirty()` の除外対象だが git 追跡対象。`git add -A` で拾われ diff に載り、C-2 の誤爆源になる |
| M-5 | `origin/auto/wip` が存在。設計上 push されるのは `nightly/<日付>` のみのはずで、想定外の push 経路がある |
| M-6 | `known_skipped = 19` がハードコード。`nightly_baseline.json` という正式な機構があるのにマジックナンバーが二重管理 |
| M-7 | `pm1_opus_review.py` の判定が非構造化。「即時承認/修正後承認/見送り」を自然言語で返させるだけで機械判定に接続されていない |
| M-8 | `docs/STATUS.md` の進行状況テーブルで 8 つの Step のコミット列が `HEAD`。時間とともに指す先が変わるため記録として機能しない |
| M-9 | `docs/workflow/ONBOARDING.md` §3 の「EditMode テスト（127件）」が実測（total 116 / passed 97）と乖離 |

---

## 6. 結論と方針

根本的な構図は「**AI の規律が足りない**」ではなく
「**ゲートの判定条件が正規の作業手順を罰している**」ことだった。
本日の却下 5 件中 4 件はハーネス側の誤検知である。

したがって当初提案の「散文ルールを増やす」方向は誤りで、
`00_rules.md` の宣言どおり **判定コード側の精度を上げる**のが正しい対処となる。

| 優先 | 対象 | 方針 |
|---|---|---|
| 1 | C-1 | baseline を `passed` 基準に統一。`known_skipped` を baseline ファイルへ移す |
| 2 | C-2 | `ABUSE_RULES` の検査対象を実行可能ファイルに限定。秘密情報検知のみ全ファイル対象で維持 |
| 3 | C-3 | シリアライズ資産を行数カウントから除外し、代わりにファイル数で監視 |
| 4 | C-5 | `main()` で中断シグナルを受ける。agy CLI フォールバックを復活 |
| 5 | C-4 | 保護ファイルが汚れている時は auto-commit せず中断 |
| 6 | H-1 | 稼働時間を 01:00–06:00 に一本化。`ExecutionTimeLimit` を 5 時間に。多重起動を止める |
| 7 | H-2 | `UNVERIFIED` は巻き戻さず保留（HOLD）にする |
| 8 | M-1 / M-2 | dangling な規約 ID を除去。判定履歴を追跡対象にする |

**スコープ外（実施しない）**

- `00_rules.md` への手続き（Gate 1〜4 等）の追記。同ファイル冒頭の
  「手続きは書かない／文章で守らせるのではない」に反する
- 修正のたびの全テスト実行の義務化。batchmode Unity は分単位であり、
  エディタ起動中は必ず失敗する
- `pm1_opus_review.py` のプロンプト改修。1 日 1 回の実行でトークン削減効果は
  誤差であり、レビュー品質を毀損するリスクのみが残る

---

## 7. 実施記録

本監査に基づく修正は **Part A（保護領域・人間/Claude が `main` 上で実施）** と
**Part B（保護領域外・Gemini 担当）** に分割した。

Part A の実施結果は `docs/log.md` の `## 2026-09-04` に記録する。

### H-3 について（未対処・要判断）

Gemini 側の事前ガード（`agy` を PowerShell ラッパーで包み、コミット前に
`check_policy()` を掛ける）は本監査では実施していない。
実装を全量担う側が無防備という構図は残っている。

### Unity-MCP について（未判断）

`Game/.mcp.json` には `rider` しか登録がない。Unity-MCP を導入しないのであれば、
上流診断の客観データ源は `nightly_gate.py` の batchmode テスト結果 XML のみである、
と明示的に決める必要がある。
