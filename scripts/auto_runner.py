# SPDX-AI-Disclosure: ai-generated
"""
Antigravity SDK を使用した自律継続実行スクリプト。
Windows Task Scheduler から定期実行し、エージェントセッション終了後も
未完了のタスク・計画を自動的に検知して前倒し実行する。

使い方:
  python scripts/auto_runner.py
"""
import asyncio
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from nightly_gate import begin_cycle, finalize_cycle, VERDICT_ACCEPT

# プロジェクトルート
PROJECT_ROOT = Path(__file__).resolve().parent.parent
LOG_DIR = PROJECT_ROOT / "logs" / "auto_runner"
LOG_DIR.mkdir(parents=True, exist_ok=True)


# ==============================================================================
# 1. 構造化計画ロードマップ（Structured Roadmap & Validation Criteria）
# ==============================================================================
ROADMAP_PLAN = [
    {
        "id": "STEP_2_1_BOSS_ASSETS_AND_SCENE",
        "title": "マルチActボスデータ生成 & シーン完全バインド",
        "instruction_pattern": r"2\.1.*マルチActボスデータ生成",
        "check_files": [
            "Game/Assets/Features/Boss/Instances/Boss_Act1_01.asset",
            "Game/Assets/Features/Boss/Instances/Boss_Act2_01.asset",
            "Game/Assets/Features/Boss/Instances/Boss_Act3_01.asset",
            "Game/Assets/Features/Boss/Instances/Boss_Act4_01.asset",
            "Game/Assets/Features/Boss/Instances/BossCatalog.asset",
        ],
        "prompt_detail": (
            "Unity-MCP で Tools/Generate Boss Assets を実行して Act 1〜4 ボスを生成し、"
            "Tools/Bind Boss to MainGame Scene および Tools/Bind Meta Progression to MainGame Scene で"
            "MainGame.unity シーンに完全バインドしてください。"
        )
    },
    {
        "id": "STEP_2_2_TESTS_100_PERCENT_GREEN",
        "title": "全自動単体テスト・シミュレーション検証（127件 100% Green）",
        "instruction_pattern": r"2\.2.*全自動単体テスト",
        "prompt_detail": (
            "Unity-MCP で refresh_unity と run_tests (EditMode) を実行し、"
            "全 127 件のテスト（4連戦ボス＋周回メタ 1,000回シミュレーションを含む）が 100% Green で通過することを確認してください。"
        )
    },
    {
        "id": "STEP_2_3_WEBGL_BUILD",
        "title": "WebGL ビルド自動化スクリプトの実行と実ビルド検証",
        "instruction_pattern": r"2\.3.*WebGL ビルド",
        "check_files": [
            "Game/Builds/WebGL/index.html",
        ],
        "prompt_detail": (
            "Unity-MCP またはコマンドラインで WebGL ビルド（Tools/Build WebGL または WebGlBuildScript）を実行し、"
            "Game/Builds/WebGL に index.html, Build/*.wasm 等が正常生成されることを確認してください。"
        )
    },
    {
        "id": "STEP_2_4_GITHUB_PAGES_DEPLOY",
        "title": "GitHub Pages 公開整備・ブラウザ動作確認",
        "instruction_pattern": r"2\.4.*GitHub Pages",
        "prompt_detail": (
            "WebGL ビルド成果物を docs/webgl/ にコピーまたは GitHub Actions ワークフロー（.github/workflows/deploy.yml）を整備し、"
            "GitHub Pages 上でゲームが単体プレイ可能な状態を確立してください。"
        )
    },
    {
        "id": "STEP_2_5_DOCS_AND_SPEC_FINAL_SYNC",
        "title": "仕様書・成果物ドキュメントの最終同期とGitコミット",
        "instruction_pattern": r"2\.5.*仕様書・成果物ドキュメント",
        "prompt_detail": (
            "docs/STATUS.md, docs/log.md, docs/instructions/08_polish_and_balance.md, README.md を最新実績に合わせて完全同期し、"
            "Git コミット＆プッシュを実行してください。"
        )
    }
]


# ==============================================================================
# 2. クォータ・環境・指示書パース処理
# ==============================================================================
def get_quotas() -> dict:
    """scripts/get_all_quotas.ps1 を実行してクォータ情報を取得する。"""
    try:
        result = subprocess.run(
            ["powershell", "-ExecutionPolicy", "Bypass", "-NoProfile",
             "-File", str(PROJECT_ROOT / "scripts" / "get_all_quotas.ps1")],
            capture_output=True, text=True, timeout=30, cwd=str(PROJECT_ROOT)
        )
        if result.returncode == 0 and result.stdout.strip():
            return json.loads(result.stdout.strip())
    except Exception as e:
        log(f"[ERROR] クォータ取得失敗: {e}")
    return {}


