#!/usr/bin/env python3
"""WebGL スモークテスト実行スクリプト。

1. WebGL ビルド成果物を静的サーバーで配信
2. ヘッドレスブラウザ（Playwright Chromium）で開く
3. 読み込み完了まで待機（タイムアウト上限あり）
4. 一定時間動かしてコンソールログを収集
5. scripts/judge_browser_logs.py でログを判定（error/exception 0 件）
6. 画面スクリーンショットを撮影し、真っ黒でないことを検証
"""

from __future__ import annotations

import argparse
import io
import json
import os
import socket
import sys
import threading
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

# scripts/judge_browser_logs.py をインポート
SCRIPTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS_DIR))
from judge_browser_logs import judge_browser_logs


def detect_content_encoding(path: Path) -> str | None:
    """ファイルの先頭バイト列から圧縮方式を判定する。

    戻り値:
        "gzip": gzip 圧縮されている
        "br": Brotli 圧縮されている
        None: 圧縮されていない（平文）
    例外:
        ValueError / FileNotFoundError: 0バイトまたは読めない場合
    """
    p = Path(path)
    if not p.is_file():
        raise FileNotFoundError(f"File not found: {p}")
    if p.stat().st_size == 0:
        raise ValueError(f"File is empty (0 bytes): {p}")

    with open(p, "rb") as f:
        head = f.read(512)
    if len(head) == 0:
        raise ValueError(f"Could not read content from file: {p}")

    # gzip マジックバイト: 1f 8b
    if head.startswith(b"\x1f\x8b"):
        return "gzip"

    # 平文（無圧縮）の判定:
    # ヌルバイトが含まれる場合はバイナリ（圧縮）と判定
    if b"\x00" in head:
        return "br"

    try:
        text = head.decode("utf-8")
        control_chars = {i for i in range(32)} - {9, 10, 13}  # \t, \n, \r 以外
        if any(ord(c) in control_chars for c in text):
            return "br"
        return None
    except UnicodeDecodeError:
        return "br"


class WebGLHTTPRequestHandler(SimpleHTTPRequestHandler):
    """Unity WebGL 用のヘッダーを付与する静的サーバーハンドラー。"""

    def end_headers(self) -> None:
        # Unity WebGL 圧縮ファイルへの対応
        path_lower = self.path.lower().split("?")[0]
        if path_lower.endswith(".unityweb"):
            if "wasm" in path_lower:
                self.send_header("Content-Type", "application/wasm")
            elif "data" in path_lower:
                self.send_header("Content-Type", "application/octet-stream")
            elif "js" in path_lower:
                self.send_header("Content-Type", "application/javascript")

            local_path = Path(self.translate_path(self.path))
            if local_path.is_file():
                encoding = detect_content_encoding(local_path)
                if encoding:
                    self.send_header("Content-Encoding", encoding)

        # 共通ヘッダー
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        super().end_headers()

    def log_message(self, format: str, *args: object) -> None:
        # テストログを埋め尽くさないよう通常アクセスログは抑制
        pass


def start_server(serve_dir: Path, port: int = 8080) -> ThreadingHTTPServer:
    """指定ディレクトリを配信する静的サーバーを起動する。"""
    handler = lambda *args, **kwargs: WebGLHTTPRequestHandler(*args, directory=str(serve_dir), **kwargs)
    server = ThreadingHTTPServer(("127.0.0.1", port), handler)
    server_thread = threading.Thread(target=server.serve_forever, daemon=True)
    server_thread.start()
    return server


