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
    "AGENT_UNAVAILABLE": "起動失敗",
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
            f"中止 {counts.get('ABORTED_DIRTY', 0)} / "
            f"起動失敗 {counts.get('AGENT_UNAVAILABLE', 0)}**")
        if counts.get("AGENT_UNAVAILABLE", 0):
            lines.append("")
            lines.append("> エージェントの起動に失敗したサイクルがあります（SDK/CLI無応答）。"
                         "logs/auto_runner/ のログを確認してください。")
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
