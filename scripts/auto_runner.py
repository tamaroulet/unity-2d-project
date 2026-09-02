# SPDX-AI-Disclosure: ai-generated
"""
Antigravity SDK を使用した自律継続実行スクリプト。
Windows Task Scheduler から定期実行し、エージェントセッション終了後も
次のタスクを自動的に前倒し実行する。

使い方:
  python scripts/auto_runner.py
  
Windows Task Scheduler からの実行:
  powershell -ExecutionPolicy Bypass -NoProfile -File scripts/register_scheduled_task.ps1
"""
import asyncio
import json
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

# プロジェクトルート
PROJECT_ROOT = Path(__file__).resolve().parent.parent
LOG_DIR = PROJECT_ROOT / "logs" / "auto_runner"
LOG_DIR.mkdir(parents=True, exist_ok=True)


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


def get_next_prompt() -> str:
    """docs/STATUS.md と docs/instructions/ を読み取り、次に実行すべきプロンプトを構築する。"""
    status_path = PROJECT_ROOT / "docs" / "STATUS.md"
    status_text = status_path.read_text(encoding="utf-8") if status_path.exists() else ""

    # 基本プロンプト: ルールファイルを読み、STATUS.md に基づいて次のタスクを自律実行する
    prompt = f"""あなたは unity-2d-project の自律開発エージェントです。
以下の規律に従って作業を進めてください。

1. まず .agents/rules/00_role.md を読み、全行動規範を確認すること。
2. docs/STATUS.md を読み、現在の進捗と次に実行すべき Step を特定すること。
3. scripts/get_all_quotas.ps1 でクォータを確認し、Gemini 25% 未満なら Claude 委譲すること。
4. 特定した次の Step を自律的に実装・テスト・コミット・プッシュすること。
5. 完了後、docs/STATUS.md と docs/log.md を更新すること。

現在の STATUS.md:
---
{status_text[:2000]}
---

ノンストップ自律チェーン規律に従い、止まらずに作業を進めてください。
"""
    return prompt


def log(message: str):
    """ログファイルに書き込む。"""
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    log_file = LOG_DIR / f"run_{datetime.now().strftime('%Y%m%d')}.log"
    line = f"[{timestamp}] {message}\n"
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(line)
    print(line, end="")


async def run_with_sdk(prompt: str):
    """Antigravity Python SDK を使用してエージェントを起動する。"""
    from google.antigravity import Agent, LocalAgentConfig, CapabilitiesConfig

    log("[SDK] エージェントを起動中...")

    config = LocalAgentConfig(
        system_instructions=(
            "あなたは unity-2d-project の自律開発エージェントです。"
            ".agents/rules/00_role.md の全行動規範に従って作業してください。"
            "淡々とした工学的・事務的な平素の日本語で応答してください。"
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

            log(f"[SDK] エージェント完了。応答長: {len(full_text)} 文字")
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
            capture_output=True, text=True, timeout=1800,  # 30分タイムアウト
            cwd=str(PROJECT_ROOT)
        )
        if result.returncode == 0:
            log(f"[CLI] agy 完了。出力長: {len(result.stdout)} 文字")
            return result.stdout
        else:
            log(f"[CLI] agy 失敗: {result.stderr[:500]}")
    except FileNotFoundError:
        log("[CLI] agy コマンドが見つからない。SDK のみで実行。")
    except Exception as e:
        log(f"[CLI] agy 実行エラー: {e}")
    return None


async def main():
    log("=" * 60)
    log("[START] 自律継続実行スクリプト開始")

    # 1. クォータ確認
    quotas = get_quotas()
    if quotas:
        claude = quotas.get("Claude", {})
        gemini = quotas.get("Gemini", {})
        log(f"[QUOTA] Claude: session={claude.get('SessionUsedPercent', '?')}% used, "
            f"weekly={claude.get('WeeklyUsedPercent', '?')}% used, "
            f"available={claude.get('IsAvailable', '?')}")
        log(f"[QUOTA] Gemini: 5h={gemini.get('FiveHourRemainingPercent', '?')}% remaining, "
            f"weekly={gemini.get('WeeklyRemainingPercent', '?')}% remaining")

        # Gemini が枯渇寸前かどうか
        if should_use_claude(quotas):
            log("[QUOTA] Gemini 25% 未満。Claude 優先モードで実行。")
        else:
            log("[QUOTA] Gemini 残量十分。通常モードで実行。")
    else:
        log("[QUOTA] クォータ取得失敗。デフォルトモードで続行。")

    # 2. 次のプロンプトを構築
    prompt = get_next_prompt()
    log(f"[PROMPT] プロンプト構築完了 ({len(prompt)} 文字)")

    # 3. SDK でエージェント実行
    result = await run_with_sdk(prompt)

    # 4. SDK が失敗した場合、CLI フォールバック
    if result is None:
        result = run_with_cli_fallback(prompt)

    if result:
        log("[DONE] 自律実行完了。")
    else:
        log("[FAIL] SDK / CLI 両方失敗。次回スケジュールで再試行。")

    log("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
