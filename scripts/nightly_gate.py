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
VERDICT_AGENT_UNAVAILABLE = "AGENT_UNAVAILABLE"


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
    """人間の作業（未コミットの変更）があるか判定する。自動生成レポート等は除外する。"""
    lines = git("status", "--porcelain").splitlines()
    meaningful_changes = []
    for l in lines:
        cleaned = l.strip()
        # docs/nightly/ 配下の朝刊レポートやログファイルは人間の作業ではないため除外
        if "docs/nightly/" in cleaned or "logs/" in cleaned:
            continue
        meaningful_changes.append(l)
    return bool(meaningful_changes)


# ==============================================================================
# ポリシー定義（guard.js の禁止事項を「事後の diff」に適用したもの）
#   Gemini / agy にはフックが効かないため、成果物をコミット後に静的検査する。
# ==============================================================================

# 自己改変の禁止領域。ここが 1 行でも変わったら無条件で隔離する（00_rules.md「停止条件」/ 自己ガードレール保護）。
PROTECTED_PREFIXES = (
    ".github/workflows/",
    ".claude/hooks/",
    ".claude/settings.json",
    ".agents/rules/",
    "scripts/nightly_gate.py",
    "scripts/morning_report.py",
    "scripts/nightly_baseline.json",
    "scripts/auto_runner.py",
    # 依存の無断追加を物理的に封鎖する。UI Toolkit / VContainer / Rosalina 等は
    # manifest.json の 1 行で入る。00_rules.md は DI コンテナと Addressables を
    # 禁止しており、その制約はここで初めて実効化される。
    "Game/Packages/manifest.json",
    "Game/Packages/packages-lock.json",
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

# 秘密情報。拡張子を問わず全ファイルを検査する（漏洩は .md でも事故のため）。
SECRET_RULES = [
    (re.compile(r"\bghp_[A-Za-z0-9]{20,}|\bgithub_pat_[A-Za-z0-9_]{20,}"
                r"|\bsk-ant-[A-Za-z0-9_\-]{20,}|\bAKIA[0-9A-Z]{16}\b"),
     "資格情報らしき文字列がコミットに含まれている"),
]

# ガード迂回。ABUSE_RULES は「実際に実行されるファイル」にのみ適用する。
# 散文（.md）はルールを引用・禁止する文章そのものが正規表現に当たるため対象外にする。
# 例:「`git push origin main` を実行してはならない」という禁止文が違反判定され、
# 指示書や朝刊レポートを書いただけで次のサイクルが隔離されていた。
EXECUTABLE_EXT = re.compile(r"\.(py|ps1|psm1|sh|bash|yml|yaml|cmd|bat)$", re.IGNORECASE)

ABUSE_RULES = [
    (re.compile(r"git\s+push[^\n]*(--force|\s-f\b)"), "git push --force がスクリプトに埋め込まれた"),
    (re.compile(r"--no-verify"), "--no-verify によるフック迂回が埋め込まれた"),
    (re.compile(r"git\s+push[^\n]*\borigin\s+(main\b|HEAD:main\b|HEAD:refs/heads/main\b)"),
     "origin/main への直接 push が埋め込まれた（origin/main 直接 push の禁止）"),
    (re.compile(r"--dangerously-skip-permissions"), "権限スキップフラグが新たに埋め込まれた"),
]

# EditMode テストの純粋性（00_rules.md「テスト」）。
# EditMode は入力と出力が純粋な計算に限る。View を AddComponent して組み立てる
# テストは PlayMode で書く。private フィールドへの reflection は実装のフィールド名を
# 変えた瞬間に静かに壊れるため禁止する。
#
# private フィールドへの reflection は意図的に対象外にしている。00_rules.md が
# `.asset` のテキスト編集を禁じているため、ScriptableObject のフィクスチャを組む
# 唯一の手段が reflection であり、*SOFactory.cs が正規の用途で使っている。
EDITMODE_PURITY_RULES = [
    (re.compile(r"\bnew\s+GameObject\s*\("), "EditMode テストで GameObject を生成している"),
    (re.compile(r"\.AddComponent\s*<"), "EditMode テストで AddComponent している"),
]

# 00_rules.md が明記する 3 枚の例外。GameFlowController を器として使うが、
# 検証内容は状態遷移の純粋計算であるため許可されている。
EDITMODE_PURITY_ALLOWLIST = frozenset({
    "Game/Assets/Tests/GameFlowControllerTests.cs",
    "Game/Assets/Tests/GameFlowControllerRelicTests.cs",
    "Game/Assets/Tests/GameMonteCarloSimulationTests.cs",
})

# アーティファクトの無断作成。エージェントが作業メモや所感を .md で撒くのを防ぐ。
# 既存ファイルの編集は対象外で、あくまで「新規作成」だけを見る。
# 文書を置いてよい場所を列挙するのではなく、コードツリーへの散布を禁じる形にする。
# docs/ 配下は自由（そこが文書の置き場である）。禁じたいのは Game/ や scripts/ に
# 作業メモが湧くことなので、そちらを名指しで塞ぐ。
ARTIFACT_MD = re.compile(r"\.md$", re.IGNORECASE)
MD_FORBIDDEN_PREFIXES = ("Game/", "scripts/", "logs/")
MD_ALWAYS_ALLOWED_NAMES = ("README.md", "AGENTS.md", "CLAUDE.md")


def _md_allowed(path: str) -> bool:
    """新規 .md を置いてよい場所か。docs/ 配下は自由、コードツリーは規約ファイルのみ。"""
    if path.rsplit("/", 1)[-1] in MD_ALWAYS_ALLOWED_NAMES:
        return True
    if any(path.startswith(prefix) for prefix in MD_FORBIDDEN_PREFIXES):
        return False
    # リポジトリ直下の野良 .md も禁じる（docs/ に置くこと）
    return "/" in path


# コード行の上限。シーン等のシリアライズ資産は _diff_numstat_code_only が除外する。
MAX_CHANGED_LINES = 3000
# シリアライズ資産は行数ではなくファイル数で暴走を見る。
MAX_SERIALIZED_FILES = 20


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


def _diff_numstat_code_only(base: str, head: str) -> dict:
    """行数上限の判定用。Unity シリアライズ資産とバイナリを除外する。

    シーンやプレハブの YAML は人間が Unity エディタで生成する正規の成果物であり、
    UI レイアウトを 1 回組み直すだけで容易に数千行になる。これを暴走と同じ
    尺度で数えると、唯一の正規ルートがゲートに弾かれる。
    """
    out = git("diff", "--numstat", f"{base}..{head}", check=False)
    files = ins = dele = 0
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) < 3:
            continue
        added, deleted, path = parts[0], parts[1], parts[2]
        if not added.isdigit() or not deleted.isdigit():
            continue  # バイナリ
        if SERIALIZED.search(path):
            continue
        files += 1
        ins += int(added)
        dele += int(deleted)
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

    # 記録用は全ファイル集計。上限判定はコード行のみ（シリアライズ資産は別枠で見る）
    stat = _diff_numstat(base, head)
    code_stat = _diff_numstat_code_only(base, head)
    if code_stat["insertions"] + code_stat["deletions"] > MAX_CHANGED_LINES:
        violations.append(
            f"1 サイクルのコード変更量が上限を超過"
            f"（{code_stat['insertions'] + code_stat['deletions']} 行 > {MAX_CHANGED_LINES} 行"
            f"／シーン等のシリアライズ資産を除く）。"
            "暴走の疑いがあるため人間の確認が必要")

    serialized_changed = [p for _, p in _diff_name_status(base, head) if SERIALIZED.search(p)]
    if len(serialized_changed) > MAX_SERIALIZED_FILES:
        violations.append(
            f"シリアライズ資産の一括変更が {len(serialized_changed)} 件"
            f"（上限 {MAX_SERIALIZED_FILES} 件）。人間の確認が必要")

    for status, path in _diff_name_status(base, head):
        if any(path.startswith(prefix) for prefix in PROTECTED_PREFIXES):
            violations.append(f"保護対象の自己ガードレールが変更された: {path}（自己ガードレールの改変）")
        if status == "D" and path.startswith(TEST_PREFIX):
            violations.append(f"テストファイルが削除された: {path}")
        if status == "D" and SERIALIZED.search(path):
            violations.append(f"Unity シリアライズ資産が削除された: {path}")
        # asmdef の新設は 00_rules.md「アーキテクチャ」で禁止されている。
        # 他のシリアライズ資産と同じ warning 扱いでは制約が実効化されない。
        if status == "A" and path.lower().endswith(".asmdef"):
            violations.append(
                f"asmdef が新設された: {path}"
                "（00_rules.md はランタイム 1 枚 + Editor 1 枚 + テスト 2 枚のみを許可）")
        elif status in ("M", "A") and SERIALIZED.search(path):
            warnings.append(f"Unity シリアライズ資産が変更された（人間の目視確認が必要）: {path}")
        if status == "A" and ARTIFACT_MD.search(path) and not _md_allowed(path):
            violations.append(
                f"許可されていない場所に .md が新規作成された: {path}"
                "（指示書は docs/instructions/、調査は docs/research/ に置く）")

    for path, sign, text in _iter_diff_lines(base, head):
        if path is None:
            continue

        if sign == "+":
            # 秘密情報は拡張子を問わず全ファイルで弾く
            for pattern, message in SECRET_RULES:
                if pattern.search(text):
                    violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

            # ガード迂回は実行されるファイルのみ。散文中の引用・禁止文は違反にしない
            if EXECUTABLE_EXT.search(path):
                for pattern, message in ABUSE_RULES:
                    if pattern.search(text):
                        violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

            if path.startswith(TEST_PREFIX):
                for pattern, message in TEST_WEAKENING_RULES:
                    if pattern.search(text):
                        violations.append(f"{message} [{path}] -> {text.strip()[:120]}")

                # EditMode の純粋性。PlayMode 配下と 00_rules の例外 3 枚は対象外
                if (not path.startswith(TEST_PREFIX + "PlayMode/")
                        and path not in EDITMODE_PURITY_ALLOWLIST):
                    for pattern, message in EDITMODE_PURITY_RULES:
                        if pattern.search(text):
                            violations.append(
                                f"{message} [{path}] -> {text.strip()[:120]}"
                                "（View の検証は PlayMode で書く）")

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