def should_use_claude(quotas: dict) -> bool:
    """Gemini 5h 残量が 25% 未満なら True を返す。"""
    gemini = quotas.get("Gemini", {})
    remaining = gemini.get("FiveHourRemainingPercent", 100)
    return remaining < 25


def parse_instruction_uncompleted_tasks() -> list:
    """docs/instructions/*.md から未完了タスク (- [ ]) を順に抽出する。"""
    instructions_dir = PROJECT_ROOT / "docs" / "instructions"
    if not instructions_dir.exists():
        return []

    uncompleted = []
    for md_file in sorted(instructions_dir.glob("*.md")):
        lines = md_file.read_text(encoding="utf-8").splitlines()
        current_section = ""
        for line in lines:
            if line.startswith("### ") or line.startswith("## "):
                current_section = f"[{md_file.name}] " + line.lstrip("#").strip()
            elif line.strip().startswith("- [ ]"):
                task_text = line.strip().replace("- [ ]", "").strip()
                uncompleted.append({
                    "file": md_file.name,
                    "section": current_section,
                    "task": task_text,
                    "raw_line": line
                })

    return uncompleted


def sanitize_text(text: str) -> str:
    """Windows コンソール出力用に安全な文字列へ正規化する。"""
    return text.encode("utf-8", errors="ignore").decode("utf-8")


def get_recently_rejected_tasks() -> set:
    """本日 REJECT されたタスクのテキスト集合を返す（同一タスクでの足踏みを防ぐため）。"""
    today_key = datetime.now().strftime("%Y%m%d")
    cycle_file = PROJECT_ROOT / "logs" / "nightly" / f"cycles-{today_key}.jsonl"
    if not cycle_file.exists():
        return set()
    rejected = set()
    for line in cycle_file.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            rec = json.loads(line)
            if rec.get("verdict") in ("REJECT_TESTS", "REJECT_POLICY"):
                target = rec.get("target_task", "")
                if target:
                    rejected.add(target[:40])  # 前方一致
        except Exception:
            pass
    return rejected


def get_next_prompt(quotas: dict) -> tuple:
    """未完了タスクとロードマップを照合し、次に実行すべき高精度プロンプトを構築する。"""
    uncompleted_tasks = parse_instruction_uncompleted_tasks()
    rejected_tasks = get_recently_rejected_tasks()

    # REJECT されたタスクは後回しにし、未挑戦のタスクを最優先して先行実装を進める
    eligible_tasks = [
        t for t in uncompleted_tasks
        if not any(rej in t["task"] for rej in rejected_tasks)
    ]
    if not eligible_tasks and uncompleted_tasks:
        eligible_tasks = uncompleted_tasks  # 全て挑戦済みの場合は最初に戻る

    target_task_description = ""
    if eligible_tasks:
        target = eligible_tasks[0]
        target_task_description = f"【最優先実行目標】\nファイル: {target['file']}\nセクション: {target['section']}\nタスク: {target['task']}\n"
    else:
        target_task_description = "【最優先実行目標】\n全指示書タスクの検証・ドキュメント同期・ビルド健全性の確認\n"

    # クォータによる戦略分岐
    use_claude = should_use_claude(quotas)
    strategy_note = ""
    if use_claude:
        strategy_note = (
            "【重要・クォータ制限】Gemini 5時間枠が25%未満のため、"
            "重いC#実装やテスト作成が必要な場合は scripts/invoke_claude_safe.ps1 経由で Claude Code へ委譲してください。"
        )
    else:
        strategy_note = (
            "【クォータ状態】Gemini 枠は十分です。Gemini + Unity-MCP を主軸に自律実装・検証・コミットを進めてください。"
        )

    prompt = f"""あなたは unity-2d-project の自律開発エージェントです。
以下の具体的計画と行動規範に従って、直ちに作業を前倒し自律実行してください。

{target_task_description}

{strategy_note}

【作業規律】
1. .agents/rules/00_rules.md の全行動規範（平素な文体、ノンストップ自律チェーン、Unity-MCP検証）を遵守すること。
2. 作業完了後は必ず docs/instructions/08_polish_and_balance.md の対応するチェックボックスを - [x] に更新すること。
3. docs/STATUS.md および docs/log.md を同期し、Git コミット＆プッシュ（origin/main）まで同一ターンで完了させること。
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

【未完了タスク一覧】
{json.dumps([t['task'] for t in uncompleted_tasks[:5]], ensure_ascii=False, indent=2)}

【現在の STATUS.md 抜粋】
---
{status_text[:1500]}
---

止まることなく自律的に作業を完遂してください。
"""
    return prompt, uncompleted_tasks