def check_screenshot_not_black(screenshot_path: Path) -> tuple[bool, str]:
    """スクリーンショットが真っ黒（または透明）でないことを検証する。"""
    try:
        from PIL import Image

        img = Image.open(screenshot_path).convert("RGBA")
        width, height = img.size
        pixels = list(img.getdata())
        total_pixels = len(pixels)

        # 非ゼロ（R, G, B が一定以上の輝度を持つ、または完全黒以外の）ピクセルを数える
        colored_pixels = 0
        for r, g, b, a in pixels:
            if a > 0 and (r > 15 or g > 15 or b > 15):
                colored_pixels += 1

        ratio = colored_pixels / total_pixels if total_pixels > 0 else 0
        if colored_pixels == 0:
            return False, f"画面が真っ黒です（有色ピクセル数: 0 / {total_pixels}）"

        return True, f"画面描画確認 OK (有色ピクセル率: {ratio:.2%})"
    except ImportError:
        # Pillow が無い場合の代替フォールバック: ファイルサイズとバイト分布で確認
        size = os.path.getsize(screenshot_path)
        if size < 1000:
            return False, f"スクリーンショットのサイズが小さすぎます（{size} bytes）"
        return True, f"Pillow 未インストールのためサイズ検証のみ通過 ({size} bytes)"
    except Exception as e:
        return False, f"スクリーンショット画像検証エラー: {e}"


