---
trigger: always_on
---

# 開発ルール（宣言的制約のみ）

このファイルが唯一のルールである。手続き（If-Then の対処法）は書かない。
違反は `.claude/hooks/guard.js` と CI が物理的にブロックする。文章で守らせるのではない。

## 役割

- **人間**: Unity エディタ操作、シーン/プレハブ構築、Inspector へのアサイン、動作の目視確認。
- **Claude（Architect）**: アーキテクチャ判断、指示書の発行、レビュー。実装はしない。
- **Gemini**: 指示書 1 枚 = 1 機能の実装のみ。指示書にないファイルは触らない。
  エラー修正・コンパイル修正を含むすべての実装は Gemini が担う。Claude に実装を委譲しない。

### Architect のモデル

Architect のモデルは、下の移行条件がすべて真になるまで **Claude Opus**、
真になった後は **Claude Sonnet** とする。

移行後も、次の 2 つだけは Opus で行う。

- アーキテクチャ判断（新しい層の追加、依存方向の変更、規約そのものの改訂）
- 停止条件に抵触した実装の裁定

移行条件（すべて真であること）:

1. 周回ループが人間の目視で通っている。クリア → ショップ → 次周回で初期ステータスが上がる
2. PlayMode スモークが 1 周回を通しで緑、実行中の Exception が 0
3. WebGL ビルドが成功し、ブラウザで 24 ターン操作できる
4. 直近 3 サイクル連続でレビューが即時承認（差し戻し 0）

移行条件の判定は人間が行い、`docs/STATUS.md` に移行日を 1 行で記録する。
**記録が無い間の Architect は Opus である。**

## AIの行動前提

- **コード単体での進捗錯覚禁止**: C# やテストの通過だけで動いたと見なさない。シーンとInspectorが結びついて初めて機能する。
- **外部一次情報の最重視**: 推測や捏造でコードを書かない。エラーや外部ツール仕様は必ず公式ドキュメントや実測値を調査して根拠を持つ。
- **対症療法の禁止**: 例外が出た箇所に小手先のパッチ（nullガード等）を当てない。設計や初期化順序の上流から根本原因を特定する。

## アーキテクチャ

- ランタイムアセンブリは `Game.asmdef` **1 つ**。機能ごとの asmdef 分割は禁止。
  例外は次の 3 枚のみ（Editor 1 枚 + テスト 2 枚）。これ以外の asmdef 新設は禁止。
  - `Game.Editor.asmdef` — Editor 専用（`includePlatforms: ["Editor"]`）
  - `Game.Tests.EditMode.asmdef` — EditMode テスト（`includePlatforms: ["Editor"]`）
  - `Game.Tests.PlayMode.asmdef` — PlayMode テスト（`includePlatforms: []`）
  テスト用 asmdef は EditMode / PlayMode で必ず別フォルダに置く。
- DI コンテナ（Zenject / VContainer）、Addressables、新規イベントチャネル SO の追加は禁止。
- 抽象（interface / 基底クラス）は、実装が 2 つ以上存在してから作る。

## Unity 固有の禁止事項

- `.unity` / `.prefab` / `.asset` / `.meta` / `.asmdef` をテキスト編集しない。
  これらの変更は必ず Unity のシリアライズ機構を通す。経路は次の 2 つだけ。
  - 人間が Unity エディタで操作する
  - `Game/Assets/Editor/` 配下の Editor スクリプトが Editor API で行い、
    それを Unity から走らせる（メニュー実行、または `-executeMethod` のバッチモード）
  どちらの経路を通っても、シーンが変わったら人間が画面を見るまで完了ではない。
- ランタイムコードでのエディタ専用 API（`UnityEditor` 名前空間、`AssetDatabase`、
  およびそれらを囲む条件付きコンパイル）は禁止。
  WebGL ビルドでコードごと消滅し、参照が null になる。
- ランタイムコードでのシーン内検索（型名検索・名前検索・子オブジェクトのパス検索）は禁止。
  毎フレーム実行される更新処理の中では特に禁止。
- 参照の解決手段は 2 つだけ: `[SerializeField]` + 人間の Inspector アサイン、または `Resources.Load`。

## テスト

- 結合の正しさは PlayMode テスト（`[UnityTest]`）でのみ証明する。
- PlayMode テストは `Game/Assets/Tests/PlayMode/` 配下にのみ置く。EditMode テストは `Game/Assets/Tests/` 直下に置く。
- EditMode テストは「入力と出力が純粋な計算」に限る。MonoBehaviour / シーン / SO の
  モックを組み立てるテストは書かない。
  例外は次の 3 枚のみ。`GameFlowController` を器として `AddComponent` するが、
  検証しているのは状態遷移の純粋計算であるため許可する。これ以外の EditMode テストで
  `new GameObject` / `AddComponent` / private フィールドへの reflection を使うことは禁止。
  - `Game/Assets/Tests/GameFlowControllerTests.cs`
  - `Game/Assets/Tests/GameFlowControllerRelicTests.cs`
  - `Game/Assets/Tests/GameMonteCarloSimulationTests.cs`
- View（`*View.cs`）の検証は PlayMode テストで行う。EditMode で View を
  `AddComponent` して組み立てるテストは書かない。
- テストを通すためにプロダクションコードへ分岐や自己修復を足すことは禁止。
  テストが通らない場合は実装をやめて人間に報告する。

## 完了の定義（DoD）

タスクは以下がすべて真のときのみ「完了」と報告してよい。

1. Unity のコンパイルエラーが 0
2. PlayMode テストが緑、実行中の Exception が 0
3. WebGL ビルドが成功し、ブラウザで操作できる
4. 人間が画面を見て確認した

進捗を自然言語で宣言しない。CI の結果とデプロイされた URL だけが進捗である。

## 停止条件

以下に該当したら即座に手を止め、人間に報告する。

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- 同じエラーの修正を 2 回試みて直らない
- 指示書に書かれていない設計判断が必要になった

## 文体・トーン

- 過剰な煽り、大げさな感嘆符（！）、過剰なヨイショや芝居がかった表現は禁止。
- 平素で落ち着きつつ、うっすら明るい自然なトーンで話す。
- 事実と結論を簡潔に伝え、不要な大風呂敷や前置き・結びの過剰な修飾を排す。

## ステータス確認時の応答規範

- 「動いているか」「進んでいるか」等、タスクやプロセスの実行状態を問われた場合、ファイル編集や余計な作業・推論・解説を一切挟まず、現在プロセスのステータス（RUNNING / STOPPED / SUCCESS / FAILED）のみを直ちに返すこと。