BASELINE_DEFAULTS = {
    "EditMode": 0,
    "PlayMode": 0,
    "EditMode_known_skipped": 0,
    "PlayMode_known_skipped": 0,
}


def load_baseline() -> dict:
    try:
        data = json.loads(BASELINE_PATH.read_text(encoding="utf-8"))
        return {key: int(data.get(key, default))
                for key, default in BASELINE_DEFAULTS.items()}
    except Exception:
        return dict(BASELINE_DEFAULTS)


def save_baseline(values: dict) -> None:
    payload = {
        "_comment": "EditMode / PlayMode は合格件数(passed)の下限。"
                    "*_known_skipped は [Explicit] 等の恒久 skip の許容数で、人間だけが変更する。"
                    "自動引き上げの対象は passed のみ。total を入れてはならない"
                    "（恒久 skip の分だけ total > passed になり、次サイクルで永久 REJECT になる）。",
        "EditMode": values["EditMode"],
        "PlayMode": values["PlayMode"],
        "EditMode_known_skipped": values["EditMode_known_skipped"],
        "PlayMode_known_skipped": values["PlayMode_known_skipped"],
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
        # 既知の Explicit（PlayMode 移管予定）以外の不正な skip のみを弾く。
        # 許容数はマジックナンバーではなく nightly_baseline.json が持つ。
        known_skipped = baseline.get(f"{platform}_known_skipped", 0)
        if record.get("skipped", 0) > known_skipped:
            ok = False
            reasons.append(f"{platform}: {record['skipped']} 件が skip/inconclusive（無効化の疑い、既知={known_skipped}）")
            break
        if record.get("passed", 0) < baseline.get(platform, 0):
            ok = False
            reasons.append(
                f"{platform}: 合格テスト件数がベースラインを下回った"
                f"（{record.get('passed', 0)} < {baseline.get(platform, 0)}）。テスト削除の疑い")
            break

    if ok:
        # baseline は「合格件数の下限」。判定側が passed と比較するため total を入れない。
        # total を入れると恒久 skip（[Explicit]）の分だけ下限が passed を追い越し、
        # 以後すべてのサイクルが REJECT_TESTS になって復帰できなくなる。
        updated = {
            "EditMode": max(baseline["EditMode"], results["EditMode"].get("passed", 0)),
            "PlayMode": max(baseline["PlayMode"], results["PlayMode"].get("passed", 0)),
            # 恒久 skip の許容数は実測で自動更新しない（人間が明示的に決める）
            "EditMode_known_skipped": baseline["EditMode_known_skipped"],
            "PlayMode_known_skipped": baseline["PlayMode_known_skipped"],
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


def consecutive_reject_count(target_task: str) -> int:
    """同一タスクに対する REJECT_TESTS / REJECT_POLICY の連続回数を返す。"""
    if not target_task:
        return 0
    path = jsonl_path()
    if not path.exists():
        return 0
    
    count = 0
    prefix = target_task[:40]
    lines = path.read_text(encoding="utf-8").splitlines()
    for line in reversed(lines):
        if not line.strip():
            continue
        try:
            rec = json.loads(line)
            rec_task = rec.get("target_task", "")
            if rec_task.startswith(prefix):
                verdict = rec.get("verdict")
                if verdict in (VERDICT_REJECT_TESTS, VERDICT_REJECT_POLICY):
                    count += 1
                elif verdict == VERDICT_ACCEPT:
                    break
        except Exception:
            pass
    return count


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
    """成果物を隔離ブランチに保全してからスナップショットへ巻き戻す。作業は失われない。"""
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

    # 1. 未コミットの成果物も含めて一旦コミットし、隔離ブランチでの保全対象にする
    if is_dirty():
        git("add", "-A")
        git("commit", "-m", f"nightly: uncommitted agent output [cycle {cycle.cycle_id}]",
            check=False)

    work_head = head_sha()
    record["work_head"] = work_head

    if work_head == cycle.snapshot:
        if not agent_ok:
            record["verdict"] = VERDICT_AGENT_UNAVAILABLE
            record["reasons"].append("エージェントの起動に失敗した（SDK/CLI無応答）。成果物なし。")
        else:
            record["verdict"] = VERDICT_NO_CHANGE
            record["reasons"].append("差分なし。エージェントは正常起動したが変更を行わなかった。")
        _write_record(record)
        record["consecutive_rejects"] = consecutive_reject_count(cycle.target_task)
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
        record["consecutive_rejects"] = consecutive_reject_count(cycle.target_task)
        return record

    # 3. ローカル Unity で検証できるか（エディタ起動中は batchmode が使えない）
    if unity_editor_running():
        # 00_rules.md は人間の役割として Unity エディタ操作を定めている。
        # エディタが開いている＝人間が作業中であり、成果物を捨てる理由にはならない。
        # 検証できないものは「破棄」ではなく「保留」にし、作業ブランチ上に温存する。
        record["verdict"] = VERDICT_UNVERIFIED
        record["reasons"].append(
            "Unity エディタが起動中のため batchmode テストを実行できなかった。"
            "成果物は作業ブランチ上に保留する。エディタを閉じて再検査すること。")
        _write_record(record)
        record["consecutive_rejects"] = consecutive_reject_count(cycle.target_task)
        return record

    # 4. テスト実行（一次ゲート）
    verdict_tests = evaluate_tests(cycle.cycle_id)
    record["tests"] = verdict_tests["results"]
    if not verdict_tests["ok"]:
        record["verdict"] = VERDICT_REJECT_TESTS
        record["reasons"] = verdict_tests["reasons"]
        record["quarantine_branch"] = _quarantine_and_rollback(cycle, work_head)
        _write_record(record)
        record["consecutive_rejects"] = consecutive_reject_count(cycle.target_task)
        return record

    # 5. 受理。origin/main ではなく nightly/<date> へ push する
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
    record["consecutive_rejects"] = consecutive_reject_count(cycle.target_task)
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
