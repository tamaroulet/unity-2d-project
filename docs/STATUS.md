# 現在地

本ファイルは「今どこにいるか」を保持する。履歴は `docs/log.md` に置く。
作業を1件終えるごとに更新する（`.agents/rules/40_docs.md`）。

最終更新: 2026-09-02

---

## 進行中の作業

| 項目 | 状態 |
|---|---|
| Step | Step 4（`EventChannelSO<T>` ＋具象型3種＋テスト） |
| 段階 | 計画レビュー完了。実装未着手 |
| 実装エージェント | Claude Code（Sonnet 5）。ターミナルで `claude` を起動 |
| 作業ディレクトリ | `.cs` の実装は `C:\dev\unity-2d-project\Game`、`docs/` の作業は `C:\dev\unity-2d-project` |

Step 4 の計画は承認済みである。差し戻し1件（テストケースを1件追加）と、
指示書の修正3件を反映済みである。次は実装の投入から再開する。

---

## 次にやること

1. Step 4 の実装を投入する
2. コンパイルと Test Runner を確認する（Unity で Ctrl + R → Run All）
3. `Assets/Core/Events/` と `Assets/Tests/` の差分をコミットする
4. Step 5 の直前に `Assets/Features/Event/Scripts/` と
   `Game.Features.Event.asmdef` を作成する（人間が Unity エディタで行う）
5. `Game.Tests.EditMode.asmdef` に `Game.Features.Event` への参照を追加する
6. Step 6 の直前に `Assets/Features/Ending/Scripts/` と
   `Game.Features.Ending.asmdef` を作成する

空フォルダのコミットは `CodingSpec` 13節で禁止されているため、
4と6は各 Step の直前まで行わない。

---

## 役割分担

| 領域 | 担当 |
|---|---|
| 計画レビュー・差し戻し素案・返す文面の生成 | Antigravity |
| git 操作 | Antigravity |
| Unity エディタ操作 | Antigravity ＋ Unity-MCP |
| 工程記録の素案生成 | Antigravity |
| `.cs` の実装 | Claude Code（ターミナル） |
| 最終判断・承認 | 人間 |

`.cs` の書き込み経路は Claude Code に一本化する。Antigravity は `.cs` を書かない。

Claude Code と Antigravity を直接やり取りさせない。人間が貼り付けを介在させる。
エージェント間で直結すると、投入内容を人間が確認できず、承認の分離が崩れる。

### 退避経路

以下は Antigravity ＋ Claude Code の範囲外とし、Claude チャットまたは Cowork に戻す。

- 一次情報の裏取りが必要な調査（公式ドキュメント、規約原文、仕様の確認）
- 規約の大幅な改訂

---

## 1サイクルの手順

| # | 実行者 | 内容 |
|---|---|---|
| 1 | 人間 | Antigravity に投入文面の生成を依頼 |
| 2 | Antigravity | 指示書と規約から投入文面を生成。実装はしない |
| 3 | 人間 | 文面を `claude` に貼る |
| 4 | Claude Code | 計画を提示して停止 |
| 5 | 人間 | 計画を Antigravity に貼り、レビューを依頼 |
| 6 | Antigravity | 判定・根拠・返す文面の3点セットを出力 |
| 7 | 人間 | 承認または修正し、返す文面を `claude` に貼る |
| 8 | Claude Code | 実装して停止 |
| 9 | 人間 | 完了報告を Antigravity に貼る |
| 10 | Antigravity | 範囲逸脱を判定し、コミット前に停止 |
| 11 | 人間 | 承認 |
| 12 | Antigravity | コミットとプッシュ、`log.md` と `STATUS.md` の更新素案を生成 |

---

## パーミッションモード

Claude Code は auto mode で動作している。Pro プランの組み込みの初期モードであり、
意図して選んだ設定ではない。

auto mode では、読み取りと作業ディレクトリ内のファイル編集が分類器も通さず
自動承認される（保護パスへの書き込みを除く）。第1週の Step 1〜3 も同条件であった。

**モードを変更しない。**変更すると第1週の実測値との比較が成立しなくなる。

git の履歴破壊のみ `Game/.claude/settings.json` の deny ルールで禁止する。
書き込み範囲に対する deny は置かない。逸脱がエージェントの挙動の測定で
なくなるためである。

出典: https://code.claude.com/docs/en/permission-modes

---

## ブロッカー

| 項目 | 影響 | 状態 |
|---|---|---|
| Unity-MCP が `Server disconnected` | Antigravity からエディタ操作ができない | 未解消。切り分け手順は `docs/research/Research-01a.md` の(E)節14項目 |

Step 4 は `Game.Core` 内で完結し、`Assets/Core/Events/` は作成済みのため、
Unity-MCP なしで進行できる。Step 5 以降のフォルダ作成で必要になる。

---

## 未確認事項

| 項目 | 内容 |
|---|---|
| Unity-MCP の接続不全の原因 | 最有力は `Antigravity 2.0` と `Antigravity IDE` の2種類のクライアント登録があり、Configure した側が誤っていること |
| ジェネリック `ScriptableObject` の IL2CPP AOT 制約 | EditMode テストは Mono 実行のため検証されない。第3週の Web ビルドまで実測できない。条件付きで安全との調査結果あり（`docs/research/Research-01.md` 項目2） |
| Antigravity が git ルートの `.agents/rules/` を読むか | ワークスペースが `C:\dev` であるため。初回投入時に確認する |
| Antigravity が `AGENTS.md` を読むか | 公式ドキュメントは `~/.gemini/GEMINI.md` と `.agents/rules` のみ記載。既存の `AGENTS.md` は `Game/` 配下にあり git ルートではない |
| Antigravity の C# 計画レビューの品質 | ドキュメント作成では範囲逸脱なし。レビュー担当としては未検証 |
| Jules の C# 実装タスクでの品質 | 同上 |
| Pro サブスクリプションでの分類器のコスト | 公式は Enterprise と API アカウントについてのみ計上と記載 |
| Decompression Fallback の起動時間の増加幅 | 絶対値のみ計測。比較対象なし |
| テストケース10 | テスト名が示す判定順序そのものを検証できていない |

---

## 期限

| 段階 | 判定日 | 内容 |
|---|---|---|
| 第1段階 | 9/9 | 通過済み（8日前倒し） |
| 第2段階 | 9/14 | イベント機能を放棄 |
| 第3段階 | 9/17 | 音声再生システム全般を放棄 |

型トラックの期限は 9/20（日）。アセットトラックは期限なし。

API クレジット $100 は 9/19 失効。Pro 枠の上限到達時に自動充当されるため、
温存せず使い切る。

---

## 参照

| 対象 | パス |
|---|---|
| 実装規約 | `docs/spec/CodingSpec.md` |
| ビルド・環境規約 | `docs/spec/BuildSpec.md` |
| Antigravity 用ルール | `.agents/rules/` 配下5ファイル |
| Claude Code 用ルール | `Game/AGENTS.md` |
| 指示書 | `docs/instructions/` 配下 |
| 調査レポート | `docs/research/` 配下 |
| 作業ログ | `docs/log.md` |
| ADR | `docs/decisions/` 配下 |
