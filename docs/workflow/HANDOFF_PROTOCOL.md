# 受け渡し規約（Handoff Protocol）

人間 → Antigravity(Gemini) → Claude → agy → Claude → 人間 の閉ループを
**ファイル受け渡し**で回すための様式。会話でつなぐと、下記 3 つが必ず緩む。

- 何回試したかを誰も数えない（今日、同じ推測を 3 回繰り返して消費を溶かした）
- Claude のコンテキストに生ログ全文が流れ込み、枠が節約できない
- 人間の言葉が Gemini の要約に置き換わり、Claude が別の問いに正しく答える

---

## 0. 運用手順（Gemini が毎回これを踏む）

人間は **1 行だけ**言えばよい。例:

> ターン6で進めない。handoff で回して

受けた Gemini は以下を順に実行する。**人間に手作業を戻さない。**

```powershell
# 1) サイクル開始。01-request.md と 02-context.md の実測部分が自動生成される
scripts/handoff.ps1 start -Request "（人間の原文をそのまま）"

# 2) 02-context.md の TODO を埋める
#    - テスト: Unity-MCP の run_tests（数秒）か batchmode。どちらか明記
#    - エラー: read_console の生出力。要約しない
#    - 既に潰した仮説 / 見立て / 判断してほしいこと（選択形式）

# 3) Claude に判断させる。03-instruction.md と確定指示書が発行される
scripts/handoff.ps1 ask -Dir docs/handoff/<日付>-<連番>

# 4) 発行された指示書を実行する（IDE で自分で実装しても、agy に投げてもよい）
#    結果を 04-result.md に規約 §5 の様式で書く

# 5) Claude に合否を判定させる
scripts/handoff.ps1 review -Dir docs/handoff/<日付>-<連番>
```

### Claude には本文を流し込まない

`ask` / `review` が Claude に渡すのは**ファイルを指す 1 行だけ**である。
Claude 側が自分でファイルを読む。会話履歴も生ログ全文も渡さない。
これが枠の節約の要であり、勝手に本文を貼り付けて呼ばないこと。

### Claude が呼べなかったとき

`invoke_claude_safe.ps1` は使用率 85% 以上で**物理的に遮断**する（課金超過の防止）。
遮断されたら Claude を諦め、**Gemini 側で進められる範囲だけ進めて人間に報告する**。
判断が要る部分を推測で埋めないこと。

---

## 1. 置き場所

```
docs/handoff/YYYY-MM-DD-NN/
  01-request.md      人間の原文
  02-context.md      Gemini が集めた事実          ← Claude が読む
  03-instruction.md  Claude の判断（→ docs/instructions/XX.md へ）
  04-result.md       agy の生出力                 ← Claude が読む
  05-review.md       Claude の合否と再試行回数
```

**Claude が読むのは `02` と `04` だけ。** 会話履歴も生ログ全文も通さない。

## 2. 回数の上限

| 何を | 上限 | 超えたら |
|---|---|---|
| agy が同じ失敗を直す試行 | **2 回** | Claude へ上げる |
| Claude ⇄ agy の差し戻し | **2 往復** | 人間へ上げる |

`05-review.md` の先頭に `再試行: N / 2` を必ず書く。数えていないと効かない
（`00_rules.md` の停止条件はこれを言っている）。

## 3. 禁止事項

- **人間の原文を要約・言い換えしない。** `02-context.md` に逐語で貼る
- **実測値を要約しない。** コマンド出力は生のまま貼る
- Claude に開いた質問を投げない。**選択肢にして推奨を添える**

---

## 4. `02-context.md` のテンプレート

````markdown
# 相談 YYYY-MM-DD-NN

## 人間の原文（逐語・改変禁止）
> （01-request.md をそのまま引用する）

## 実測データ

### git
```
（git status --porcelain と git log --oneline -5 の生出力）
```

### ビルド
```
（dotnet build Game/Game.sln -v q --nologo の生出力。2〜3 秒）
```

### テスト
```
（Unity-MCP の run_tests、または batchmode の結果。件数と失敗名を生で）
```

### エラー・ログ
```
（Console の該当行。要約せず、行番号ごと貼る）
```

## 既に潰した仮説
- （調べて違うと分かったこと。Claude に再調査させないため）

## 私（Gemini）の見立て
- 原因の候補: A =（1行）/ B =（1行）
- 推す案: （A か B か）。理由は（1〜2行）

## Claude に判断してほしいこと
（A か B か、等の選択形式。開いた質問にしない）
````

## 5. `04-result.md` のテンプレート

````markdown
# 実行結果 YYYY-MM-DD-NN

指示書: docs/instructions/XX.md
再試行: N / 2

## 変更
```
（git diff --stat の生出力）
```

## 検証
```
（指示書が求めた検証コマンドの生出力を、そのまま全部）
```

## 停止条件への抵触
なし / あり（内容）
````

---

## 6. なぜこの分担なのか（今日の実測）

| | Claude | Gemini / agy |
|---|---|---|
| Unity-MCP `run_tests` | **不可**（Unity を開けない） | **可**（数秒） |
| Unity を開いたまま反復 | 不可（batchmode は 1 回 2 分） | 可 |
| 利用枠 | 少ない。判断に使う | 潤沢。実働に使う |
| 保護領域の変更 | 可 | 物理的に不可（隔離される） |

**Unity が絡む検証は構造的に Gemini の担当**であり、
**設計判断・保護領域・監査は Claude の担当**である。
どちらかに寄せるのではなく、できることが違う。
