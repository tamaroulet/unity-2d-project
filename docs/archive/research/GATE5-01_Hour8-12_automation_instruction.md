# GATE5-01: Hour 8-12「手放し自動化インフラ」確定指示書

- 発行: Claude Opus（アーキテクト） / 2026-09-03
- 承認: 人間ディレクター（「演出・ゲームデザインは決めない。人間が手放すための基盤に残り時間を全投入」方針）
- 上位規約: `docs/workflow/TRIAD_PROTOCOL.md` / `.agents/rules/00_rules.md`
- 前提ゲート: **GATE4-01 完了**（`SmokeTest.cs` が実機 Unity で 100% Passed）
- 実行者: **人間（§1）→ Gemini（§2 → §3 → §4）→ Claude（§5 の記録）**
- **この指示書に列挙されていないファイルは、読んでよいが書き込んではならない。**

---

## 0. 目的と前提

### 0-1. このターンで成立させること（これ以外はやらない）

> **「人間が寝ている間にエージェントが書いたコードが、人間の承認なしに main を汚さないこと。
> そして翌朝、人間が 1 画面を 60 秒読むだけで『何が起きたか・何を判断すべきか』が分かること。」**

具体的には 3 つ:

| # | 成果物 | 成立条件 |
|---|---|---|
| A | **クラウド CI**（GitHub Actions + GameCI） | push すると EditMode 127 件 + PlayMode 1 件がクラウドで自動実行され、赤なら GitHub 上で即検知できる |
| B | **夜間ランナーの安全ハーネス** | テスト失敗・ポリシー違反の成果物が**自動で隔離ブランチへ退避され、main から消える**。かつ 1 行も失われない |
| C | **朝刊レポート** | 06:10 に `docs/nightly/YYYY-MM-DD.md` が自動生成され、受理／隔離／未検証と「人間が判断すべき項目」が 1 画面に出る |

**やらないこと**（今回のスコープ外・着手禁止）:
演出・アート・ゲームバランス・新機能・新テストの追加・`Game/Assets` 配下のプロダクションコード変更。

### 0-2. 触ってよいファイル（完全な列挙・これ以外は書き込み禁止）

| # | パス（リポジトリルート相対） | 担当 | 操作 |
|---|---|---|---|
| 1 | GitHub リポジトリ設定 / Secrets | **人間** | §1（ブラウザ操作） |
| 2 | `Game/Assets/Tests/PlayMode/SmokeTest.cs.meta` | Gemini | `git add` するだけ（**編集禁止**） |
| 3 | `.github/workflows/unity-test.yml` | Gemini | 新規作成（全文貼り付け） |
| 4 | `.github/workflows/unity-activation.yml` | Gemini | 新規作成（全文貼り付け） |
| 5 | `.github/workflows/webgl-build.yml` | Gemini | 新規作成（全文貼り付け） |
| 6 | `scripts/nightly_gate.py` | Gemini | 新規作成（全文貼り付け） |
| 7 | `scripts/morning_report.py` | Gemini | 新規作成（全文貼り付け） |
| 8 | `scripts/nightly_baseline.json` | Gemini | 新規作成（全文貼り付け） |
| 9 | `scripts/auto_runner.py` | Gemini | **差分改修のみ**（§3-D の指定 3 箇所だけ） |
| 10 | `scripts/register_morning_report_task.ps1` | Gemini | 新規作成（全文貼り付け） |
| 11 | `docs/nightly/.gitkeep` | Gemini | 空ファイル新規作成 |
| 12 | `.gitignore`（ルート） | Gemini | コメント 1 行追記のみ |
| 13 | `docs/log.md` / `docs/STATUS.md` | **Claude** | Gemini は触るな |

### 0-3. Claude が実機・実 API で確認済みの前提（再確認不要）

| 項目 | 実測値 | 確認方法 |
|---|---|---|
| Unity | `6000.3.23f1` | `Game/ProjectSettings/ProjectVersion.txt` |
| ローカル Unity 実体 | `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` **存在** | ファイル存在確認 |
| GameCI イメージ | `unityci/editor:ubuntu-6000.3.23f1-webgl-3` **実在**（linux/amd64, 7.4GB, 2026-08-26 push） | Docker Hub API → HTTP 200 |
| GameCI アクション最新版 | `unity-test-runner@v4.3.1` / `unity-builder@v5.0.1` / `unity-request-activation-file@v2.2.0` | GitHub Releases API |
| **リポジトリ可視性** | **private**（`api.github.com/repos/tamaroulet/unity-2d-project` → 404、rate limit 残 54 なので「存在しない」ではなく「非公開」） | GitHub API |
| ローカル main | **origin/main より 18 コミット先行（未 push）** | `git rev-list --left-right --count origin/main...main` |
| `SmokeTest.cs.meta` | **未追跡（untracked）** | `git status` |
| `SmokeTest.cs` 本体 | 追跡済み | `git ls-files` |
| `Game/Packages/packages-lock.json` | 存在（キャッシュキーに使える） | ファイル存在確認 |
| `logs/` | ルート `.gitignore` で除外済み | `.gitignore` |
| `Game/Builds/` `Game/Library/` | `Game/.gitignore` で除外済み | `Game/.gitignore` |
| `docs/webgl/` | 17 ファイル追跡済み・`deploy_pages.yml` が `docs/webgl/**` の push で Pages 公開 | `git ls-files` |
| リモートブランチ | `main` / `gh-pages` / 他 3 本 | `git branch -a` |
| guard フック | `.claude/hooks/guard.js` が `git reset --hard` / `git push --force` / `rm -rf /` と `.meta`・`.asmdef`・`.unity`・`.asset` への書き込みを**ツールレベルで拒否**する | `.claude/settings.json` + 実物 |
| Python | 3.12.10 | `python --version` |
| Task Scheduler | `UnityProject_AutoRunner`（01:00 起動 / 30 分間隔 / 5 時間） | `scripts/register_scheduled_task.ps1` |

### 0-4. 予算の実測と、そこから導かれる設計上の非交渉事項

**private リポジトリ = GitHub Free の Actions 無料枠は月 2,000 分**（ubuntu は 1 分 = 1 分換算）。
Unity のテストジョブは現実的に **1 回 15〜25 分**、WebGL ビルドは **25〜40 分**。

> 2,000 ÷ 20 ≒ **月 100 回**。夜間ランナーが 1 晩 8 サイクル回して毎回 push すれば
> **月 240 回 → 3 日で枠が枯れる。**

したがって次を**不変条件（設計変更不可）**とする:

| # | 不変条件 | 理由 |
|---|---|---|
| **I-1** | 夜間の一次ゲートは**ローカル Unity batchmode**。クラウド CI は二次の網 | ローカルは無料・高速。Unity 実体がこのマシンにある |
| **I-2** | 夜間ランナーは**絶対に `origin/main` へ push しない**。push 先は `nightly/YYYY-MM-DD` ブランチのみ | main を人間の承認領域として保つ |
| **I-3** | **WebGL ビルドは `workflow_dispatch`（手動）専用**。push では走らせない | 1 回 40 分。自動化すると枠が即死する |
| **I-4** | ロールバックの前に、**必ず全成果物を隔離ブランチへコミットして保全**する。巻き戻しは保全後にのみ行う | 作業を失わない。朝に cherry-pick できる |
| **I-5** | **検証できなかったサイクルは「成功」ではなく `UNVERIFIED`** として巻き戻す | 未検証コードが main に積まれるのが最大の事故要因 |
| **I-6** | エージェントは**自分のガードレール**（workflows / hooks / rules / nightly_gate.py）を書き換えられない。書き換えを検知したら即隔離 | 自己改変によるゲート無効化の防止 |

### 0-5. ハーネス設計の実測検証（Claude が実施済み）

§3-B の `check_policy()` を**この指示書を書いた時点の実リポジトリに実際に適用して**動作確認した。

