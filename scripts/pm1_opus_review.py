# SPDX-AI-Disclosure: ai-generated
"""
PM 1:00 帰宅時用: 留守中自律実行の成果物（受理・隔離ブランチ）一括 Opus 評価レポート生成スクリプト。
"""
import subprocess
from datetime import datetime
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
REPORT_DIR = PROJECT_ROOT / "docs" / "nightly"
REPORT_DIR.mkdir(parents=True, exist_ok=True)


def git(*args: str) -> str:
    proc = subprocess.run(["git", *args], cwd=str(PROJECT_ROOT),
                          capture_output=True, text=True, encoding="utf-8", errors="replace")
    return proc.stdout.strip() if proc.returncode == 0 else ""


def main():
    today_key = datetime.now().strftime("%Y-%m-%d")
    report_file = REPORT_DIR / f"{today_key}_pm1_review.md"

    # 1. 隔離ブランチ一覧の取得
    branches_raw = git("branch", "--list", "nightly-reject/*")
    branches = [b.strip().lstrip("* ") for b in branches_raw.splitlines() if b.strip()]

    # 2. main に積まれたコミット
    recent_commits = git("log", "-n", "10", "--oneline", "origin/main..main")
    if not recent_commits:
        recent_commits = git("log", "-n", "5", "--oneline")

    # 3. 隔離ブランチごとの差分概要
    quarantine_summaries = []
    for branch in branches:
        stat = git("diff", "--stat", f"main..{branch}")
        log_msg = git("log", "-n", "1", "--oneline", branch)
        quarantine_summaries.append(f"### ブランチ: `{branch}`\n- コミット: {log_msg}\n```\n{stat}\n```\n")

    # 4. Opus への一括評価プロンプト
    review_prompt = f"""あなたは unity-2d-project のアーキテクト（Claude Opus）です。
人間ディレクターの留守中（08:00〜13:00）に行われた自律開発サイクルの成果物を一括評価してください。

【メインブランチの直近状況】
{recent_commits}

【留守中に作成・隔離保全されたブランチ一覧（出来高）】
{chr(10).join(quarantine_summaries) if quarantine_summaries else '(隔離ブランチなし。全タスクが main に正常受理されたか、変更なし)'}

【指示】
1. 各隔離ブランチの成果物の実装意図と出来高を評価してください。
2. どのブランチの成果物を本流（main）に cherry-pick して合流すべきか、優先度をつけて提案してください。
3. 合流にあたって修正が必要な箇所（テスト修正、シーン配線など）があれば、具体的なコマンドとともに指示してください。
※平素で落ち着いた工学的なトーンで簡潔に回答してください。
"""

    # Opus または Sonnet でレビュー実行
    script_path = PROJECT_ROOT / "scripts" / "invoke_claude_safe.ps1"
    res = subprocess.run(
        ["powershell", "-ExecutionPolicy", "Bypass", "-NoProfile",
         "-File", str(script_path), "-Prompt", review_prompt, "-Model", "opus"],
        capture_output=True, text=True, timeout=1800, cwd=str(PROJECT_ROOT)
    )

    review_text = res.stdout if res.returncode == 0 and "API Error: 529" not in res.stdout else ""
    if not review_text:
        # 代打 Sonnet
        res_sonnet = subprocess.run(
            ["powershell", "-ExecutionPolicy", "Bypass", "-NoProfile",
             "-File", str(script_path), "-Prompt", review_prompt, "-Model", "sonnet"],
            capture_output=True, text=True, timeout=1800, cwd=str(PROJECT_ROOT)
        )
        review_text = res_sonnet.stdout if res_sonnet.returncode == 0 else "レビュー取得失敗"

    content = f"""# PM 1:00 留守中自律開発 Opus 総合評価レポート — {today_key}

生成: {datetime.now().isoformat(timespec='seconds')}

## 1. 成果物一覧（出来高）
- **隔離保全ブランチ数**: {len(branches)} 件
{chr(10).join(f"- `{b}`" for b in branches) if branches else "- なし"}

## 2. Claude Opus による一括評価と本流（main）合流判定
{review_text}
"""
    report_file.write_text(content, encoding="utf-8")
    print(f"Report generated: {report_file}")


if __name__ == "__main__":
    main()
