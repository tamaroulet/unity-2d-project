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
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from nightly_gate import (
    begin_cycle, finalize_cycle, VERDICT_ACCEPT, VERDICT_UNVERIFIED,
    PROTECTED_PREFIXES)

# 自律ループの終了時刻（時）。Task Scheduler の稼働窓（01:00-06:00）と一致させる。
AUTO_RUN_END_HOUR = int(os.environ.get("AUTO_RUN_END_HOUR", "6"))

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
            "docs/status.md, docs/log.md, docs/instructions/08_polish_and_balance.md, README.md を最新実績に合わせて完全同期し、"
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
    status_path = PROJECT_ROOT / "docs" / "status.md"
    status_text = status_path.read_text(encoding="utf-8") if status_path.exists() else ""

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

    strategy_note = (
        "【役割】コーディング・エラー修正等すべての実装は Gemini が担います。Claude に実装を委譲せず自己完結してください。"
    )

    prompt = f"""あなたは unity-2d-project の自律開発エージェントです。
以下の具体的計画と行動規範に従って、直ちに作業を前倒し自律実行してください。

{target_task_description}

{strategy_note}

【役割の固定（docs/workflow/TRIAD_PROTOCOL.md 第1節）】
あなたは Executor（実装者）である。アーキテクトは Claude であり、あなたではない。
次の 4 つは、たとえ技術的に正しいと確信していても禁止する。
1. 別解・代替設計の提案（「UI Toolkit に移行したい」「YAML から脱却したい」等）。
   思いついた場合は実装せず、コミットメッセージの末尾に 1 行で書いて終わりにする。
2. 方針変更・独断でのリファクタ。指示書に書かれた変更だけを行う。
3. 指示書に列挙されていないファイルへの書き込み。
4. 作業メモ・所感・調査結果を新規 .md として撒くこと。
   .md の新規作成は docs/instructions/ と docs/research/ 配下のみ許可される。
指示書に書かれていない設計判断が必要になったら、推測で埋めず、手を止めて報告する。

【作業規律】
1. .agents/rules/development-rules.md の全行動規範（平素な文体、ノンストップ自律チェーン、Unity-MCP検証）を遵守すること。
2. 作業完了後は必ず docs/instructions/08_polish_and_balance.md の対応するチェックボックスを - [x] に更新すること。
3. docs/status.md および docs/log.md を同期し、Git コミット＆プッシュ（origin/main）まで同一ターンで完了させること。
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

【現在の status.md 抜粋】
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
AGY_TIMEOUT_SEC = 1800


def resolve_agy() -> str | None:
    """agy の実体を返す。見つからなければ None。

    インストーラは %LOCALAPPDATA%\\agy\\bin を User PATH レジストリに追加するが、
    その変更は既存のセッションには反映されない。タスクスケジューラ経由の実行では
    PATH に乗っている保証がないため、既定の設置場所も直接見に行く。
    """
    found = shutil.which("agy")
    if found:
        return found
    local = os.environ.get("LOCALAPPDATA")
    if local:
        candidate = Path(local) / "agy" / "bin" / "agy.exe"
        if candidate.exists():
            return str(candidate)
    return None


def run_with_agy(prompt: str):
    """Antigravity CLI (agy) を headless モードで実行する。

    agy は Gemini CLI の後継であり、Google アカウントのサブスク枠で動く
    （API キー不要）。--output-format json は status / response / usage を持つ
    エンベロープを返すので、成否をテキストの中身から推測しなくて済む。

    --dangerously-skip-permissions を付けているのは無人実行のためである。
    これを外すと承認待ちのツール呼び出しが soft-deny され、一晩何も進まない。
    暴走の抑止は nightly_gate.py の事後検査が担う。
    """
    agy = resolve_agy()
    if agy is None:
        log("[AGY] agy が見つからない。"
            "curl -fsSL https://antigravity.google/cli/install.cmd -o install.cmd で導入すること。")
        return None

    log(f"[AGY] Antigravity CLI を headless 実行中... ({agy})")
    try:
        result = subprocess.run(
            [agy, "-p", prompt,
             "--dangerously-skip-permissions",
             "--output-format", "json",
             "--print-timeout", f"{AGY_TIMEOUT_SEC}s"],
            capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=AGY_TIMEOUT_SEC + 120, cwd=str(PROJECT_ROOT),
        )
    except subprocess.TimeoutExpired:
        log(f"[AGY] {AGY_TIMEOUT_SEC} 秒でタイムアウト。")
        return None
    except Exception as exc:
        log(f"[AGY] 実行エラー: {exc}")
        return None

    if result.returncode != 0:
        log(f"[AGY] 異常終了 (exit={result.returncode}): {result.stderr.strip()[:400]}")
        return None

    try:
        envelope = json.loads(result.stdout.strip())
    except Exception:
        # JSON で返らなかった場合も、出力があるなら成果物とみなして先へ進める
        text = result.stdout.strip()
        if text:
            log(f"[AGY] JSON として解釈できなかったが出力あり。長さ: {len(text)} 文字")
            return text
        log("[AGY] 出力が空。")
        return None

    status = envelope.get("status")
    usage = envelope.get("usage", {})
    log(f"[AGY] status={status} turns={envelope.get('num_turns')} "
        f"duration={envelope.get('duration_seconds')}s "
        f"tokens={usage.get('total_tokens')} conv={envelope.get('conversation_id')}")

    if status != "SUCCESS":
        log(f"[AGY] 失敗: {str(envelope.get('error'))[:400]}")
        return None
    return envelope.get("response") or ""




# ==============================================================================
# 4. メインルーチン
# ==============================================================================
async def run_single_cycle():
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
    if not remaining_tasks:
        log("[PLAN] 全タスク完了。追加検証・ドキュメント同期を実行します。")
        target_task = "(全タスク完了後の検証・同期)"
    else:
        target_task = remaining_tasks[0]["task"]
        log(f"[PLAN] 残り未完了タスク数: {len(remaining_tasks)} 件")
        log(f"[PLAN] 今回のターゲット: {target_task}")

    # 3. 隔離環境の確保: main ではなく auto/wip ブランチで作業する
    current_branch = subprocess.run(["git", "branch", "--show-current"], capture_output=True, text=True, cwd=str(PROJECT_ROOT)).stdout.strip()
    if current_branch != "auto/wip":
        subprocess.run(["git", "checkout", "-B", "auto/wip"], cwd=str(PROJECT_ROOT))
        log("[BRANCH] 作業用隔離ブランチ auto/wip に切り替えました。本流(main)は一切汚しません。")

    cycle = begin_cycle(target_task=target_task, quotas=quotas)
    if cycle is None:
        # begin_cycle は「汚れていたら起動しない」ことで人間の作業を守る。
        # ここで無条件に commit すると保護対象の未コミット変更がスナップショット側に
        # 入り、policy 検査を素通りしてしまう。保護対象が汚れているときは中断する。
        dirty_out = subprocess.run(
            ["git", "status", "--porcelain"], capture_output=True, text=True,
            encoding="utf-8", errors="replace", cwd=str(PROJECT_ROOT)).stdout
        dirty_paths = [line[3:].strip() for line in dirty_out.splitlines() if line.strip()]
        protected_dirty = [p for p in dirty_paths
                           if any(p.startswith(prefix) for prefix in PROTECTED_PREFIXES)]
        if protected_dirty:
            log("[HALT] 保護対象ファイルが未コミットのまま残っている。審査なしで取り込むことは"
                "できないため中断する。人間がコミットまたは破棄すること: "
                + ", ".join(protected_dirty))
            return -1
        log("[GATE] 未コミットの作業を wip コミットに退避して継続する。")
        subprocess.run(["git", "add", "-A"], cwd=str(PROJECT_ROOT))
        subprocess.run(["git", "commit", "-m", "wip: save in-progress work"], cwd=str(PROJECT_ROOT))
        cycle = begin_cycle(target_task=target_task, quotas=quotas)

    if cycle:
        log(f"[GATE] cycle={cycle.cycle_id} snapshot={cycle.snapshot[:8]}")

    # 4. エージェント起動（Antigravity CLI）。実装の Claude 委譲は行わない
    result = run_with_agy(prompt)
    if result is None:
        log("[HALT] agy が起動不能または失敗。実装は Gemini 専任のため "
            "Claude への委譲は行わない。空コミットを避けるため本サイクルを中断する。")
        return -1

    # 5. 成果物のコミット（通し作業の優先: 途中で巻き戻さず auto/wip に積み上げる）
    subprocess.run(["git", "add", "-A"], cwd=str(PROJECT_ROOT))
    subprocess.run(["git", "commit", "-m", f"auto(wip): {target_task[:50]}"], cwd=str(PROJECT_ROOT))
    log(f"[PIPELINE] タスク「{target_task[:40]}」の実装を auto/wip にコミットしました。止まらず次へ進みます。")

    # 6. テスト実測（現状把握・評価用）
    if cycle:
        record = finalize_cycle(cycle, agent_ok=result is not None)
        log(f"[GATE] 現状テスト判定: verdict={record['verdict']} diff={record['diff_stat']}")
        if record["verdict"] == VERDICT_UNVERIFIED:
            # テストが物理的に走らない状態。回し続けても未検証の成果物が積むだけ
            log("[HALT] Unity エディタ起動中のため検証できない。成果物は保留のまま中断する。"
                "エディタを閉じてから再開すること。")
            return -1
        if record.get("consecutive_rejects", 0) >= 2:
            log("[HALT] 同一タスクで 2 回連続 REJECT。development-rules.md 停止条件により中断する。")
            log(f"理由: {', '.join(record.get('reasons', []))}")
            return -1

    return len(remaining_tasks)


async def main():
    log("=" * 60)
    log("[START] AutoRunner ノンストップ通し自律実行ループ開始")
    log("[POLICY] 隔離環境(auto/wip)上で止まらず前進。本流(main)は一切汚しません。")

    cycle_count = 0
    while True:
        now = datetime.now()
        # 稼働窓の終端に達したらループを抜ける。Task Scheduler の登録と同じ時刻を使う
        if now.hour >= AUTO_RUN_END_HOUR:
            log(f"[TIME] {AUTO_RUN_END_HOUR}:00 到達。自律作業ループを終了する。")
            break

        cycle_count += 1
        log(f"\n--- [CYCLE {cycle_count}] 通し自律実行ステップ ---")
        try:
            remaining = await run_single_cycle()
            if remaining < 0:
                log("[HALT] 中断シグナルを受信した。ループを終了し人間の判断を待つ。")
                break
            if remaining == 0:
                log("[COMPLETE] 全指示書タスクが完了。Opus の一括評価を待つ。")
                break
        except Exception as e:
            log(f"[ERROR] サイクル実行中エラー: {e}。隔離環境のため停止せず次へ進みます。")

        # インターバルを置かずに次タスクへ通しで即座に進む（10秒のクールダウンのみ）
        log("[CONTINUE] 止まらずに直ちに次のタスクへ通し実行を継続します...")
        await asyncio.sleep(10)

    log("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