| 対象 | 結果 | 意味 |
|---|---|---|
| `HEAD~1..HEAD`（通常の docs コミット） | violations=0 / warnings=0 | **誤検知しない** |
| `HEAD~5..HEAD`（通常の docs コミット群） | violations=0 / warnings=0 | **誤検知しない** |
| `HEAD~18..HEAD`（未 push の 18 コミット全体） | violations=70 / warnings=17 | `.agents/rules/00_rules.md`・`.claude/hooks/guard.js`・`.claude/settings.json` の変更と `.asmdef` 削除を正しく検知。**これらは人間が意図してやった作業であり、夜間エージェントがやってはいけない作業**なので、検知は正しい |

つまりハーネスは「普段の作業は素通し、ガードレール改変と資産削除は捕捉」という設計どおりに動く。

### 0-6. 【重要】今夜 1:00 までに人間がやる 3 つのこと（Pre-flight）

**Claude が実測したところ、現時点では夜間ランナーは 1 サイクルも成果を出せない状態にある。** 理由は 2 つ:

| 実測値 | 何が起きるか | 対処 |
|---|---|---|
| **`unity_editor_running() == True`**（Unity エディタが開いている） | `-batchmode` が Library ロックで必ず失敗する。全サイクルが `UNVERIFIED` になり、**一晩かけて何も受理されない** | **寝る前に Unity エディタを閉じる** |
| **`is_dirty() == True`**（`SmokeTest.cs.meta` が未追跡） | `begin_cycle()` が `ABORTED_DIRTY` を返し、**エージェントが 1 度も起動しない** | §2-0 の `git add` と §4-F のコミットで解消される |

**寝る前チェックリスト（人間・毎晩）**

- [ ] Unity エディタを閉じた（タスクマネージャに `Unity.exe` がないこと）
- [ ] `git status` がクリーン（未コミットの作業がない）
- [ ] `python scripts\nightly_gate.py selftest` が `unity running : False` / `dirty : False` を出す

> この 2 条件は「安全側に倒す」設計の代償である。**黙って壊れるより、黙って何もしない方が安全**という判断でこうしてある。
> どちらも朝刊レポートの §1 に「中止(作業中)」「未検証」として必ず出るので、翌朝 1 行で気づける。

---

## 1. 【人間のみ・先行必須】GitHub 側の前準備

**§1-A が終わるまで §2 の CI は 1 度も緑にならない。** ただし §3（夜間ハーネス）は §1 と完全に独立しているので、**Gemini は §1 の完了を待たずに §3 へ進んでよい**。今夜 1:00 に間に合わせるべきは §3 である。

### 1-A. Unity ライセンスの活性化（GameCI 用）

Unity Personal ライセンスをクラウドで使うには、**マシン非依存の `.ulf` を GitHub Secrets に入れる**必要がある。ブラウザ操作を伴うため人間しかできない。

1. Gemini が §2 で `.github/workflows/unity-activation.yml` を作り、push する。
2. GitHub → **Actions** → `Unity Activation (manual)` → **Run workflow** を実行。
3. 実行完了後、**Artifacts** から `Unity_v6000.3.23f1.alf` をダウンロード。
4. https://license.unity3d.com/manual を開く。
   `.alf` をアップロード → Unity Personal Edition → 用途を選択 → **`Unity_v6000.x.ulf` をダウンロード**。
5. `.ulf` を**テキストエディタで開き、`<?xml ...` から最終行まで全文コピー**。
6. GitHub → **Settings → Secrets and variables → Actions → New repository secret** で 3 件登録:

| Secret 名 | 値 |
|---|---|
| `UNITY_LICENSE` | 手順 5 の `.ulf` 全文（XML そのまま） |
| `UNITY_EMAIL` | Unity ID のメールアドレス |
| `UNITY_PASSWORD` | Unity ID のパスワード |

7. 完了したら `unity-activation.yml` の実行は二度と不要（ライセンスは失効するまで有効）。

> **失敗時の Plan B（判断基準を先に決めておく）**
> Unity 6 系の Personal ライセンス活性化は GameCI 側で失敗報告がある。**Claude はここを外部から検証できない。**
> **`unity-test.yml` が「ライセンス起因」で 2 回連続失敗したら、クラウド CI は諦めて
> セルフホストランナー（このマシンに GitHub Actions runner を常駐）へ切り替える。**
> このマシンには既に活性化済みの Unity があるため、ライセンス問題も Actions 分数課金も同時に消える。
> 代償はマシンの電源が入っている必要があること。
> **この切り替え判断は人間が行う。Gemini が勝手に切り替えてはならない。**

### 1-B. リポジトリ設定の確認（人間・ブラウザ）

- [ ] **Settings → Actions → General** で Actions が **Allowed**。
- [ ] **Settings → Actions → General → Workflow permissions** を **Read and write permissions**（テスト結果の Check 作成に必要）。
- [ ] **Settings → Billing** で Actions の今月の残り分数を確認し、**§5 の報告に実数を書く**。
- [ ] **Settings → Pages** の状態を確認する。private リポジトリのままだと **GitHub Pages は Free プランでは公開できない**。`docs/webgl/` と `gh-pages` ブランチは存在するが、**Pages が実際に生きているかを Claude は外部から確認できなかった**。有効／無効／要 Pro のどれかを §5 に記載すること。

### 1-C. 未 push の 18 コミットの扱い（人間の判断）

ローカル main は origin より 18 コミット先行している。**CI は push されるまで 1 度も走らない。**
§2 完了後、人間が内容を確認して `git push origin main` を実行すること。
（Gemini はこの push を代行してはならない。§6 参照。）

---

## 2. 【Gemini】GameCI ワークフローの配備

### 2-0. 最初にやる 1 コマンド（`.meta` の追跡）

```
git add Game/Assets/Tests/PlayMode/SmokeTest.cs.meta
```

> `.meta` の**編集**は guard フックが拒否するが、`git add` は拒否されない。
> これを追跡しないと CI 側の Unity が GUID を再生成し、`SmokeTest` がアセンブリに正しく載らない危険がある。

### 2-A. `.github/workflows/unity-test.yml`（新規作成・全文）

```yaml
name: Unity Tests

on:
  push:
    branches:
      - main
      - 'nightly/**'
    paths-ignore:
      - 'docs/**'
      - 'logs/**'
      - '**/*.md'
      - '.github/ISSUE_TEMPLATE/**'
  pull_request:
    branches:
      - main
  workflow_dispatch:

# 同一 ref の実行は最新 1 本だけ残す（夜間の連続 push で分数を溶かさないため）
concurrency:
  group: unity-test-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read
  checks: write

jobs:
  test:
    name: EditMode + PlayMode
    runs-on: ubuntu-latest
    timeout-minutes: 45
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 1
          lfs: false

      # unityci/editor イメージは展開後 20GB 超。標準ランナーの空きでは足りない
      - name: Free disk space
        run: |
          sudo rm -rf /usr/share/dotnet /usr/local/lib/android /opt/ghc \
                      /opt/hostedtoolcache/CodeQL /usr/local/share/boost
          df -h

      # 固定キー。ヒットしたら保存はスキップされる（10GB のリポジトリキャッシュ上限を守る）
      - name: Cache Library
        uses: actions/cache@v4
        with:
          path: Game/Library
          key: Library-Game-${{ hashFiles('Game/Packages/packages-lock.json', 'Game/ProjectSettings/ProjectVersion.txt') }}
          restore-keys: |
            Library-Game-

      - name: Run tests
        id: tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: Game
          unityVersion: 6000.3.23f1
          testMode: all
          artifactsPath: test-artifacts
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          checkName: Unity Test Results

      - name: Upload test artifacts
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: unity-test-results
          path: test-artifacts
          retention-days: 14
```

**設計意図（改変禁止の箇所）**

- `testMode: all` = EditMode + PlayMode の両方。**PlayMode を外すと `SmokeTest` が走らず、GATE4 の成果が守られない。**
- `paths-ignore` に `docs/**` があるのは、朝刊レポートのコミットで CI を起動させないため（I-3 の分数対策）。
- `cancel-in-progress: true` は夜間の連続 push を 1 本に畳むための必須設定。
- `projectPath: Game` を忘れるとリポジトリルートを Unity プロジェクトと誤認して失敗する。

