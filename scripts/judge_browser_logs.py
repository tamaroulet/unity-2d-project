#!/usr/bin/env python3
"""WebGL ブラウザログ判定スクリプト。

ブラウザから収集したログを受け取り、error / exception の有無を判定する。
対照群テストに対応し、空のログは「見ていない」と判断して赤（False）を返す。
"""

from __future__ import annotations

import argparse
import io
import json
import re
import sys
from pathlib import Path


DEFAULT_IGNORE_FILE = Path(__file__).resolve().parent / "webgl_log_ignore.json"


def load_ignore_patterns(ignore_file: Path | None = None) -> tuple[bool, list[dict], str]:
    """除外設定を読み込み、バリデーションを行う。
    
    Returns:
        (valid, patterns, error_message)
    """
    path = ignore_file or DEFAULT_IGNORE_FILE
    if not path.is_file():
        return True, [], ""

    try:
        with io.open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        return False, [], f"除外設定ファイルの読み込み失敗: {e}"

    if not isinstance(data, list):
        return False, [], "除外設定の形式が配列ではありません"

    # 止まる条件: 除外の一覧が 5 件を超える
    if len(data) > 5:
        return False, [], f"除外設定が 5 件を超えています（{len(data)} 件）。止まる条件に該当します。"

    # やってはいけないこと: 除外の一覧を理由と日付なしで増やす
    for item in data:
        if not isinstance(item, dict):
            return False, [], f"除外設定の項目が辞書ではありません: {item}"
        if not item.get("pattern"):
            return False, [], f"除外設定に pattern がありません: {item}"
        if not item.get("reason"):
            return False, [], f"除外設定に理由 (reason) がありません: {item}"
        if not item.get("date"):
            return False, [], f"除外設定に日付 (date) がありません: {item}"

    return True, data, ""


def judge_browser_logs(
    entries: list[dict],
    ignore_file: Path | None = None,
) -> tuple[bool, list[str]]:
    """ブラウザログ一覧を判定する。
    
    Args:
        entries: ブラウザログの辞書リスト（[{'level': '...', 'text': '...'}, ...]）
        ignore_file: 既知のエラー除外設定ファイルパス（省略時は scripts/webgl_log_ignore.json）
        
    Returns:
        (ok, errors): ok が True なら緑、False なら赤。errors はエラー理由の一覧。
    """
    # ログが空のときは赤（「見ていない」を「問題なし」にしない）
    if not entries:
        return False, ["ブラウザログが 0 件です（ログが収集できていないか、接続に失敗しています）"]

    valid_ignore, ignore_patterns, ignore_err = load_ignore_patterns(ignore_file)
    if not valid_ignore:
        return False, [ignore_err]

    errors: list[str] = []
    error_levels = {"error", "exception"}

    for entry in entries:
        level = str(entry.get("level") or entry.get("type") or "").strip().lower()
        text = str(entry.get("text") or entry.get("message") or "").strip()

        # 警告や情報ログは落とさない。error と exception だけを見る。
        is_error = False
        for err_lvl in error_levels:
            if err_lvl in level:
                is_error = True
                break

        if not is_error:
            continue

        # 除外パターンと照合
        ignored = False
        for item in ignore_patterns:
            pattern = item["pattern"]
            if pattern in text or (pattern and re.search(pattern, text)):
                ignored = True
                break

        if not ignored:
            errors.append(f"[{level}] {text}")

    if errors:
        return False, errors

    return True, []


def main() -> int:
    parser = argparse.ArgumentParser(description="WebGL ブラウザログの判定")
    parser.add_argument("log_file", nargs="?", help="ログ JSON ファイルパス（省略時は標準入力）")
    parser.add_argument("--ignore-file", help="除外設定 JSON ファイルパス")
    args = parser.parse_args()

    if args.log_file:
        path = Path(args.log_file)
        if not path.is_file():
            print(f"ログファイルが見つかりません: {path}", file=sys.stderr)
            return 1
        with io.open(path, "r", encoding="utf-8") as f:
            entries = json.load(f)
    else:
        content = sys.stdin.read()
        if not content.strip():
            entries = []
        else:
            entries = json.loads(content)

    ignore_path = Path(args.ignore_file) if args.ignore_file else None
    ok, errs = judge_browser_logs(entries, ignore_file=ignore_path)

    if ok:
        print("PASS: ブラウザログ判定 緑 (error/exception 0 件)")
        return 0
    else:
        print("FAIL: ブラウザログ判定 赤", file=sys.stderr)
        for err in errs:
            print(f"  - {err}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
