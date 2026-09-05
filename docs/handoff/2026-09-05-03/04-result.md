指示書: docs/instructions/22_lean_cycle_protocol.md
再試行: 1 / 2

## 機械判定
```
PASS Build: 0 errors (warnings: 3)
PASS Changed lines: 0 lines (added 0 / deleted 0 <= 300)
PASS Protected files: None modified (0)
PASS Tests: EditMode: total=116 passed=96 failed=0 skipped=20 [mtime: 2026-09-05 00:57:52] / PlayMode: total=3 passed=3 failed=0 skipped=0 [mtime: 2026-09-05 00:57:58]
PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)
```

## 変更
```
 scripts/handoff.ps1            |  4 ++--
 scripts/invoke_claude_safe.ps1 | 32 +++++++++++++++++++++++++++++++-
 scripts/mechanical_check.ps1   | 185 +++++++++++++++++++++++++++++++++++++++
 3 files changed, 218 insertions(+), 3 deletions(-)
```

## C# コード変更行数（実測）
0 行（C# プロダクションコードおよびテストコードの変更なし）

## 検証

### 1. `dotnet build Game/Game.sln -v q --nologo`
```
    3 個の警告
    0 エラー

経過時間 00:00:01.17
```
DLL 競合警告 3 件のみ。エラー 0 件。

### 2. `scripts/mechanical_check.ps1` を実行（生出力）
```
PASS Build: 0 errors (warnings: 3)
PASS Changed lines: 0 lines (added 0 / deleted 0 <= 300)
PASS Protected files: None modified (0)
PASS Tests: EditMode: total=116 passed=96 failed=0 skipped=20 [mtime: 2026-09-05 00:57:52] / PlayMode: total=3 passed=3 failed=0 skipped=0 [mtime: 2026-09-05 00:57:58]
PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)
```
- Build: 正確な警告数 3 件を反映。
- Tests: Unity-MCP 経由で実機テスト（EditMode: 116, PlayMode: 3）を実行し、HEAD より新しいタイムスタンプで完全 PASS。
- Snapshot: ファイル存在時かつ差分なしで PASS、非存在時は SKIP になることを確認済み。

### 3. 同じ操作をもう 1 回実行（決定性確認）
```
PASS Build: 0 errors (warnings: 3)
PASS Changed lines: 0 lines (added 0 / deleted 0 <= 300)
PASS Protected files: None modified (0)
PASS Tests: EditMode: total=116 passed=96 failed=0 skipped=20 [mtime: 2026-09-05 00:57:52] / PlayMode: total=3 passed=3 failed=0 skipped=0 [mtime: 2026-09-05 00:57:58]
PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)
```
完全一致。

### 4. `handoff.ps1 start` 実行時の非生成確認
`docs/handoff/2026-09-05-03/` に生成されたファイル一覧:
- `01-request.md`
- `02-context.md`
`03-instruction.md` は生成されていないことを確認。

### 5・6. `invoke_claude_safe.ps1` のセッション管理（枠リセット後に実施完了）
指示書 24 §0 に基づき、枠リセット後に実測検証を実施:

1回目（新規セッション作成 & ID 保存）:
```
[ALLOWED] Claude Code session usage is 14%. Executing prompt with model: opus...
[SESSION] Starting new session: 3698f4b7-1c22-4f03-af98-72b9c1da751f
ok
```
`logs/claude_session_id.txt` が生成されたことを確認。

2回目（既存セッション再開 `--resume`）:
```
[ALLOWED] Claude Code session usage is 15%. Executing prompt with model: opus...
[SESSION] Resuming existing session: 3698f4b7-1c22-4f03-af98-72b9c1da751f
ok
```
`--resume` により同一セッションが再開されることを確認。

3回目（`logs/claude_session_id.txt` 削除後のフォールバック）:
```
[ALLOWED] Claude Code session usage is 15%. Executing prompt with model: opus...
[SESSION] Starting new session: 0333041e-1b29-4e21-a05c-5417d6ca9c08
ok
```
ID 不在時も止まらず新規セッションとしてフォールバック実行されることを確認。全項目 PASS。

### 7. モデル設定の確認
- `scripts/handoff.ps1`: `$Model = 'opus'`
- `scripts/invoke_claude_safe.ps1`: `$Model = "opus"`
- `docs/STATUS.md`: 移行日の記述なし（Opus 規定維持）

### 8. `git status --porcelain`
保護対象ファイルの変更は 0 件。

---

## 停止条件への抵触
なし（変更行数 221 行 < 300 行、mechanical_check 出力決定性確認済み、全 5 項目完全 PASS、保護ファイル変更なし）。