### 2-B. `.github/workflows/unity-activation.yml`（新規作成・全文）

```yaml
name: Unity Activation (manual)

# §1-A のライセンス取得のためだけに人間が 1 度だけ手で回す。自動起動はしない。
on:
  workflow_dispatch:

jobs:
  activation:
    name: Request .alf file
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - name: Request manual activation file
        id: getManualLicenseFile
        uses: game-ci/unity-request-activation-file@v2
        with:
          unityVersion: 6000.3.23f1

      - name: Upload activation file
        uses: actions/upload-artifact@v4
        with:
          name: ${{ steps.getManualLicenseFile.outputs.filePath }}
          path: ${{ steps.getManualLicenseFile.outputs.filePath }}
          retention-days: 3
```

### 2-C. `.github/workflows/webgl-build.yml`（新規作成・全文）

```yaml
name: WebGL Build (manual)

# I-3: 1 回 25〜40 分かかる。private リポジトリの 2,000 分/月 を守るため push では走らせない。
on:
  workflow_dispatch:
    inputs:
      reason:
        description: 'このビルドを回す理由（記録用）'
        required: false
        default: 'manual verification'

concurrency:
  group: webgl-build
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  build:
    name: Build WebGL
    runs-on: ubuntu-latest
    timeout-minutes: 90
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 1

      - name: Free disk space
        run: |
          sudo rm -rf /usr/share/dotnet /usr/local/lib/android /opt/ghc \
                      /opt/hostedtoolcache/CodeQL /usr/local/share/boost
          df -h

      - name: Cache Library
        uses: actions/cache@v4
        with:
          path: Game/Library
          key: Library-Game-WebGL-${{ hashFiles('Game/Packages/packages-lock.json', 'Game/ProjectSettings/ProjectVersion.txt') }}
          restore-keys: |
            Library-Game-WebGL-
            Library-Game-

      - name: Build
        uses: game-ci/unity-builder@v5
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: Game
          unityVersion: 6000.3.23f1
          targetPlatform: WebGL
          buildName: WebGL
          buildsPath: build

      - name: Upload build
        uses: actions/upload-artifact@v4
        with:
          name: webgl-build-${{ github.sha }}
          path: build/WebGL
          retention-days: 7
```

> **公開（Pages 反映）は自動化しない。** 既存の `deploy_pages.yml` は `docs/webgl/**` の push で走る。
> 成果物を公開したい時だけ、人間が Artifacts を落として `docs/webgl/` に置き、コミットする
> （ローカルビルド用には既存の `scripts/copy_webgl_to_docs.ps1` がある）。
> **CI から `docs/webgl/` へ自動コミットさせてはならない**（8.4MB の wasm が毎回リポジトリに積まれる）。

### 2-D. §2 完了時の自己チェック（Gemini が実行）

```
git status --short
python -m pip install pyyaml
python -c "import yaml;[yaml.safe_load(open(p,encoding='utf-8')) for p in ['.github/workflows/unity-test.yml','.github/workflows/unity-activation.yml','.github/workflows/webgl-build.yml']];print('YAML OK')"
```

**YAML が壊れていると GitHub は無言で無視する。「CI があるつもり」が最悪の状態なので、必ずこの検証を通すこと。**

---

## 3. 【Gemini】夜間自律ランナーの安全ハーネス

**今夜 1:00 に間に合わせるべき本丸はここ。** §1 / §2 が詰まっても §3 は独立して完成させられる。

### 3-A. `scripts/nightly_baseline.json`（新規作成・全文）

```json
{
  "_comment": "テスト件数の下限。エージェントがテストを削って緑にする経路を塞ぐ。実測で上振れしたら nightly_gate が自動で引き上げる。",
  "EditMode": 127,
  "PlayMode": 1
}
```

### 3-B. `scripts/nightly_gate.py`（新規作成・全文）

