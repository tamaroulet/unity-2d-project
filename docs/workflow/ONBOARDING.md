# AI 開発者向けオンボーディング・マスターガイド（引き継ぎ書）

初見の AI エージェント（Claude / Gemini / その他）は、作業前に**必ずこの文書を最初に通読**してください。
このプロジェクトには過去の失敗から得られた厳格なルールと仕組みがあります。

---

## 0. AIの認知原則（「有能な素人」であるあなたの行動指針）

AIは言われたコードを高速に生成できるが、暗黙の了解や実機の常識を持たない。
作業にあたり、以下の3原則を前提として行動すること。

1. **コードだけで進んでいると錯覚しない（Runtime Blindness の自覚）**:
   - C# のコンパイルが通り、テストが緑でも、ゲーム画面は1ミリも動いていない可能性がある。
   - Unity は「シーン」「Inspectorのアサイン」「uGUIのイベント配線」が揃って初めて成立する。コードのテキスト空間だけで自己満足して「完了」と報告しないこと。
2. **外部情報・一次情報を最重視する（脳内知識で突っ走らない）**:
   - エラーやツールの挙動に違和感があれば、自分の記憶（LLMの事前知識）だけでパッチコードを捏造しない。
   - 公式ドキュメント、エラーメッセージの指示（URL等）、先人の知見を即座に調べ、客観的な事実に基づいて判断すること。
3. **おかしかったら小手先の修正を禁じ、上流から洗い出す**:
   - 例外が出た行に `if (x != null)` を足すような「その場しのぎの対症療法」は厳禁。
   - なぜそこが null なのか、どこで参照が外れたのか、設計や初期化順序の上流に立ち返って根本原因を特定すること。

---

## 1. プロジェクトの概要と直近のゴール

- **内容**: Unity 2D のターン制ローグライク育成ゲーム（24ターン、3大パラメータ、Act 1〜4ボス、レリック3択ドラフト、周回ショップ）。
- **直近のゴール（完了形）**:
  1. **基本図形（モック）のままで、ゲーム開始〜24ターン〜ボス戦〜レリック選択〜エンディングまで通しで最後まで遊べる状態にする**。リッチな絵や演出は後回しでよい。
  2. **「AI主導ゲーム開発の体系資料」として開発フレームワークを完成させる**。

---

## 2. 厳格なアーキテクチャ規約（破ると即隔離されます）

詳細は [`.agents/rules/00_rules.md`](file:///c:/dev/unity-2d-project/.agents/rules/00_rules.md) を参照。

1. **アセンブリ定義（asmdef）**:
   - ランタイムは **`Game.asmdef` 1枚のみ**。機能ごとの細分化は厳禁。
   - 他は `Game.Editor.asmdef`, `Game.Tests.EditMode.asmdef`, `Game.Tests.PlayMode.asmdef` の3枚のみ。
2. **Unity シリアライズの不可侵**:
   - `.unity` / `.prefab` / `.asset` / `.meta` / `.asmdef` をテキスト編集しない。人間が Unity エディタで行う。
3. **ランタイムコードの禁止事項**:
   - `#if UNITY_EDITOR`、`AssetDatabase`、`UnityEditor` の混入禁止（WebGL ビルドで消滅する）。
   - `transform.Find` や `FindFirstObjectByType` などのシーン検索ハック禁止。
4. **UI とロジックの結合（重ね紙方式）**:
   - `Canvas` の下に各画面の `Panel`（`StatusPanel`, `CommandPanel`, `BossBattleDialogPanel`, `RelicDraftDialogPanel`, `MetaShopDialogPanel`）が重なって配置されている。
   - 画面の切り替えは `SetActive(true/false)` で行う。
   - UI とロジックの通信は `ScriptableObject` イベントチャンネル（`GameStateEventChannelSO` 等）を介して行う。

---

## 3. テストと品質検証

- **EditMode テスト（127件）**: 純粋計算ロジックのみ。MonoBehaviour やシーンのモックテストは禁止。
- **PlayMode テスト（1件）**: `Game/Assets/Tests/PlayMode/SmokeTest.cs`。実機シーン（`MainGame`）をロードして uGUI ボタンをクリックし、ターン進行と例外 0 件を検証する。**結合の正しさは PlayMode テストでのみ証明する**。

---

## 4. 役割分担と作業手順（Triad Protocol）

詳細は [`docs/workflow/TRIAD_PROTOCOL.md`](file:///c:/dev/unity-2d-project/docs/workflow/TRIAD_PROTOCOL.md) を参照。

- **人間（ディレクター）**: 方針決定、Unity エディタ操作、Inspector アサイン、目視確認。
- **Claude（アーキテクト）**: 大枠提示、設計判断、確定指示書の発行、レビュー。実装コードは原則書かない。
- **Gemini（実装者）**: 指示書（`docs/instructions/` または `docs/research/`）の Tasklist を 1 枚 = 1 機能で忠実に実装。指示書にないファイルは触らない。
- **文体規定**: 平素で落ち着いたトーン。煽り、大げさな感嘆符（！）、過剰なヨイショは禁止。

---

## 5. 安全ハーネスと自動化インフラ（Gate 5）

1. **夜間自律ランナー（`scripts/auto_runner.py`）**:
   - 毎日 01:00〜06:00 の夜間にのみ 30 分間隔で稼働。
   - `scripts/nightly_gate.py` がサイクル前後の HEAD を監視。
   - テスト失敗、ハックコード（`[Ignore]` や `Find` 系の混入）、保護領域の改変を検知した場合、**全成果物を `nightly-reject/<timestamp>` ブランチへ保全した上で `main` を自動ロールバック**する。
   - **注意**: 夜間ランナーを動かす際は、**必ず Unity エディタを閉じておく**こと（開いていると Library 排他ロックにより `UNVERIFIED` で全巻き戻しになる）。
2. **朝刊レポート（`scripts/morning_report.py`）**:
   - 毎朝 06:10 に自動実行され、[`docs/nightly/YYYY-MM-DD.md`](file:///c:/dev/unity-2d-project/docs/nightly/) に昨夜の「受理 / 隔離 / 未検証」が 1 画面で出力される。
3. **クラウド CI（GameCI / GitHub Actions）**:
   - `.github/workflows/unity-test.yml`: push / PR 時にクラウド上で EditMode + PlayMode テストを自動実行。

---

## 6. 次に行うべきタスク（Next Actions）

1. **モック通しプレイの開通**:
   - `GameFlowController` で現在停止している `ShowingRelicDraft`（レリック3択画面）のイベント配線を完了させ、ボス戦後〜レリック選択〜次Act〜24ターン終了〜エンディングまで、ゲームが基本図形のまま止まらずに完走できるようにする。
2. **AI開発フレームワークの体系化資料まとめ**:
   - 本プロジェクトで実証された「PlayMode CI ✕ 隔離ハーネス ✕ Triad Protocol」を、次作（2.5Dアクション等）でも即座に使い回せる汎用マニュアルとして整理する。