def run_smoke_test(
    build_dir: Path,
    port: int = 8080,
    timeout_sec: int = 180,
    play_duration_sec: int = 5,
    screenshot_path: Path = Path("smoke_screenshot.png"),
    logs_path: Path = Path("browser_logs.json"),
) -> int:
    """ヘッドレスブラウザで WebGL スモークテストを実行する。"""
    # 適切な配信ディレクトリを解決（build/WebGL/WebGL または build/WebGL または 指定パス）
    if (build_dir / "index.html").is_file():
        actual_serve_dir = build_dir
    elif (build_dir / "WebGL" / "index.html").is_file():
        actual_serve_dir = build_dir / "WebGL"
    else:
        print(f"エラー: index.html が見つかりません: {build_dir}", file=sys.stderr)
        return 1

    print(f"静的サーバー配信元: {actual_serve_dir}")
    server = start_server(actual_serve_dir, port=port)
    url = f"http://127.0.0.1:{port}/index.html"
    print(f"テスト対象 URL: {url}")

    try:
        from playwright.sync_api import sync_playwright, TimeoutError as PlaywrightTimeoutError
    except ImportError:
        print("エラー: playwright がインストールされていません。", file=sys.stderr)
        print("pip install playwright && playwright install --with-deps chromium を実行してください。", file=sys.stderr)
        return 1

    browser_logs: list[dict] = []

    print("Playwright ヘッドレスブラウザ（Chromium）を起動中...")
    with sync_playwright() as p:
        # ヘッドレスで WebGL を動作させる引数を付与
        browser = p.chromium.launch(
            headless=True,
            args=[
                "--enable-webgl",
                "--use-gl=angle",
                "--use-angle=swiftshader",
                "--ignore-gpu-blocklist",
                "--no-sandbox",
            ],
        )
        context = browser.new_context(
            viewport={"width": 1280, "height": 720},
        )
        page = context.new_page()

        # ログ監視を設定
        def on_console(msg):
            entry = {"level": msg.type, "text": msg.text}
            browser_logs.append(entry)
            print(f"[Browser Console {msg.type.upper()}] {msg.text}")

        def on_page_error(exc):
            entry = {"level": "error", "text": str(exc)}
            browser_logs.append(entry)
            print(f"[Browser PageError] {exc}", file=sys.stderr)

        page.on("console", on_console)
        page.on("pageerror", on_page_error)

        print(f"ページ読み込み開始（タイムアウト上限: {timeout_sec} 秒）...")
        start_time = time.time()

        try:
            page.goto(url, wait_until="domcontentloaded", timeout=timeout_sec * 1000)

            # Unity WebGL の読み込み完了を待機
            # 既定の Unity テンプレートでは #unity-loading-bar が display: none になる
            loading_bar = page.locator("#unity-loading-bar")
            if loading_bar.count() > 0:
                print("Unity ロードバーの非表示（読み込み完了）を待機中...")
                loading_bar.wait_for(state="hidden", timeout=timeout_sec * 1000)
            else:
                # ロードバーが無いテンプレートの場合、canvas の表示を待機
                print("#unity-canvas の描画可能状態を待機中...")
                page.wait_for_selector("#unity-canvas", timeout=timeout_sec * 1000)

            elapsed = time.time() - start_time
            print(f"Unity WebGL 読み込み完了（所要: {elapsed:.1f} 秒）")

            # 読み込み後、一定時間動かしてエラーや例外が発生しないか見る
            print(f"動作確認中（{play_duration_sec} 秒間待機）...")
            page.wait_for_timeout(play_duration_sec * 1000)

            # 画面スクリーンショットの撮影
            print(f"スクリーンショット撮影: {screenshot_path}")
            canvas = page.locator("#unity-canvas")
            if canvas.count() > 0:
                canvas.screenshot(path=str(screenshot_path))
            else:
                page.screenshot(path=str(screenshot_path))

        except PlaywrightTimeoutError:
            elapsed = time.time() - start_time
            # やってはいけないこと: 待ち時間を超えたときに緑と見なす
            print(f"エラー: 読み込み待ち時間上限（{timeout_sec} 秒）を超過しました（経過: {elapsed:.1f} 秒）", file=sys.stderr)
            # ログを保存してから終了
            with io.open(logs_path, "w", encoding="utf-8") as f:
                json.dump(browser_logs, f, ensure_ascii=False, indent=2)
            browser.close()
            server.shutdown()
            return 1
        except Exception as e:
            print(f"ブラウザ実行時エラー: {e}", file=sys.stderr)
            with io.open(logs_path, "w", encoding="utf-8") as f:
                json.dump(browser_logs, f, ensure_ascii=False, indent=2)
            browser.close()
            server.shutdown()
            return 1

        browser.close()

    server.shutdown()

    # ログを JSON ファイルへ保存
    with io.open(logs_path, "w", encoding="utf-8") as f:
        json.dump(browser_logs, f, ensure_ascii=False, indent=2)
    print(f"ブラウザログ保存: {logs_path} ({len(browser_logs)} 件)")

    # 1. 画面描画の検証（真っ黒でないこと）
    is_rendered, render_msg = check_screenshot_not_black(screenshot_path)
    print(f"画面描画検証: {render_msg}")
    if not is_rendered:
        print(f"FAIL: {render_msg}", file=sys.stderr)
        return 1

    # 2. ログの判定（judge_browser_logs を使用）
    ok, errors = judge_browser_logs(browser_logs)
    if not ok:
        print("FAIL: ブラウザログ判定でエラーが検出されました:", file=sys.stderr)
        for err in errors:
            print(f"  - {err}", file=sys.stderr)
        return 1

    print("PASS: WebGL スモークテスト成功（画面描画あり・error/exception 0 件）")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="WebGL スモークテストランナー")
    parser.add_argument("--build-dir", default="build/WebGL", help="WebGL ビルドディレクトリ")
    parser.add_argument("--port", type=int, default=8080, help="静的サーバーポート")
    parser.add_argument("--timeout", type=int, default=180, help="読み込み完了待ち時間上限（秒）")
    parser.add_argument("--play-duration", type=int, default=5, help="読み込み後の動作確認時間（秒）")
    parser.add_argument("--screenshot-path", default="smoke_screenshot.png", help="スクリーンショット保存パス")
    parser.add_argument("--logs-path", default="browser_logs.json", help="ログ保存パス")
    args = parser.parse_args()

    return run_smoke_test(
        build_dir=Path(args.build_dir),
        port=args.port,
        timeout_sec=args.timeout,
        play_duration_sec=args.play_duration,
        screenshot_path=Path(args.screenshot_path),
        logs_path=Path(args.logs_path),
    )


if __name__ == "__main__":
    sys.exit(main())