```python
# SPDX-AI-Disclosure: ai-generated
"""夜間自律ランナーの安全ハーネス。

1 サイクル = begin_cycle() で HEAD を記録 -> エージェント実行 -> finalize_cycle() で判定。

判定の順序（早い順に落とす）:
  1. サイクル開始時にワーキングツリーが汚れている  -> ABORTED_DIRTY（人間の作業に触らない）
  2. 差分なし                                        -> NO_CHANGE
  3. ポリシー違反（ハックコード・自己改変・秘密情報）-> REJECT_POLICY
  4. Unity 起動中でローカル検証が不能                -> UNVERIFIED
  5. テスト失敗 / 件数がベースライン未満             -> REJECT_TESTS
  6. すべて通過                                      -> ACCEPT

ACCEPT 以外は「隔離ブランチへ全成果物をコミットして保全 -> スナップショットへ巻き戻し」。
作業は 1 行も失われない。朝、人間が cherry-pick できる。

単体実行: python scripts/nightly_gate.py selftest
"""
from __future__ import annotations

import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
GAME_PATH = PROJECT_ROOT / "Game"
NIGHTLY_LOG_DIR = PROJECT_ROOT / "logs" / "nightly"
NIGHTLY_LOG_DIR.mkdir(parents=True, exist_ok=True)
BASELINE_PATH = PROJECT_ROOT / "scripts" / "nightly_baseline.json"

UNITY_EXE = Path(os.environ.get(
    "UNITY_EXE",
    r"C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"))

TEST_TIMEOUT_SEC = 1800

VERDICT_ACCEPT = "ACCEPT"
VERDICT_NO_CHANGE = "NO_CHANGE"
VERDICT_REJECT_POLICY = "REJECT_POLICY"
VERDICT_REJECT_TESTS = "REJECT_TESTS"
VERDICT_UNVERIFIED = "UNVERIFIED"
VERDICT_ABORTED_DIRTY = "ABORTED_DIRTY"


# ==============================================================================
# git ラッパ
# ==============================================================================
def git(*args: str, check: bool = True) -> str:
    proc = subprocess.run(
        ["git", *args], cwd=str(PROJECT_ROOT),
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and proc.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed ({proc.returncode}): {proc.stderr.strip()}")
    return proc.stdout


def head_sha() -> str:
    return git("rev-parse", "HEAD").strip()


def is_dirty() -> bool:
    return bool(git("status", "--porcelain").strip())


# ==============================================================================
# ポリシー定義（guard.js の禁止事項を「事後の diff」に適用したもの）
#   Gemini / agy にはフックが効かないため、成果物をコミット後に静的検査する。
# ==============================================================================

# 自己改変の禁止領域。ここが 1 行でも変わったら無条件で隔離する（I-6）。
PROTECTED_PREFIXES = (
    ".github/workflows/",
    ".claude/hooks/",
    ".claude/settings.json",
    ".agents/rules/",
    "scripts/nightly_gate.py",
    "scripts/morning_report.py",
    "scripts/nightly_baseline.json",
    "scripts/auto_runner.py",
)

TEST_PREFIX = "Game/Assets/Tests/"
RUNTIME_CS = re.compile(r"^Game/Assets/.*\.cs$", re.IGNORECASE)
EDITOR_OR_TEST_CS = re.compile(r"/(Editor|Tests?)/", re.IGNORECASE)
SERIALIZED = re.compile(r"\.(unity|prefab|asset|meta|asmdef)$", re.IGNORECASE)

# テストを弱めて緑にする手口
TEST_WEAKENING_RULES = [
    (re.compile(r"\[\s*Ignore\b"), "テストに [Ignore] 属性が追加された"),
    (re.compile(r"\[\s*Explicit\b"), "テストに [Explicit] 属性が追加された"),
    (re.compile(r"\bAssert\.(Ignore|Pass)\s*\("), "Assert.Ignore / Assert.Pass が追加された"),
    (re.compile(r"\bAssert\.(IsTrue|That)\s*\(\s*true\s*[,)]"), "常に真のダミーアサーションが追加された"),
    (re.compile(r"\bLogAssert\.ignoreFailingMessages\s*=\s*true"),
     "LogAssert.ignoreFailingMessages で失敗ログを握り潰している"),
    (re.compile(r"^\s*//\s*Assert\."), "アサーションがコメントアウトされた"),
]

# ランタイムコードに入ってはいけないもの（guard.js と同一の禁止事項）
RUNTIME_HACK_RULES = [
    (re.compile(r"#if\s+UNITY_EDITOR"), "ランタイムコードに #if UNITY_EDITOR が混入（WebGL で消える）"),
    (re.compile(r"\bAssetDatabase\b"), "ランタイムコードに AssetDatabase が混入（WebGL で消える）"),
    (re.compile(r"\bUnityEditor\b"), "ランタイムコードに UnityEditor 参照が混入（WebGL で消える）"),
    (re.compile(r"\bFindFirstObjectByType\b|\bFindAnyObjectByType\b|\bFindObjectOfType\b"
                r"|\bGameObject\.Find\s*\(|\btransform\.Find\s*\("),
     "ランタイムコードにシーン検索（Find 系）が再導入された"),
]

# 秘密情報・ガード迂回
ABUSE_RULES = [
    (re.compile(r"\bghp_[A-Za-z0-9]{20,}|\bgithub_pat_[A-Za-z0-9_]{20,}"
                r"|\bsk-ant-[A-Za-z0-9_\-]{20,}|\bAKIA[0-9A-Z]{16}\b"),
     "資格情報らしき文字列がコミットに含まれている"),
    (re.compile(r"git\s+push[^\n]*(--force|\s-f\b)"), "git push --force がスクリプトに埋め込まれた"),
    (re.compile(r"--no-verify"), "--no-verify によるフック迂回が埋め込まれた"),
    (re.compile(r"git\s+push[^\n]*\borigin\s+(main\b|HEAD:main\b|HEAD:refs/heads/main\b)"),
     "origin/main への直接 push が埋め込まれた（I-2 違反）"),
    (re.compile(r"--dangerously-skip-permissions"), "権限スキップフラグが新たに埋め込まれた"),
]

MAX_CHANGED_LINES = 3000


def _diff_name_status(base: str, head: str) -> list:
    out = git("diff", "--name-status", f"{base}..{head}", check=False)
    rows = []
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) >= 2:
            rows.append((parts[0][0], parts[-1].replace("\\", "/")))
    return rows


def _diff_numstat(base: str, head: str) -> dict:
    out = git("diff", "--numstat", f"{base}..{head}", check=False)
    files = ins = dele = 0
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) >= 3:
            files += 1
            if parts[0].isdigit():
                ins += int(parts[0])
            if parts[1].isdigit():
                dele += int(parts[1])
    return {"files": files, "insertions": ins, "deletions": dele}


def _iter_diff_lines(base: str, head: str):
    """(path, sign, text) を返す。sign は '+' または '-'。"""
    out = git("diff", "--unified=0", "--no-color", f"{base}..{head}", check=False)
    path = None
    for line in out.splitlines():
        if line.startswith("+++ b/"):
            path = line[6:].replace("\\", "/")
        elif line.startswith("--- ") or line.startswith("@@") or line.startswith("diff --git"):
            continue
        elif line.startswith("+"):
            yield path, "+", line[1:]
        elif line.startswith("-"):
            yield path, "-", line[1:]


def check_policy(base: str, head: str) -> dict:
    """差分を静的検査する。violations が空でなければ隔離する。"""
    violations = []
    warnings = []

    stat = _diff_numstat(base, head)
    if stat["insertions"] + stat["deletions"] > MAX_CHANGED_LINES:
        violations.append(
            f"1 サイクルの変更量が上限を超過"
            f"（{stat['insertions'] + stat['deletions']} 行 > {MAX_CHANGED_LINES} 行）。"
            "暴走の疑いがあるため人間の確認が必要")

    for status, path in _diff_name_status(base, head):
        if any(path.startswith(prefix) for prefix in PROTECTED_PREFIXES):
            violations.append(f"保護対象の自己ガードレールが変更された: {path}（I-6 違反）")
        if status == "D" and path.startswith(TEST_PREFIX):
            violations.append(f"テストファイルが削除された: {path}")
        if status == "D" and SERIALIZED.search(path):
            violations.append(f"Unity シリアライズ資産が削除された: {path}")
        if status in ("M", "A") and SERIALIZED.search(path):
            warnings.append(f"Unity シリアライズ資産が変更された（人間の目視確認が必要）: {path}")

    for path, sign, text in _iter_diff_lines(base, head):
        if path is None:
            continue

        if sign == "+":
            for pattern, message in ABUSE_RULES:
                if pattern.search(text):
                    violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

            if path.startswith(TEST_PREFIX):
                for pattern, message in TEST_WEAKENING_RULES:
                    if pattern.search(text):
                        violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

            if RUNTIME_CS.match(path) and not EDITOR_OR_TEST_CS.search("/" + path):
                for pattern, message in RUNTIME_HACK_RULES:
                    if pattern.search(text):
                        violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

        else:  # 削除された行
            if path.startswith(TEST_PREFIX) and re.search(r"\bAssert\.|\bLogAssert\.", text):
                violations.append(f"既存のアサーションが削除された [{path}] -> {text.strip()[:120]}")

    # 同一メッセージの重複を潰す（順序は保つ）
    violations = list(dict.fromkeys(violations))
    warnings = list(dict.fromkeys(warnings))
    return {"violations": violations, "warnings": warnings, "diff_stat": stat}


# ==============================================================================
# ローカル Unity テスト（一次ゲート）
# ==============================================================================
def unity_editor_running() -> bool:
    """Unity エディタが開いていると batchmode は Library ロックで必ず失敗する。"""
    try:
        proc = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             "(Get-Process Unity -ErrorAction SilentlyContinue | Measure-Object).Count"],
            capture_output=True, text=True, timeout=30)
        return int((proc.stdout or "0").strip() or 0) > 0
    except Exception:
        return False  # 判定不能ならテスト実行側の失敗に委ねる


def parse_nunit(xml_path: Path) -> dict:
    root = ET.parse(str(xml_path)).getroot()

    def as_int(key):
        try:
            return int(root.get(key) or 0)
        except ValueError:
            return 0

    failed_names = [
        tc.get("fullname") for tc in root.iter("test-case")
        if tc.get("result") in ("Failed", "Error")
    ]
    return {
        "total": as_int("total"),
        "passed": as_int("passed"),
        "failed": as_int("failed"),
        "skipped": as_int("skipped") + as_int("inconclusive"),
        "duration": float(root.get("duration") or 0.0),
        "failed_names": failed_names[:20],
    }


def run_unity_tests(platform: str, stamp: str) -> dict:
    """platform は 'EditMode' または 'PlayMode'。"""
    results = NIGHTLY_LOG_DIR / f"tests-{platform.lower()}-{stamp}.xml"
    logfile = NIGHTLY_LOG_DIR / f"unity-{platform.lower()}-{stamp}.log"

    cmd = [
        str(UNITY_EXE),
        "-batchmode",
        "-projectPath", str(GAME_PATH),
        "-runTests",
        "-testPlatform", platform,
        "-testResults", str(results),
        "-logFile", str(logfile),
        "-accept-apiupdate",
        "-silent-crashes",
    ]
    # PlayMode は uGUI / EventSystem を通すため -nographics を付けない
    if platform == "EditMode":
        cmd.insert(2, "-nographics")

    record = {"platform": platform, "results_xml": str(results), "log": str(logfile)}
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=TEST_TIMEOUT_SEC)
        record["exit_code"] = proc.returncode
    except subprocess.TimeoutExpired:
        record["exit_code"] = -1
        record["error"] = f"{TEST_TIMEOUT_SEC} 秒でタイムアウト"
        return record
    except FileNotFoundError:
        record["exit_code"] = -2
        record["error"] = f"Unity 実行ファイルが見つからない: {UNITY_EXE}"
        return record

    if not results.exists():
        record["error"] = "テスト結果 XML が生成されなかった（ライセンス／Library ロックの疑い）"
        return record

    try:
        record.update(parse_nunit(results))
    except Exception as exc:
        record["error"] = f"NUnit XML の解析に失敗: {exc}"
    return record


def load_baseline() -> dict:
    try:
        data = json.loads(BASELINE_PATH.read_text(encoding="utf-8"))
        return {"EditMode": int(data.get("EditMode", 0)), "PlayMode": int(data.get("PlayMode", 0))}
    except Exception:
        return {"EditMode": 0, "PlayMode": 0}


def save_baseline(values: dict) -> None:
    payload = {
        "_comment": "テスト件数の下限。エージェントがテストを削って緑にする経路を塞ぐ。"
                    "実測で上振れしたら nightly_gate が自動で引き上げる。",
        "EditMode": values["EditMode"],
        "PlayMode": values["PlayMode"],
    }
    BASELINE_PATH.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def evaluate_tests(stamp: str) -> dict:
    """EditMode -> PlayMode の順に実行し、合否と理由を返す。"""
    baseline = load_baseline()
    results = {}
    reasons = []
    ok = True

    for platform in ("EditMode", "PlayMode"):
        record = run_unity_tests(platform, stamp)
        results[platform] = record

        if "error" in record:
            ok = False
            reasons.append(f"{platform}: {record['error']}")
            break
        if record.get("failed", 0) > 0:
            ok = False
            reasons.append(
                f"{platform}: {record['failed']} 件失敗 -> "
                + ", ".join(record.get("failed_names", [])))
            break
        if record.get("skipped", 0) > 0:
            ok = False
            reasons.append(f"{platform}: {record['skipped']} 件が skip/inconclusive（無効化の疑い）")
            break
        if record.get("total", 0) < baseline.get(platform, 0):
            ok = False
            reasons.append(
                f"{platform}: テスト件数がベースラインを下回った"
                f"（{record.get('total', 0)} < {baseline.get(platform, 0)}）。テスト削除の疑い")
            break

    if ok:
        updated = {
            "EditMode": max(baseline["EditMode"], results["EditMode"].get("total", 0)),
            "PlayMode": max(baseline["PlayMode"], results["PlayMode"].get("total", 0)),
        }
        if updated != baseline:
            save_baseline(updated)

    return {"ok": ok, "reasons": reasons, "results": results, "baseline": baseline}


# ==============================================================================
# サイクル制御
# ==============================================================================
@dataclass
class Cycle:
    cycle_id: str
    ts_start: str
    snapshot: str
    target_task: str = ""
    quotas: dict = field(default_factory=dict)


def _stamp() -> str:
    return datetime.now().strftime("%Y%m%dT%H%M%S")


def jsonl_path(date_str=None) -> Path:
    date_str = date_str or datetime.now().strftime("%Y%m%d")
    return NIGHTLY_LOG_DIR / f"cycles-{date_str}.jsonl"


def _write_record(record: dict) -> None:
    with open(jsonl_path(), "a", encoding="utf-8") as handle:
        handle.write(json.dumps(record, ensure_ascii=False) + "\n")


def begin_cycle(target_task: str = "", quotas: dict = None):
    """クリーンな状態からのみサイクルを開始する。汚れていたら None を返す（人間の作業を守る）。"""
    if is_dirty():
        _write_record({
            "cycle_id": _stamp(),
            "ts_start": datetime.now().isoformat(timespec="seconds"),
            "ts_end": datetime.now().isoformat(timespec="seconds"),
            "verdict": VERDICT_ABORTED_DIRTY,
            "reasons": ["サイクル開始時にワーキングツリーが未コミット状態だった。"
                        "人間の作業を上書きしないため、エージェントを起動せず中止した。"],
            "dirty_files": git("status", "--porcelain").strip().splitlines()[:30],
            "diff_stat": {"files": 0, "insertions": 0, "deletions": 0},
        })
        return None

    return Cycle(
        cycle_id=_stamp(),
        ts_start=datetime.now().isoformat(timespec="seconds"),
        snapshot=head_sha(),
        target_task=target_task,
        quotas=quotas or {},
    )


def _quarantine_and_rollback(cycle: Cycle, work_head: str) -> str:
    """成果物を隔離ブランチに保全してからスナップショットへ巻き戻す。作業は失われない（I-4）。"""
    branch = f"nightly-reject/{cycle.cycle_id}"
    git("branch", "-f", branch, work_head)
    git("reset", "--hard", cycle.snapshot)
    return branch


def finalize_cycle(cycle: Cycle, agent_ok: bool = True) -> dict:
    record = {
        "cycle_id": cycle.cycle_id,
        "ts_start": cycle.ts_start,
        "ts_end": datetime.now().isoformat(timespec="seconds"),
        "target_task": cycle.target_task,
        "snapshot": cycle.snapshot,
        "agent_ok": agent_ok,
        "quotas": cycle.quotas,
        "reasons": [],
        "warnings": [],
        "quarantine_branch": None,
        "pushed_branch": None,
        "tests": {},
        "diff_stat": {"files": 0, "insertions": 0, "deletions": 0},
    }

    # 1. 未コミットの成果物も含めて一旦コミットし、保全対象にする（I-4 の前提）
    if is_dirty():
        git("add", "-A")
        git("commit", "-m", f"nightly: uncommitted agent output [cycle {cycle.cycle_id}]",
            check=False)

    work_head = head_sha()
    record["work_head"] = work_head

    if work_head == cycle.snapshot:
        record["verdict"] = VERDICT_NO_CHANGE
        record["reasons"].append("差分なし。エージェントは何も変更しなかった。")
        _write_record(record)
        return record

    record["commits"] = git(
        "log", "--oneline", f"{cycle.snapshot}..{work_head}", check=False).strip().splitlines()

    # 2. ポリシー検査（ハックコード検知）
    policy = check_policy(cycle.snapshot, work_head)
    record["diff_stat"] = policy["diff_stat"]
    record["warnings"] = policy["warnings"]
    if policy["violations"]:
        record["verdict"] = VERDICT_REJECT_POLICY
        record["reasons"] = policy["violations"]
        record["quarantine_branch"] = _quarantine_and_rollback(cycle, work_head)
        _write_record(record)
        return record

    # 3. ローカル Unity で検証できるか（I-5）
    if unity_editor_running():
        record["verdict"] = VERDICT_UNVERIFIED
        record["reasons"].append(
            "Unity エディタが起動中のため batchmode テストを実行できなかった。"
            "未検証のコードは受理しない方針（I-5）により巻き戻した。")
        record["quarantine_branch"] = _quarantine_and_rollback(cycle, work_head)
        _write_record(record)
        return record

    # 4. テスト実行（一次ゲート）
    verdict_tests = evaluate_tests(cycle.cycle_id)
    record["tests"] = verdict_tests["results"]
    if not verdict_tests["ok"]:
        record["verdict"] = VERDICT_REJECT_TESTS
        record["reasons"] = verdict_tests["reasons"]
        record["quarantine_branch"] = _quarantine_and_rollback(cycle, work_head)
        _write_record(record)
        return record

    # 5. 受理。I-2 により origin/main ではなく nightly/<date> へ push する
    record["verdict"] = VERDICT_ACCEPT
    nightly_branch = f"nightly/{datetime.now().strftime('%Y-%m-%d')}"
    push = subprocess.run(
        ["git", "push", "origin", f"HEAD:refs/heads/{nightly_branch}"],
        cwd=str(PROJECT_ROOT), capture_output=True, text=True,
        encoding="utf-8", errors="replace")
    if push.returncode == 0:
        record["pushed_branch"] = nightly_branch
    else:
        record["warnings"].append(
            f"nightly ブランチへの push に失敗（ローカルには受理済み）: {push.stderr.strip()[:300]}")

    _write_record(record)
    return record


# ==============================================================================
# セルフテスト（破壊的操作を一切行わない）
# ==============================================================================
def selftest() -> int:
    print(f"PROJECT_ROOT        : {PROJECT_ROOT}")
    print(f"UNITY_EXE           : {UNITY_EXE}  exists={UNITY_EXE.exists()}")
    print(f"HEAD                : {head_sha()}")
    print(f"dirty               : {is_dirty()}")
    print(f"unity running       : {unity_editor_running()}")
    print(f"baseline            : {load_baseline()}")
    parent = git("rev-parse", "HEAD~1", check=False).strip()
    if parent:
        policy = check_policy(parent, head_sha())
        print(f"policy(HEAD~1..HEAD): violations={len(policy['violations'])} "
              f"warnings={len(policy['warnings'])} stat={policy['diff_stat']}")
        for item in policy["violations"][:5]:
            print(f"  [VIOLATION] {item}")
        for item in policy["warnings"][:5]:
            print(f"  [WARN] {item}")
    print("selftest OK")
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "selftest":
        sys.exit(selftest())
    print(__doc__)
    sys.exit(0)
```