def log(message: str):
    """ログファイルに書き込む。"""
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    log_file = LOG_DIR / f"run_{datetime.now().strftime('%Y%m%d')}.log"
    line = f"[{timestamp}] {message}\n"
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(line)
    print(line, end="")


# ==============================================================================
# 3. エージェント実行エンジン (SDK / CLI Fallback)
# ==============================================================================
async def run_with_sdk(prompt: str):
    """Antigravity Python SDK を使用してエージェントを起動する。"""
    from google.antigravity import Agent, LocalAgentConfig, CapabilitiesConfig

    log("[SDK] Antigravity SDK Agent を起動中...")

    config = LocalAgentConfig(
        system_instructions=(
            "あなたは unity-2d-project の自律開発エージェントです。"
            ".agents/rules/00_rules.md の全行動規範に従って作業してください。"
            "淡々とした工学的・事務的な平素の日本語で応答し、指示された計画タスクを確実に前倒し完遂してください。"
        ),
        capabilities=CapabilitiesConfig(),
    )

    try:
        async with Agent(config) as agent:
            response = await agent.chat(prompt)
            full_text = ""
            async for token in response:
                full_text += token
                sys.stdout.write(token)
                sys.stdout.flush()

            log(f"[SDK] エージェント実行完了。出力長: {len(full_text)} 文字")
            return full_text
    except Exception as e:
        log(f"[SDK] エージェント実行エラー: {e}")
        return None


def run_with_cli_fallback(prompt: str):
    """agy CLI が利用可能な場合のフォールバック実行。"""
    log("[CLI] agy CLI でのフォールバック実行を試行中...")
    try:
        result = subprocess.run(
            ["agy", "-p", prompt, "--dangerously-skip-permissions",
             "--output-format", "text"],
            capture_output=True, text=True, timeout=1800,
            cwd=str(PROJECT_ROOT)
        )
        if result.returncode == 0:
            log(f"[CLI] agy 完了。出力長: {len(result.stdout)} 文字")
            return result.stdout
        else:
            log(f"[CLI] agy 失敗: {result.stderr[:500]}")
    except FileNotFoundError:
        log("[CLI] agy コマンドが見つからないためスキップ。")
    except Exception as e:
        log(f"[CLI] agy 実行エラー: {e}")
    return None


def run_with_claude_fallback(prompt: str):
    """invoke_claude_safe.ps1 経由で Claude Code による自律実行を行う。本命: opus、代打: sonnet。"""
    script_path = PROJECT_ROOT / "scripts" / "invoke_claude_safe.ps1"

    # 1. 本命: Opus
    log("[Claude] 本命 Claude Code (Opus) による自律実行を開始中...")
    try:
        result = subprocess.run(
            ["powershell", "-ExecutionPolicy", "Bypass", "-NoProfile",
             "-File", str(script_path), "-Prompt", prompt, "-Model", "opus"],
            capture_output=True, text=True, timeout=1800,
            cwd=str(PROJECT_ROOT)
        )
        if result.returncode == 0 and "API Error: 529" not in result.stdout:
            log(f"[Claude] Opus 実行完了。出力長: {len(result.stdout)} 文字")
            return result.stdout
        else:
            log("[Claude] Opus が過負荷(529)または失敗。代打の Sonnet へフォールバックします。")
    except Exception as e:
        log(f"[Claude] Opus 実行エラー: {e}。代打の Sonnet へフォールバックします。")

    # 2. 代打: Sonnet
    log("[Claude] 代打 Claude Code (Sonnet) による自律実行を開始中...")
    try:
        result = subprocess.run(
            ["powershell", "-ExecutionPolicy", "Bypass", "-NoProfile",
             "-File", str(script_path), "-Prompt", prompt, "-Model", "sonnet"],
            capture_output=True, text=True, timeout=1800,
            cwd=str(PROJECT_ROOT)
        )
        if result.returncode == 0:
            log(f"[Claude] Sonnet 実行完了。出力長: {len(result.stdout)} 文字")
            return result.stdout
        else:
            log(f"[Claude] Sonnet 実行失敗: {result.stderr[:500]}")
    except Exception as e:
        log(f"[Claude] Sonnet 実行エラー: {e}")

    return None


# ==============================================================================
# 4. メインルーチン
# ==============================================================================
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

    # 4. エージェント起動（SDK 優先、CLI フォールバック、Claude フォールバック）
    result = await run_with_sdk(prompt)
    if result is None:
        result = run_with_cli_fallback(prompt)
    if result is None:
        result = run_with_claude_fallback(prompt)

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


if __name__ == "__main__":
    asyncio.run(main())
