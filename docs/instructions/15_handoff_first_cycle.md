# 指示書 15: 受け渡し規約で 1 周だけ回す

```
ROLE: Context Gatherer（今回は実装しない）
BRANCH: main
```

## 目的

`docs/workflow/HANDOFF_PROTOCOL.md` の様式が実際に回るかを、**1 周だけ手で確認する**。
仕組みやスクリプトは作らない。**回ることを確認してから作る。**

理由: 今日、夜間の自律実行が 3 日間一度も起動していなかったのに誰も気づかなかった。
先に仕組みを作ると「動いてるつもり」が生まれる。

## やること

- [x] `docs/workflow/HANDOFF_PROTOCOL.md` を読む
- [x] `docs/handoff/<今日の日付>-01/` を作り、`01-request.md` に人間から受けた要件を**逐語で**書く
- [x] `02-context.md` を規約 §4 のテンプレートどおりに作る。実測データは**必ず自分でコマンドを実行して生出力を貼る**（過去のログの引用や記憶で書かない）
- [x] `02-context.md` が完成したら**そこで止める**。実装には着手しない。人間に「Claude へ渡してよいか」を確認する

## 実測データの取り方

```bash
git status --porcelain
git log --oneline -5
dotnet build Game/Game.sln -v q --nologo     # 2〜3 秒
```

テストは Unity が開いていれば Unity-MCP の `run_tests`（数秒）。
閉じていれば batchmode（1 回 2 分）。**どちらを使ったか明記する。**

## 守ること

- 人間の原文を**要約・言い換えしない**。逐語で貼る
- コマンド出力を**要約しない**。生のまま貼る
- Claude への問いは**選択肢にする**。「どうすればいいですか」ではなく「A か B か」
- 自分の見立ては書いてよいが、**それで実装を始めない**

## 触ってはいけないもの

```
.agents/rules/**  .claude/**  .github/workflows/**
scripts/nightly_gate.py  scripts/auto_runner.py
scripts/morning_report.py  scripts/nightly_baseline.json
Game/Packages/manifest.json  .mcp.json
```

`.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集も禁止。

## 完了報告

```
指示書: docs/instructions/15_handoff_first_cycle.md
作成: docs/handoff/<日付>-01/01-request.md, 02-context.md
テストの取得方法: Unity-MCP / batchmode のどちら
停止: 02-context.md 完成時点で停止。実装未着手
```

`02-context.md` の全文を人間に見せること。コミットのみ行い、push はしない。