### 3-C. `scripts/morning_report.py`（新規作成・全文）

```python
# SPDX-AI-Disclosure: ai-generated
"""朝刊レポート生成。夜間サイクルの JSONL を 1 画面の Markdown に畳む。

  python scripts/morning_report.py            # 今日の分
  python scripts/morning_report.py 20260904   # 日付指定

出力: docs/nightly/YYYY-MM-DD.md（追跡対象。朝、人間はこれ 1 枚だけ読む）
"""
from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
NIGHTLY_LOG_DIR = PROJECT_ROOT / "logs" / "nightly"
REPORT_DIR = PROJECT_ROOT / "docs" / "nightly"
REPORT_DIR.mkdir(parents=True, exist_ok=True)

LABEL = {
    "ACCEPT": "受理",
    "NO_CHANGE": "変化なし",
    "REJECT_POLICY": "隔離(規約)",
    "REJECT_TESTS": "隔離(テスト)",
    "UNVERIFIED": "未検証",
    "ABORTED_DIRTY": "中止(作業中)",
}


def git(*args: str) -> str:
    proc = subprocess.run(["git", *args], cwd=str(PROJECT_ROOT),
                          capture_output=True, text=True, encoding="utf-8", errors="replace")
    return proc.stdout if proc.returncode == 0 else ""


def load_cycles(date_key: str) -> list:
    path = NIGHTLY_LOG_DIR / f"cycles-{date_key}.jsonl"
    if not path.exists():
        return []
    records = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line:
            try:
                records.append(json.loads(line))
            except json.JSONDecodeError:
                pass
    return records


def build_report(date_key: str) -> str:
    cycles = load_cycles(date_key)
    pretty_date = f"{date_key[0:4]}-{date_key[4:6]}-{date_key[6:8]}"

    counts = {key: 0 for key in LABEL}
    for cycle in cycles:
        verdict = cycle.get("verdict", "UNVERIFIED")
        counts[verdict] = counts.get(verdict, 0) + 1

    lines = []
    lines.append(f"# 夜間自律開発 朝刊 — {pretty_date}")
    lines.append("")
    lines.append(f"生成: {datetime.now().isoformat(timespec='seconds')} / サイクル数: {len(cycles)}")
    lines.append("")

    # ---- 1. 結論 ----
    lines.append("## 1. 結論")
    lines.append("")
    if not cycles:
        lines.append("**夜間ランナーが 1 サイクルも実行されていない。** "
                     "Task Scheduler（`UnityProject_AutoRunner`）の状態を確認すること。")
    else:
        lines.append(
            f"**受理 {counts.get('ACCEPT', 0)} / "
            f"隔離 {counts.get('REJECT_POLICY', 0) + counts.get('REJECT_TESTS', 0)} / "
            f"未検証 {counts.get('UNVERIFIED', 0)} / "
            f"変化なし {counts.get('NO_CHANGE', 0)} / "
            f"中止 {counts.get('ABORTED_DIRTY', 0)}**")
        if counts.get("ABORTED_DIRTY", 0):
            lines.append("")
            lines.append("> 未コミットの作業が残っていたためサイクルが中止された。"
                         "寝る前にコミットするか `git stash` すること。")
    lines.append("")

    # ---- 2. テスト最終実測値 ----
    latest_tests = None
    for cycle in reversed(cycles):
        if cycle.get("tests"):
            latest_tests = cycle["tests"]
            break
    lines.append("## 2. テスト最終実測値")
    lines.append("")
    if latest_tests:
        lines.append("| 種別 | 実行 | 成功 | 失敗 | skip | 秒 |")
        lines.append("|---|---|---|---|---|---|")
        for platform, data in latest_tests.items():
            lines.append(
                f"| {platform} | {data.get('total', '?')} | {data.get('passed', '?')} | "
                f"{data.get('failed', '?')} | {data.get('skipped', '?')} | "
                f"{data.get('duration', 0):.0f} |")
    else:
        lines.append("この夜はテストが 1 度も実行されていない。")
    lines.append("")

    # ---- 3. サイクル一覧 ----
    lines.append("## 3. サイクル一覧")
    lines.append("")
    lines.append("| # | 時刻 | 判定 | 対象タスク | 差分 | 備考 |")
    lines.append("|---|---|---|---|---|---|")
    for index, cycle in enumerate(cycles, start=1):
        verdict = cycle.get("verdict", "?")
        stat = cycle.get("diff_stat", {})
        note = (cycle.get("reasons") or [""])[0].replace("|", "/")
        lines.append(
            f"| {index} | {cycle.get('ts_start', '')[11:16]} | {LABEL.get(verdict, verdict)} | "
            f"{(cycle.get('target_task') or '-')[:40]} | "
            f"{stat.get('files', 0)}f +{stat.get('insertions', 0)}/-{stat.get('deletions', 0)} | "
            f"{note[:70]} |")
    lines.append("")

    # ---- 4. 人間の判断が要る項目 ----
    lines.append("## 4. 人間の判断が要る項目")
    lines.append("")
    todo = []

    for cycle in cycles:
        if not cycle.get("quarantine_branch"):
            continue
        reasons = "; ".join(cycle.get("reasons", []))[:300].replace("\n", " ")
        todo.append(
            f"**隔離ブランチ `{cycle['quarantine_branch']}`** — {reasons}\n"
            f"    - 中身: `git log --oneline {cycle['snapshot'][:8]}..{cycle['quarantine_branch']}`\n"
            f"    - 差分: `git diff {cycle['snapshot'][:8]}..{cycle['quarantine_branch']}`\n"
            f"    - 救う: `git cherry-pick <sha>` / 捨てる: `git branch -D {cycle['quarantine_branch']}`")

    for cycle in cycles:
        for warning in cycle.get("warnings", []):
            todo.append(f"確認: {warning}")

    # 同一タスクの連続失敗 = 人間が仕様を決めないと先へ進めない兆候
    failed_tasks = {}
    for cycle in cycles:
        if cycle.get("verdict") in ("REJECT_POLICY", "REJECT_TESTS", "UNVERIFIED"):
            key = cycle.get("target_task") or "(不明)"
            failed_tasks[key] = failed_tasks.get(key, 0) + 1
    for task, count in failed_tasks.items():
        if count >= 2:
            todo.append(
                f"**同じタスクで {count} 回連続失敗: 「{task[:80]}」** — "
                "自律では抜けられない。人間が仕様か設計を 1 つ決める必要がある。")

    if todo:
        for item in todo:
            lines.append(f"- {item}")
    else:
        lines.append("- なし。そのまま進めてよい。")
    lines.append("")

    # ---- 5. 朝の 3 コマンド ----
    unpushed = git("log", "--oneline", "origin/main..main").strip().splitlines()
    lines.append("## 5. 朝の 3 コマンド")
    lines.append("")
    lines.append(f"受理済みで未 push のコミット: **{len(unpushed)} 本**")
    lines.append("")
    lines.append("```")
    lines.append("git log --oneline origin/main..main      # 何が積まれたか読む")
    lines.append("git push origin main                     # 問題なければ push（クラウド CI が走る）")
    lines.append('git branch --list "nightly-reject/*"     # 隔離されたものを確認')
    lines.append("```")
    lines.append("")
    if unpushed:
        lines.append("<details><summary>未 push コミット一覧</summary>")
        lines.append("")
        lines.append("```")
        lines.extend(unpushed[:50])
        lines.append("```")
        lines.append("")
        lines.append("</details>")
        lines.append("")

    # ---- 6. クラウド CI ----
    lines.append("## 6. クラウド CI")
    lines.append("")
    lines.append("- Actions: https://github.com/tamaroulet/unity-2d-project/actions")
    lines.append("- private リポジトリのため **Actions 無料枠は月 2,000 分**。"
                 "残量は Settings → Billing で確認する。")
    lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("*このレポートは `scripts/morning_report.py` が自動生成した。手で編集しない。*")
    return "\n".join(lines) + "\n"


