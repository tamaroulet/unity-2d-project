# Deep Research 調査指示書 04：2Dターン制ゲームにおけるUI・フェーズ遷移のAI自律設計

## 1. 調査対象のプロジェクト（完全な具体情報）

* **エンジン**: Unity 6 (6000.3.23f1 LTS), WebGL ターゲット
* **ゲーム仕様**:
  - 24ターンのローグライク・パラメータ育成ゲーム（Stamina, Skill, Mental）
  - 3つのコマンド（Study, Train, Rest）
  - 6, 12, 18, 24ターン目にボス戦（オートバトル計算）
  - ボス戦勝利後にレリック（遺物）3択ドラフト画面
  - 24ターン終了時にエンディング分岐
* **アセンブリ構成**: `Game.asmdef`, `Game.Editor.asmdef`, `Game.Tests.EditMode.asmdef`, `Game.Tests.PlayMode.asmdef`
* **現状の課題（今直面している壁）**:
  - さきほど実機テストで `Current phase: ShowingRelicDraft`（レリック選択フェーズ）に入ったが、ダイアログ表示と解除の連携が未完成でゲームが止まった。
  - AI（Gemini / Claude）に UI 画面を作らせると、`transform.Find` や `FindFirstObjectByType` を書いてシーンを壊す。
  - かといって人間が全ダイアログの全UIパーツ（テキスト、ボタン、パネル）を手作業でインスペクタからドラッグ＆ドロップして配線するのは工数が大きすぎる。

---

## 2. 調査・回答を求める具体的課題（Scope を限定）

### 課題A：ゲームフェーズ（GamePhase）とダイアログUIの完全疎結合パターン
* `GameFlowController` が `ShowingRelicDraft` や `ShowingBossBattle` に遷移した際、**UI 側（ダイアログ）とどう通信して画面を開き、プレイヤー（またはテスト）が選択を完了したことをどうコントローラーへ安全に通知してターンを進めるか？**
* **条件**:
  - `transform.Find` / `GameObject.Find` は完全禁止（物理フックで弾かれる）。
  - `FindFirstObjectByType` も禁止。
  - `#if UNITY_EDITOR` による `AssetDatabase` 解決も禁止。
  - 人間がインスペクタでアサインするスロット数は最小限（1〜2個）に抑えること。
* **知りたいこと**:
  - ScriptableObject Event Channel（例: `ShowRelicDraftChannelSO`, `RelicSelectedChannelSO`）を用いた、このゲームのための**完全な C# 実装コード**（コントローラー側とビュー側の両方）。

### 課題B：PlayMode テストで「ダイアログ選択」を安全に自動検証する手法
* さきほど開通した `SmokeTest.cs`（STUDYボタンを押して1ターン進むテスト）の型を踏まえ、
  **「ボス戦ダイアログが表示され、OKを押し、レリックドラフトで1枚選んで通常ターンへ復帰する」一連のモーダルUI操作を、PlayMode テスト（`[UnityTest]`）でどう記述するか？**
* UI のアニメーション待ちやドメインリロードを跨いでも絶対にフレーキー（不安定）にならない、先人のテスト記述パターン。

---

## 3. 要求成果物（コードのみ・解説は最小限）

1. **`GameFlowController` ⇔ `RelicDraftDialogView` 間の完全な疎結合 C# 実装コード**
   - 状態通知チャネル SO
   - 選択完了チャネル SO
   - コントローラー側の購読・解除
   - ビュー側の表示・非表示・ボタン押下イベント
2. **これを検証する PlayMode テストの C# 実装コード**
