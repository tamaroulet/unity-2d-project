# ルール移行対応表

中枢 10 節について、harness 側の対応ファイルと検証用の語を記す。

| 節 | harness 側のファイル | その節を確かめる語 |
| --- | --- | --- |
| 役割 | C:/dev/harness/config/execution-paths.json | claude-code |
| AIの行動前提 | C:/dev/harness/docs/decision-axes.md | AI の自己申告を判定に含めない |
| 停止条件 | C:/dev/harness/docs/decision-axes.md | 手を止めて報告する |
| 運用 | C:/dev/harness/docs/decision-axes.md | 人間の連続した創作時間を最大化するのが目的である |
| セッション枠 | C:/dev/harness/docs/decision-axes.md | 席は量で分ける |
| 指示書とレビューの分量 | C:/dev/CLAUDE.md | その単位に固有のことだけを書く |
| 知識の蓄積 | C:/dev/harness/docs/decision-axes.md | 文書は寿命で分け、量を増やさない |
| 人間への報告 | C:/dev/harness/docs/report-format.md | 報告は次の 4 節だけで構成します |
| 文体・トーン | C:/dev/harness/docs/report-format.md | 過剰な煽り、大げさな感嘆符、芝居がかった表現を使いません |
| ステータス確認時の応答規範 | C:/dev/harness/docs/report-format.md | RUNNING |