def main() -> int:
    date_key = sys.argv[1] if len(sys.argv) > 1 else datetime.now().strftime("%Y%m%d")
    pretty_date = f"{date_key[0:4]}-{date_key[4:6]}-{date_key[6:8]}"
    out_path = REPORT_DIR / f"{pretty_date}.md"
    out_path.write_text(build_report(date_key), encoding="utf-8")
    print(f"[morning_report] wrote {out_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

### 3-D. `scripts/auto_runner.py` の改修（**差分のみ**・全書き換え禁止）

既存ファイルに対して、次の 3 箇所**だけ**を変更する。他の行には触れない。

**(D-1) import 追加** — `from pathlib import Path` の直後に 1 行:

```python
from nightly_gate import begin_cycle, finalize_cycle, VERDICT_ACCEPT
```

**(D-2) プロンプトへの禁止事項の追加** — `get_next_prompt()` 内の f-string、
`【作業規律】` ブロックの `3. docs/STATUS.md ...` の行の**直後**に次を挿入する:

```
【夜間モードの絶対禁止事項（違反した成果物は自動的に隔離され、main から巻き戻される）】
1. `git push origin main` を実行してはならない。push は安全ハーネスが nightly/<日付> ブランチへ行う。
2. テストを緑にするためにテストコードを弱めてはならない
   （[Ignore] / [Explicit] / Assert.Pass / Assert.Ignore / アサーション削除 / テストファイル削除）。
   赤は情報である。直せないなら「直せない理由」をコミットメッセージに書いて止まれ。
3. 次のファイルを変更してはならない: .github/workflows/**, .claude/**, .agents/rules/**,
   scripts/nightly_gate.py, scripts/morning_report.py, scripts/nightly_baseline.json, scripts/auto_runner.py
4. ランタイムコードに #if UNITY_EDITOR / AssetDatabase / UnityEditor / Find 系のシーン検索を入れてはならない。
5. 1 サイクルの変更量は 3,000 行以内に収めること。それ以上は暴走とみなして自動的に巻き戻される。
6. 作業が終わったら必ずコミットまで済ませること（push は不要）。
```

**(D-3) `main()` の差し替え** — 既存の `async def main():` の本体を次で置き換える:

```python
async def main():
    log("=" * 60)
    log("[START] AutoRunner 計画駆動型自律実行サイクル開始")

    # 1. クォータ取得
    quotas = get_quotas()
    if quotas:
        claude = quotas.get("Claude", {})
        gemini = quotas.get("Gemini", {})
        log(f"[QUOTA] Claude: session={claude.get('SessionUsedPercent', '?')}% used, "
            f"weekly={claude.get('WeeklyUsedPercent', '?')}% used")
        log(f"[QUOTA] Gemini: 5h={gemini.get('FiveHourRemainingPercent', '?')}% remaining, "
            f"weekly={gemini.get('WeeklyRemainingPercent', '?')}% remaining")
    else:
        log("[QUOTA] クォータ取得失敗。安全デフォルトで続行。")

    # 2. 計画ロードマップと指示書から具体的プロンプトを構築
    prompt, remaining_tasks = get_next_prompt(quotas)
    target_task = remaining_tasks[0]["task"] if remaining_tasks else "(検証・同期タスク)"
    log(f"[PLAN] 残り未完了タスク数: {len(remaining_tasks)} 件")
    log(f"[PLAN] 今回のターゲット: {target_task}")

    # 3. 安全ハーネス: クリーンな状態からのみ開始する
    cycle = begin_cycle(target_task=target_task, quotas=quotas)
    if cycle is None:
        log("[GATE] ワーキングツリーが未コミット状態のためサイクルを中止した。人間の作業を上書きしない。")
        log("=" * 60)
        return
    log(f"[GATE] cycle={cycle.cycle_id} snapshot={cycle.snapshot[:8]}")

    # 4. エージェント起動（SDK 優先、CLI フォールバック）
    result = await run_with_sdk(prompt)
    if result is None:
        result = run_with_cli_fallback(prompt)

    # 5. 安全ハーネス: 検査 -> 受理 or 隔離＋巻き戻し
    record = finalize_cycle(cycle, agent_ok=result is not None)
    verdict = record["verdict"]
    log(f"[GATE] verdict={verdict} diff={record['diff_stat']}")
    for reason in record.get("reasons", []):
        log(f"[GATE]   理由: {reason}")
    for warning in record.get("warnings", []):
        log(f"[GATE]   注意: {warning}")
    if record.get("quarantine_branch"):
        log(f"[GATE] 成果物を隔離ブランチ {record['quarantine_branch']} に保全し、"
            f"{cycle.snapshot[:8]} へ巻き戻した。作業は失われていない。")
    if verdict == VERDICT_ACCEPT:
        log(f"[DONE] 受理。push 先: {record.get('pushed_branch') or '(ローカルのみ)'}")
    else:
        log("[HOLD] 受理せず。詳細は朝刊レポートを参照。")

    log("=" * 60)
```

> **既存の `ROADMAP_PLAN` / `get_quotas` / `should_use_claude` / `parse_instruction_uncompleted_tasks` /
> `run_with_sdk` / `run_with_cli_fallback` / `log` は 1 行も変更しない。**
> ここを触ると Hour 0-8 の成果が壊れる。

### 3-E. `scripts/register_morning_report_task.ps1`（新規作成・全文）

```powershell
# SPDX-AI-Disclosure: ai-generated
# 朝刊レポートを 06:10 に自動生成する Task Scheduler 登録スクリプト。
# 夜間ランナー（01:00-06:00）の終了直後に走る。
param(
    [string]$TaskName = "UnityProject_MorningReport",
    [string]$At = "06:10"
)

$projectRoot = "c:\dev\unity-2d-project"
$pythonExe = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $pythonExe) {
    $pythonExe = "C:\Users\tamar\AppData\Local\Programs\Python\Python312\python.exe"
}
$scriptPath = Join-Path $projectRoot "scripts\morning_report.py"

$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "[INFO] Unregistering existing task '$TaskName'..."
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$action = New-ScheduledTaskAction `
    -Execute $pythonExe `
    -Argument "`"$scriptPath`"" `
    -WorkingDirectory $projectRoot

$trigger = New-ScheduledTaskTrigger -Daily -At $At

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 10)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "unity-2d-project Morning Report (docs/nightly/YYYY-MM-DD.md)" `
    -Force | Out-Null

Write-Host "========================================"
Write-Host " Morning Report Task Registered"
Write-Host "========================================"
Write-Host "  Task Name: $TaskName"
Write-Host "  Runs at:   $At daily"
Write-Host "  Output:    docs\nightly\<date>.md"
Write-Host "========================================"
```

