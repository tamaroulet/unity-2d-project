# SPDX-AI-Disclosure: ai-generated
"""セッション開始時のブリーフィングと、開発ログの自動生成。

Claude の枠を節約するための道具である。セッション冒頭に現状を把握するための
git / ログ / 指示書の走査を、1 コマンドにまとめる。

  python scripts/session_brief.py            # 現状を 1 画面で出す（読み取りのみ）
  python scripts/session_brief.py --log      # docs/log.md 追記用の下書きを標準出力へ

--log は追記を行わない。生成した下書きを人間か Claude が確認してから貼る。
自動追記にしないのは、log.md が「何を決めたか」の記録であり、
git 履歴から機械的に導けるのは「何をしたか」までだからである。
"""
from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
NIGHTLY_LOG_DIR = PROJECT_ROOT / "logs" / "nightly"
INSTRUCTIONS_DIR = PROJECT_ROOT / "docs" / "instructions"
BASELINE_PATH = PROJECT_ROOT / "scripts" / "nightly_baseline.json"

PROTECTED_PREFIXES = (
    ".github/workflows/", ".claude/hooks/", ".claude/settings.json",
    ".agents/rules/", "scripts/nightly_gate.py", "scripts/morning_report.py",
    "scripts/nightly_baseline.json", "scripts/auto_runner.py",
    "Game/Packages/manifest.json", "Game/Packages/packages-lock.json",
)


def git(*args: str) -> str:
    proc = subprocess.run(["git", *args], cwd=str(PROJECT_ROOT),
                          capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    return proc.stdout.rstrip() if proc.returncode == 0 else ""


def section(title: str) -> None:
    print()
    print(f"## {title}")


def collect_cycles(date_key: str) -> list:
    path = NIGHTLY_LOG_DIR / f"cycles-{date_key}.jsonl"
    if not path.exists():
        return []
    out = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip():
            try:
                out.append(json.loads(line))
            except Exception:
                pass
    return out


def collect_open_tasks() -> list:
    """docs/instructions/*.md の未完了チェックボックスをファイル別に数える。"""
    if not INSTRUCTIONS_DIR.exists():
        return []
    rows = []
    for md in sorted(INSTRUCTIONS_DIR.glob("*.md")):
        lines = md.read_text(encoding="utf-8").splitlines()
        open_n = sum(1 for l in lines if l.strip().startswith("- [ ]"))
        done_n = sum(1 for l in lines if l.strip().startswith("- [x]"))
        if open_n or done_n:
            rows.append((md.name, open_n, done_n))
    return rows


def brief() -> None:
    print(f"# セッションブリーフィング  {datetime.now().strftime('%Y-%m-%d %H:%M')}")

    section("ブランチと作業ツリー")
    print(f"branch : {git('branch', '--show-current')}")
    dirty = [l for l in git("status", "--porcelain").splitlines() if l.strip()]
    if dirty:
        protected = [l for l in dirty
                     if any(l[3:].strip().startswith(p) for p in PROTECTED_PREFIXES)]
        print(f"dirty  : {len(dirty)} 件" + ("  ★保護対象を含む" if protected else ""))
        for l in dirty[:15]:
            print(f"         {l}")
    else:
        print("dirty  : クリーン")

    section("直近のコミット")
    for l in git("log", "-8", "--format=%h %ad %s", "--date=short").splitlines():
        print(f"  {l}")

    section("未合流ブランチ")
    for br in git("branch", "--format=%(refname:short)").splitlines():
        br = br.strip()
        if not br or br == "main":
            continue
        ahead = git("rev-list", "--count", f"main..{br}")
        if ahead and ahead != "0":
            print(f"  {br}: main より {ahead} コミット先行")

    section("ハーネス判定（本日）")
    cycles = collect_cycles(datetime.now().strftime("%Y%m%d"))
    if not cycles:
        print("  本日の記録なし")
    else:
        counts = {}
        for c in cycles:
            v = c.get("verdict", "?")
            counts[v] = counts.get(v, 0) + 1
        print("  " + " / ".join(f"{k} {v}" for k, v in sorted(counts.items())))
        for c in cycles:
            if c.get("verdict") in ("REJECT_POLICY", "REJECT_TESTS", "UNVERIFIED"):
                for r in (c.get("reasons") or [])[:2]:
                    print(f"    - {r[:110]}")

    section("テストのベースライン")
    try:
        print("  " + json.dumps(json.loads(BASELINE_PATH.read_text(encoding="utf-8")),
                                ensure_ascii=False))
    except Exception as exc:
        print(f"  読み取り失敗: {exc}")

    section("指示書の残タスク")
    rows = collect_open_tasks()
    if not rows:
        print("  なし")
    for name, open_n, done_n in rows:
        mark = "  " if open_n == 0 else "->"
        print(f"  {mark} {name}: 未完了 {open_n} / 完了 {done_n}")

    section("実行者の状態")
    import os
    print(f"  GEMINI_API_KEY : {'設定済み' if os.environ.get('GEMINI_API_KEY') else '未設定（headless 実行は不可）'}")
    task = subprocess.run(
        ["powershell", "-NoProfile", "-Command",
         "(Get-ScheduledTask -TaskName 'UnityProject_AutoRunner' -ErrorAction SilentlyContinue).State"],
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(f"  夜間タスク     : {task.stdout.strip() or '未登録'}")


def log_draft() -> None:
    """docs/log.md 追記用の下書きを標準出力へ。追記自体は行わない。"""
    today = datetime.now().strftime("%Y-%m-%d")
    since = git("log", "-1", "--format=%H", "--before", f"{today} 00:00:00") or ""
    rng = f"{since}..HEAD" if since else "HEAD"

    print(f"### （見出しを書く）")
    print()
    print("- **実施内容**:")
    for line in git("log", rng, "--format=%h %s").splitlines():
        print(f"  - {line}")
    print()
    stat = git("diff", "--shortstat", rng)
    print(f"- **変更量**: {stat.strip() or '(なし)'}")
    print()

    cycles = collect_cycles(datetime.now().strftime("%Y%m%d"))
    if cycles:
        counts = {}
        for c in cycles:
            counts[c.get("verdict", "?")] = counts.get(c.get("verdict", "?"), 0) + 1
        print("- **ハーネス判定**: " + " / ".join(f"{k} {v}" for k, v in sorted(counts.items())))
        print()
    print("- **決定事項**: （git から導けないため人間か Claude が記述する）")


if __name__ == "__main__":
    if "--log" in sys.argv:
        log_draft()
    else:
        brief()
