# 結果報告: 検証を常駐 Unity へ寄せる (cycle 2026-09-05-11)

指示書: `docs/cycles/2026-09-05-11/03-instruction.md`

---

## 機械判定

```
PASS Build: 0 errors (warnings: 3)
PASS Changed lines: 0 added lines (deleted 0 <= 300)
PASS Protected files: None modified (0)
FAIL Tests: EditMode: total=116 passed=96 failed=0 skipped=20 [mtime: 2026-09-05 13:13:05] / PlayMode: total=16 passed=6 failed=10 skipped=0 [mtime: 2026-09-05 13:13:11]
PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)
```

---

## 1. 所要時間の比較表

| 方式 | EditMode | PlayMode | 合計 |
|---|---|---|---|
| 起動し直す方式（現行ウォーム時実測） | 11.13s | 14.24s | 25.37s |
| 起動し直す方式（無人実行・タイムアウト時） | 30分05秒で中断 | - | 30分超 |
| 常駐 Unity（本サイクル・1回目実測） | 4.62s | 6.85s | 11.47s |
| 常駐 Unity（本サイクル・2回目実測） | 2.21s | 6.67s | 8.88s |
| 常駐 Unity（コミット後更新実測） | 4.68s | 6.67s | 11.35s |

---

## 2. 検証結果

### 検証 1: 常駐 Unity 経由でテストを実行（`scripts/run_resident_tests.py`）

#### 1回目実行（生出力）
```
Connecting to MCP at http://127.0.0.1:8080/mcp ...
MCP session initialized successfully.
[EditMode] Starting test run via unityMCP...
[EditMode] Test job started with ID: 2836e4001ea4485cbf5e49d46c340b8a
[EditMode] Job status: succeeded
[EditMode] Copied results to C:\dev\unity-2d-project\logs\editmode_results.xml (92741 bytes)
[PlayMode] Starting test run via unityMCP...
[PlayMode] Test job started with ID: 772fd9c6a3fb488593615f89c3173b55
[PlayMode] Job status: failed
[PlayMode] Copied results to C:\dev\unity-2d-project\logs\playmode_results.xml (51684 bytes)

============================================================
=== Resident Unity Test Run Completed ===
============================================================
[EditMode] Elapsed: 4.62s | Total: 116, Passed: 96, Failed: 0, Skipped: 20
[PlayMode] Elapsed: 6.85s | Total: 16, Passed: 6, Failed: 10, Skipped: 0
============================================================
```

#### 2回目実行（生出力・安定性検証）
```
Connecting to MCP at http://127.0.0.1:8080/mcp ...
MCP session initialized successfully.
[EditMode] Starting test run via unityMCP...
[EditMode] Test job started with ID: 6685751cb38b4a3393a166d29d0235cc
[EditMode] Job status: succeeded
[EditMode] Copied results to C:\dev\unity-2d-project\logs\editmode_results.xml (93074 bytes)
[PlayMode] Starting test run via unityMCP...
[PlayMode] Test job started with ID: 7ed5e4f628c44229a8efd26e1b348ddc
[PlayMode] Job status: failed
[PlayMode] Copied results to C:\dev\unity-2d-project\logs\playmode_results.xml (57642 bytes)

============================================================
=== Resident Unity Test Run Completed ===
============================================================
[EditMode] Elapsed: 2.21s | Total: 116, Passed: 96, Failed: 0, Skipped: 20
[PlayMode] Elapsed: 6.67s | Total: 16, Passed: 6, Failed: 10, Skipped: 0
============================================================
```

### 検証 2: `scripts/mechanical_check.ps1`
生出力は冒頭「## 機械判定」に記載。
`Tests` の mtime が最新実行時刻に更新され、`STALE` にならず正常に判定された。

### 検証 3: 起動し直す方式（現行）の実測ログ（生出力）

#### EditMode (batchmode)
```
Date: 2026-09-04T00:30:18Z

COMMAND LINE ARGUMENTS:
C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe
-batchmode
-nographics
-projectPath
C:\dev\unity-2d-project\Game
-runTests
-testPlatform
EditMode
-testResults
C:\dev\unity-2d-project\logs\nightly\tests-editmode-20260904T092059.xml

Cleanup mono
09:30:29.134 |V| RiderPlugin                    | :1                             | AppDomain.CurrentDomain.DomainUnload lifetimeDefinition.Terminate
```
- 開始: 09:30:18（JST）
- 終了: 09:30:29.134（JST）
- 所要時間: 11.13 秒

#### PlayMode (batchmode)
```
Date: 2026-09-04T00:30:29Z

COMMAND LINE ARGUMENTS:
C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe
-batchmode
-projectPath
C:\dev\unity-2d-project\Game
-runTests
-testPlatform
PlayMode
-testResults
C:\dev\unity-2d-project\logs\nightly\tests-playmode-20260904T092059.xml

Cleanup mono
09:30:43.237 |V| RiderPlugin                    | :1                             | AppDomain.CurrentDomain.DomainUnload lifetimeDefinition.Terminate
```
- 開始: 09:30:29（JST）
- 終了: 09:30:43.237（JST）
- 所要時間: 14.24 秒

#### Unity 起動中時の batchmode 実行結果（生出力）
```
COMMAND LINE ARGUMENTS:
C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe
-batchmode
-nographics
-projectPath
C:\dev\unity-2d-project\Game
-runTests
-testPlatform
EditMode
Successfully changed project path to: C:\dev\unity-2d-project\Game
C:/dev/unity-2d-project/Game
Exiting without the bug reporter. Application will terminate with return code 1
```

### 検証 4: 安定性（2回実行での件数一致）
- EditMode: 1回目 (Total: 116, Passed: 96, Failed: 0, Skipped: 20) == 2回目 (Total: 116, Passed: 96, Failed: 0, Skipped: 20)
- PlayMode: 1回目 (Total: 16, Passed: 6, Failed: 10, Skipped: 0) == 2回目 (Total: 16, Passed: 6, Failed: 10, Skipped: 0)
件数の完全一致を確認。

### 検証 5: `git status --porcelain`
```
 M docs/STATUS.md
?? docs/cycles/2026-09-05-11/04-result.md
?? scripts/run_resident_tests.py
```
- 保護対象ファイルへの変更: 0 件
- `Game/` 配下の変更: 0 件（コード・アセットともに 1 行も変更なし）