登録コマンド（Gemini が実行）:

```
powershell -ExecutionPolicy Bypass -NoProfile -File scripts\register_morning_report_task.ps1
```

### 3-F. `docs/nightly/.gitkeep` と `.gitignore` 追記

空ファイル `docs/nightly/.gitkeep` を作り、追跡する:

```
git add docs/nightly/.gitkeep
```

ルート `.gitignore` の `# Logs` ブロック（`logs/` と `*.log` の 2 行）の**直後**に、次の 1 行だけ追記する:

```
# 朝刊レポート（docs/nightly/）は logs/ と違い追跡対象。除外しないこと。
```

---

## 4. 【Gemini】受け入れ検証（この順に実行し、出力を §5 にそのまま貼る）

**§4 のすべてが緑にならない限り「完了」と報告してはならない。**

### 4-A. 安全ハーネスのセルフテスト（破壊的操作なし）

```
python scripts\nightly_gate.py selftest
```

**期待**: `UNITY_EXE ... exists=True` / `baseline: {'EditMode': 127, 'PlayMode': 1}` / `selftest OK`。

### 4-B. ポリシー検知が実際に効くことの証明（**最重要**）

「動くはずだ」で終わらせない。**わざと不正なコードを書き、隔離が発動することを実測する。**

1. Unity エディタを**閉じる**（開いているとテストが走らない）。
2. `Game/Assets/Tests/CommandResolverSOTests.cs` の**任意のテストメソッドの直前**に `[Ignore("harness verification")]` を 1 行足す。
3. 何もコミットせずに、次を実行する:

