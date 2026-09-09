#!/usr/bin/env python3
"""WebGL 静的サーバーの Content-Encoding 付与動作を検証するスクリプト。

一時ディレクトリに対照群（gzip / br / 平文 / 空）の .unityweb 見本を置き、
静的サーバーを起動して HTTP 経由で取得した際の Content-Encoding ヘッダーおよび
挙動を検査する。
"""

from __future__ import annotations

import gzip
import os
import sys
import tempfile
import urllib.error
import urllib.request
from pathlib import Path

SCRIPTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS_DIR))

from run_webgl_smoke import detect_content_encoding, start_server


def main() -> int:
    print("=== WebGL 静的サーバー Content-Encoding 検査開始 ===")

    with tempfile.TemporaryDirectory() as tmpdir:
        serve_dir = Path(tmpdir)

        # 1. 見本ファイルの作成
        # 必ず "gzip": gzip のマジックバイト (1f 8b) で始まる見本
        file_gzip = serve_dir / "sample.data.unityweb"
        file_gzip.write_bytes(gzip.compress(b"UnityWebData gzip sample payload" * 50))

        # 必ず "br": gzip でも無圧縮でもない見本
        file_br = serve_dir / "sample.wasm.unityweb"
        file_br.write_bytes(bytes([0x1b, 0x2e, 0x00, 0x00, 0x24]) + os.urandom(64))

        # 必ず None: 平文の見本（Content-Encoding ヘッダーが応答に無いこと）
        file_plain = serve_dir / "sample.js.unityweb"
        file_plain.write_text("console.log('plain text javascript payload');", encoding="utf-8")

        # 必ず 例外: 0 バイトの見本（緑にしない）
        file_empty = serve_dir / "sample.empty.unityweb"
        file_empty.write_bytes(b"")

        # 2. 関数の単体検証（対照群 4 パターン）
        print("1. detect_content_encoding 単体検証中...")
        res_gzip = detect_content_encoding(file_gzip)
        assert res_gzip == "gzip", f"gzip 見本で 'gzip' が返るべき: {res_gzip}"

        res_br = detect_content_encoding(file_br)
        assert res_br == "br", f"Brotli 見本で 'br' が返るべき: {res_br}"

        res_plain = detect_content_encoding(file_plain)
        assert res_plain is None, f"平文見本で None が返るべき: {res_plain}"

        empty_raised = False
        try:
            detect_content_encoding(file_empty)
        except Exception as e:
            empty_raised = True
            print(f"  [OK] 0 バイト見本で期待通り例外送出: {e}")
        assert empty_raised, "0 バイト見本で例外が送出されるべき（緑にしない）"
        print("  [OK] detect_content_encoding 単体検証通過")

        # 3. 静的サーバー経由での HTTP 検証
        print("2. 静的サーバー起動中 (ポート自動割当)...")
        server = start_server(serve_dir, port=0)
        port = server.server_port
        base_url = f"http://127.0.0.1:{port}"

        try:
            # (A) gzip 見本の検証
            url_gzip = f"{base_url}/sample.data.unityweb"
            with urllib.request.urlopen(url_gzip) as resp:
                encoding = resp.headers.get("Content-Encoding")
                assert encoding == "gzip", f"Content-Encoding が 'gzip' ではありません: {encoding}"
                print("  [OK] sample.data.unityweb -> Content-Encoding: gzip")

            # (B) Brotli 見本の検証 (要: 決め打ち旧実装を弾く)
            url_br = f"{base_url}/sample.wasm.unityweb"
            with urllib.request.urlopen(url_br) as resp:
                encoding = resp.headers.get("Content-Encoding")
                assert encoding == "br", f"Content-Encoding が 'br' ではありません: {encoding}"
                print("  [OK] sample.wasm.unityweb -> Content-Encoding: br")

            # (C) 平文見本の検証 (Content-Encoding ヘッダーが存在しないこと)
            url_plain = f"{base_url}/sample.js.unityweb"
            with urllib.request.urlopen(url_plain) as resp:
                encoding = resp.headers.get("Content-Encoding")
                assert encoding is None, f"平文見本に応答ヘッダー Content-Encoding が付与されています: {encoding}"
                print("  [OK] sample.js.unityweb -> Content-Encoding: なし (None)")

            # (D) 0 バイト見本の検証 (緑にしない: 正常応答 200 + エンコーディング無し等で通してはならない)
            url_empty = f"{base_url}/sample.empty.unityweb"
            empty_failed = False
            try:
                with urllib.request.urlopen(url_empty) as resp:
                    print(f"  [FAIL] 0 バイト見本に対して正常応答が返りました (status: {resp.status})", file=sys.stderr)
            except Exception:
                empty_failed = True
                print("  [OK] sample.empty.unityweb -> 期待通りエラー発生（緑にしない）")
            assert empty_failed, "0 バイト見本は正常応答になってはならない（緑にしない）"

        finally:
            server.shutdown()
            server.server_close()

    print("=== 全検査項目 PASS: WebGL 静的サーバー Content-Encoding 正常動作確認完了 ===")
    return 0


if __name__ == "__main__":
    sys.exit(main())
