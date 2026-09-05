# 指示書 27: ドキュメントを寿命で再編する

```
ROLE: Executor
BRANCH: main
判断: docs/decisions/0003-knowledge-by-lifetime.md
例外: 本サイクルに限り docs/ 配下の移動・整理を許可する（下の §3 で範囲を限定する）
並行: 指示書 26 と同時に進めてよい。あちらはテストコード、本書は docs のみでコードに触らない
```

## 目的

ADR 0003 の実施。`docs/` 15,000 行のうち、次のゲームに持っていける知識が 328 行（2%）しかない。
サイクルごとに約 5 ファイル増え続ける構造を止める。

**器を先に作る。** 移動だけ先にやっても複利は始まらない。

---

## 1. `laws/` を新設し、中身を入れる（最優先）

### `docs/laws/invariants.md`

指示書 26 の INV-1〜6 を写す。**1 法則につき次の 3 行だけ。**

```
### INV-1 フェーズが進む
どのフェーズも WaitingInput または終端に到達せずに 10 秒以上留まらない。
塞いだ実バグ: エンディング停止（18）/ 敗北フリーズ（20）/ ショップ未結線（19）
```

指示書 26 から解説や実装方針を持ってこないこと。**法則と、それが塞いだ実バグの名前だけ。**

### `docs/laws/failure-modes.md`

過去に実際に起きた失敗の型を書く。**1 型 2〜3 行。**最低この 5 件を含めること。

| 症状 | 法則 | 検出手段 |
|---|---|---|
| 非アクティブなパネルに View が乗り `OnEnable` が走らない | View は常時アクティブなホストに置き、`_panelRoot` にパネル自身を張る | スナップショットの `active=` / INV-3 |
| 実施していない検証を実施したと報告する | 実測値は生出力のみ。要約で代替しない | 報告値とアセット実体の突き合わせ |
| Claude の枠が枯れると実装 AI が越権する | 枠枯渇は停止条件であって、役割の解除理由ではない | `05-review.md` の作成者 |
| シーンを読むと 10 万トークン消える | `.unity` を AI が読まない。結線はスナップショットで見る | 規約（`0546e3c`） |
| 同じ型のバグに 2 枚目の指示書を書く | それは知識化の失敗である | `laws/` に該当法則があるかを確認する |

### 上限

**`laws/` は 2 ファイル合計 150 行以内。** 超えたら足すのではなく統合する。
超えそうになったら**手を止めて報告する。** 勝手に上限を緩めないこと。

---

## 2. `cycles/` へ統合する

`docs/cycles/` と `docs/cycles/` を `docs/cycles/` にまとめる。

```
docs/cycles/2026-09-05-06/
  01-request.md          （あれば）
  02-context.md          （あれば）
  03-instruction.md      ← docs/cycles/*/03-instruction.md をここへ
  04-result.md
  05-review.md
```

### 手順

- **必ず `git mv` を使う。** `cp` + `rm` にしない（履歴が切れる）
- 指示書と handoff サイクルの対応は各 `04-result.md` の冒頭「指示書:」行から取れる。
  **対応が取れない指示書は `docs/cycles/_unpaired/` に置く。推測で紐づけないこと**
- 番号なしの `audit_fixes.md` / `step8_fix_instructions.md` は `docs/archive/` へ

### リンクの張り替え

移動後、`docs/` 配下と `.agents/rules/00_rules.md` の中の
`docs/cycles/` / `docs/cycles/` へのパス参照を新しいパスへ直す。

```bash
grep -rn "docs/cycles/\|docs/cycles/" docs .agents --include=*.md
```

**`.agents/rules/00_rules.md` はパス文字列の置換のみ許可する。** 他の変更をしない。

---

## 3. 追記型を止める

| 対象 | 処置 |
|---|---|
| `docs/log.md`（1,243 行） | `docs/archive/log_until_20260905.md` へ `git mv`。**以後は追記しない** |
| `docs/PLAN_ROADMAP.md` | 生きている項目だけ `docs/STATUS.md` へ移し、本体は `docs/archive/` へ |
| `docs/nightly/` | `.gitignore` に追加し `git rm -r --cached docs/nightly`（**ファイルは消さない**） |
| `docs/research/` | `Research-07` の brief と report を除き、`docs/archive/research/` へ `git mv` |

`docs/research/Research-07_*` は現行の判断の根拠なので残す。

---

## 4. やってはいけないこと

```
Game/**（コードもアセットも 1 行も触らない）  .claude/hooks/guard.js
.github/workflows/**  scripts/**  Game/Packages/manifest.json  .mcp.json
docs/spec/**  docs/decisions/**  docs/strategy/**  docs/webgl/**
```

- **ファイルを削除しない。** すべて `git mv` による移動である。`docs/archive/` は捨て場ではなく保管庫
- `.agents/rules/00_rules.md` は**パス文字列の置換のみ。** §6 の追記は Claude が行う（下記）
- **`05-review.md` を新規に書かない・書き換えない。** レビューは Claude の役割
- `laws/` の 150 行上限を勝手に緩めない
- 対応の取れない指示書を推測で紐づけない
- 移動のついでに中身を要約・整形しない。**移すだけ**

---

## 5. 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `scripts/mechanical_check.ps1` | 全項目。**`Changed lines` が 0 に近いこと**（移動が主のため） |
| 2 | `git status --porcelain` | 保護対象 0 件。**`Game/` 配下の差分が 0 であること** |
| 3 | `git log --follow` を移動後のファイル 1 本で実行 | 履歴が繋がっていること（`git mv` を使った証拠）。生出力を貼る |
| 4 | `wc -l docs/laws/*.md` | **合計 150 行以内**。生出力を貼る |
| 5 | 壊れたリンクの確認 | `grep -rn "docs/cycles/\|docs/cycles/" docs .agents --include=*.md` の出力が **0 件** |
| 6 | 移動前後のファイル数 | `docs/` 配下の `.md` 総数が**移動前と一致**すること（削除していない証拠） |
| 7 | `docs/cycles/_unpaired/` の中身 | 対応が取れなかった指示書の一覧。**0 件でなくてよい** |

**検証 3 と 6 が本サイクルの合否である。** 履歴を切らず、1 ファイルも失っていないこと。

---

## 6. 停止条件

- `laws/` が 150 行に収まらない
- 指示書とサイクルの対応が半数以上取れない
- `git mv` で履歴が繋がらない
- `Game/` 配下に差分が出た
- 保護対象ファイルの変更が必要になった

---

## 7. 報告

`docs/cycles/2026-09-05-07/04-result.md`（移動後の新しい場所）に書く。
先頭に `## 機械判定`。移動前後のファイル数と、`_unpaired/` の中身を明記すること。

コミットのみ行い、push はしない。

---

## 8. Claude が別途行うこと（Gemini は触らない）

- `.agents/rules/00_rules.md` への追記
  - Architect の読み取り範囲に `docs/laws/` を追加
  - 「サイクルを閉じるとき、これは法則になるかを問う」を `05-review.md` の必須項目として明文化
- `docs/decisions/0003-knowledge-by-lifetime.md` のステータスを提案 → 採用へ