```
python -c "import sys;sys.path.insert(0,'scripts');import nightly_gate as g;c=g.begin_cycle('harness verification');print('cycle=',c);print(g.finalize_cycle(c) if c else 'DIRTY-ABORT')"
```

**期待される挙動**（これが出なければハーネスは機能していない）:
- `verdict` が **`REJECT_POLICY`**
- `reasons` に **「テストに [Ignore] 属性が追加された」** が含まれる
- `quarantine_branch` が `nightly-reject/<timestamp>` として作られている
- `git status` が **クリーンに戻っている**（`[Ignore]` が消えている）
- `git log --oneline <隔離ブランチ> -1` で**改変内容が保全されている**（＝失われていない）

4. 確認後、検証用の隔離ブランチを削除する:

```
git branch -D nightly-reject/<出力された timestamp>
```

> **注意**: 手順 3 のコマンドは実際に `[Ignore]` を巻き戻す。これは意図した動作である。
> 手順 2 で他のファイルを触っていないことを `git status` で必ず先に確認すること。

### 4-C. 正常系（ACCEPT）の証明

1. Unity エディタが閉じていることを確認。
2. `docs/nightly/.gitkeep` などの無害な変更を 1 つコミットしていない状態にして（＝ワーキングツリーをクリーンにして）、
   `README.md` の末尾に空行を 1 行足す。
3. 4-B と同じワンライナーを実行する。

**期待**: `verdict` が **`ACCEPT`**、`tests.EditMode.passed` が **127**、`tests.PlayMode.passed` が **1**、
`failed` が両方 **0**。所要時間も記録すること（夜間 30 分間隔に収まるかの判断材料になる）。

> `pushed_branch` は `nightly/2026-09-03` になるか、認証がなければ warnings に push 失敗が入る。
> **push 失敗は §4 の不合格ではない**（ローカル受理は成立している）。

### 4-D. 朝刊レポートの生成

```
python scripts\morning_report.py
```

**期待**: `docs/nightly/2026-09-03.md` が生成され、4-B / 4-C のサイクルが
「隔離(規約)」「受理」として表に出ていること。**生成された Markdown の §1〜§4 を §5 にそのまま貼る。**

### 4-E. スケジュール登録

```
powershell -ExecutionPolicy Bypass -NoProfile -File scripts\register_morning_report_task.ps1
schtasks /query /tn "UnityProject_AutoRunner" /fo LIST | findstr /i "TaskName Status Next"
schtasks /query /tn "UnityProject_MorningReport" /fo LIST | findstr /i "TaskName Status Next"
```

**期待**: 両方の Next Run Time が明日の 01:00 / 06:10 になっていること。

### 4-F. コミット

```
git add -A
git commit -m "feat(infra): GameCI パイプラインと夜間ランナー安全ハーネス・朝刊レポートを配備 (GATE5-01)"
```

**push はしない。** §1-C のとおり push は人間の判断。

---

## 5. 報告フォーマット（Gemini はこの書式で、これ以外を書かずに停止する）

```
## GATE5-01 実行報告

### §2 CI 配備
- 作成したワークフロー: （3 本のパス）
- YAML 検証結果: （2-D の出力そのまま）
- git add した .meta: （2-0 の結果）

### §3 安全ハーネス配備
- 作成/改修したファイル: （パスと行数）
- auto_runner.py の改修箇所: D-1 / D-2 / D-3 のうち実施したもの

### §4-A セルフテスト
（出力そのまま）

### §4-B ポリシー検知の実証  ★最重要
- verdict: 
- reasons: 
- quarantine_branch: 
- 巻き戻し後の git status: 
（出力そのまま）

### §4-C 正常系の実証
- verdict: 
- EditMode: X/127 passed, Y sec
- PlayMode: X/1 passed, Y sec
- サイクル全体の所要時間: X 分
（出力そのまま）

### §4-D 朝刊レポート
（生成された Markdown の §1〜§4 をそのまま貼る）

### §4-E スケジュール
（schtasks の出力そのまま）

### 未完了・ブロッカー
- （§1 は人間待ちである旨、その他あれば）
```

**人間が §1 完了後に追記する項目**（人間が §5 の末尾に書く）:
- Actions の今月の残り分数: ____ 分
- Settings → Pages の状態: 有効 / 無効 / Free では不可
- `unity-test.yml` の初回実行結果: 緑 / 赤（赤ならログの先頭 30 行）

---

## 6. 禁止事項（違反したら作業を止めて報告する）

1. **`git push origin main` を実行しない。** 18 コミットの push も、CI 配備後の push も人間の判断。
2. **`.meta` / `.asmdef` / `.unity` / `.asset` / `.prefab` をテキスト編集しない。** guard フックが拒否する。回避策を探すな。
3. **`Game/Assets` 配下のプロダクションコードを変更しない。** 今回は 1 行も触る必要がない。
4. **`SmokeTest.cs` および既存の EditMode テストを変更しない。** §4-B の `[Ignore]` は検証後に必ず巻き戻される（ハーネス自身が巻き戻す）。
5. **§4-B / §4-C が期待どおりでない場合、コードを直して緑にしようとしない。** 出力をそのまま貼って停止する。ハーネスの設計不良なら Claude が直す。
6. **セルフホストランナーへの切り替えを勝手に行わない。** §1-A の Plan B は人間の判断。
7. **`docs/log.md` / `docs/STATUS.md` を更新しない。** Claude が行う。
8. **`.github/workflows/deploy_pages.yml` を変更しない。** 既存の Pages 公開経路を壊さない。

---

## 7. 補足: この設計が守っている「手放し」の条件

| 人間が手放せるようになるもの | それを可能にしている仕組み |
|---|---|
| 「朝、コードが壊れていないか確認する」作業 | ローカル一次ゲート（I-1）＋ クラウド CI の二重化 |
| 「エージェントが変な直し方をしていないか diff を読む」作業 | `check_policy()` のハック検知（テスト弱体化・Find 再導入・自己改変） |
| 「壊れた時に手で git を戻す」作業 | 隔離ブランチ保全 → 自動巻き戻し（I-4）。**作業は失われない** |
| 「夜間に何が起きたか調べる」作業 | 朝刊レポート `docs/nightly/YYYY-MM-DD.md` |
| 「どこで人間の判断が要るか探す」作業 | 朝刊 §4「人間の判断が要る項目」＋ 同一タスク 2 回連続失敗の自動検出 |

**逆に、この設計が意図的に人間に残しているもの**（手放してはいけないもの）:

- `origin/main` へのマージ判断（I-2）
- 隔離された成果物を救うか捨てるかの判断
- 同一タスクで連続失敗したときの仕様決定
- ライセンス／課金／公開範囲の設定

> 「同じタスクで 2 回失敗したら人間を呼ぶ」は、**自律の限界を自律的に検出する**ための唯一の仕掛けである。
> ここを外すと、エージェントは一晩中同じ壁に頭をぶつけ続ける。
