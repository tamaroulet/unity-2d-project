#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = [
#     "mcp",
# ]
# ///
# SPDX-AI-Disclosure: ai-generated
"""
Runs EditMode and PlayMode tests via resident Unity (unityMCP)
and outputs results to logs/editmode_results.xml and logs/playmode_results.xml.
"""

import argparse
import asyncio
import json
import os
import re
import shutil
import sys
import time
from pathlib import Path
from mcp.client.session import ClientSession
from mcp.client.streamable_http import streamable_http_client


def get_unity_test_results_path() -> Path:
    user_profile = os.environ.get("USERPROFILE")
    if user_profile:
        candidate = Path(user_profile) / "AppData" / "LocalLow" / "DefaultCompany" / "Game" / "TestResults.xml"
        return candidate
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        candidate = Path(local_app_data).parent / "LocalLow" / "DefaultCompany" / "Game" / "TestResults.xml"
        return candidate
    raise RuntimeError("Cannot determine AppData LocalLow path for Unity TestResults.xml.")


async def run_single_test_mode(session: ClientSession, mode: str, output_xml: Path, unity_xml: Path) -> dict:
    if unity_xml.exists():
        try:
            unity_xml.unlink()
        except OSError:
            pass

    print(f"[{mode}] Starting test run via unityMCP...")
    t0 = time.perf_counter()

    call_args = {"mode": mode}
    if mode == "PlayMode":
        call_args["init_timeout"] = 120000

    call_res = await session.call_tool("run_tests", call_args)
    if not call_res.content:
        raise RuntimeError(f"[{mode}] run_tests returned empty content.")

    payload = json.loads(call_res.content[0].text)
    if not payload.get("success"):
        raise RuntimeError(f"[{mode}] run_tests failed: {payload.get('error') or payload.get('message')}")

    job_id = payload["data"]["job_id"]
    print(f"[{mode}] Test job started with ID: {job_id}")

    last_status = None
    timeout_sec = 300
    poll_start = time.perf_counter()

    while True:
        if time.perf_counter() - poll_start > timeout_sec:
            raise TimeoutError(f"[{mode}] Timed out after {timeout_sec}s waiting for test job {job_id}")

        job_res = await session.call_tool("get_test_job", {"job_id": job_id, "wait_timeout": 10})
        job_payload = json.loads(job_res.content[0].text)
        data = job_payload.get("data", {})
        status = data.get("status")

        if status != last_status:
            print(f"[{mode}] Job status: {status}")
            last_status = status

        if status in ("succeeded", "failed"):
            break

        await asyncio.sleep(0.5)

    elapsed = time.perf_counter() - t0

    for _ in range(25):
        if unity_xml.exists() and unity_xml.stat().st_size > 0:
            break
        await asyncio.sleep(0.2)

    if not unity_xml.exists():
        raise FileNotFoundError(f"[{mode}] Expected Unity test results file not found at: {unity_xml}")

    output_xml.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(unity_xml, output_xml)
    print(f"[{mode}] Copied results to {output_xml} ({output_xml.stat().st_size} bytes)")

    summary = {}
    content = output_xml.read_text(encoding="utf-8")
    m = re.search(r'<test-run\b([^>]+)>', content)
    if m:
        attrs = m.group(1)
        for key in ("total", "passed", "failed", "skipped", "duration"):
            km = re.search(rf'\b{key}="([^"]+)"', attrs)
            if km:
                summary[key] = km.group(1)

    summary["elapsed_seconds"] = f"{elapsed:.2f}"
    return summary


async def main_async(mcp_url: str, modes: list[str], repo_root: Path):
    unity_xml = get_unity_test_results_path()
    logs_dir = repo_root / "logs"

    print(f"Connecting to MCP at {mcp_url} ...")
    async with streamable_http_client(mcp_url) as streams:
        read, write = streams[0], streams[1]
        async with ClientSession(read, write) as session:
            await session.initialize()
            print("MCP session initialized successfully.")

            results = {}
            for mode in modes:
                out_xml = logs_dir / f"{mode.lower()}_results.xml"
                res = await run_single_test_mode(session, mode, out_xml, unity_xml)
                results[mode] = res

    print("\n" + "=" * 60)
    print("=== Resident Unity Test Run Completed ===")
    print("=" * 60)
    for mode, res in results.items():
        print(
            f"[{mode}] Elapsed: {res.get('elapsed_seconds')}s | "
            f"Total: {res.get('total')}, Passed: {res.get('passed')}, "
            f"Failed: {res.get('failed')}, Skipped: {res.get('skipped')}"
        )
    print("=" * 60)


def main():
    parser = argparse.ArgumentParser(description="Run tests via resident Unity MCP bridge.")
    parser.add_argument(
        "--mode",
        choices=["EditMode", "PlayMode", "Both"],
        default="Both",
        help="Test mode to run (default: Both)",
    )
    parser.add_argument(
        "--url",
        default="http://127.0.0.1:8080/mcp",
        help="Unity MCP HTTP endpoint URL (default: http://127.0.0.1:8080/mcp)",
    )
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent

    if args.mode == "Both":
        modes = ["EditMode", "PlayMode"]
    else:
        modes = [args.mode]

    try:
        asyncio.run(main_async(args.url, modes, repo_root))
    except Exception as exc:
        print(f"Error during resident test run: {exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
