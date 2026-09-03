# Deep Research 調査指示書 05：Unity 6 WebGL 用 GameCI と auto_runner.py の完全自動化実装

## 1. 調査対象のプロジェクト（完全な具体環境）

* **Unity バージョン**: `6000.3.23f1` (Unity 6 LTS)
* **ターゲットプラットフォーム**: WebGL
* **OS**: Windows 11
* **連携ツール**:
  - `com.coplaydev.unity-mcp` (v10.0.0): Unity Editor と通信する Model Context Protocol サーバー（ポート接続、`run_tests`, `get_test_job` 等をサポート）
  - Python: `3.12`（Windows環境）
  - Git: ローカルリポジトリ（`main` ブランチ）
* **現在のテスト構成**:
  - EditMode: 94件 Passed（`Game.Tests.EditMode.asmdef`）
  - PlayMode: 1件 Passed（`SmokeTest.cs`, `Game.Tests.PlayMode.asmdef`）
* **現状の課題（今直面している壁）**:
  - 先ほどタスクスケジューラで「深夜 1:00〜6:00 に 30分間隔で `scripts/auto_runner.py` を実行」と設定したが、**`auto_runner.py` にはまだ「PlayMode テストが落ちたら自律で git reset して元の安全な状態に戻す」防壁ロジックが組み込まれていない**。
  - GitHub Actions で Unity 6 + WebGL のビルドとテストを回す `main.yml` がまだなく、プッシュしてもクラウドでテストが走らない。

---

## 2. 調査・回答を求める具体的課題（Scope を限定）

### 課題A：Unity 6 LTS (6000.3.23f1) + WebGL 用の完全な GameCI GitHub Actions YAML
* **条件**:
  - Unity 6 の公式 Docker イメージまたは GameCI v4+ を使用。
  - **Job 1**: `EditMode` および `PlayMode` の全テスト実行（1件でも落ちたら失敗）。
  - **Job 2**: WebGL ビルドの作成。
  - **Job 3**: GitHub Pages への自動デプロイ（任意または成果物アップロード）。
  - Unity Personal ライセンス（無料版）でのアクティベーション設定（Unity Activation GitHub Action）を含む。
* **求めるもの**:
  - そのまま `.github/workflows/gameci.yml` にコピペして動く**完全な YAML コード**。

### 課題B：`scripts/auto_runner.py` 用の「自律ロールバック・テスト検査」ループの Python 実装
* **条件**:
  - Python 3.12 から、ローカルの Unity MCP サーバー（または Unity CLI）を呼び出す。
  - または、ローカルの Unity エディタが起動中の場合、MCP を叩いて `run_tests(mode="PlayMode")` を実行し完了を待つ。
  - もしテストが `Passed` なら → `git commit` を維持。
  - もしテストが `Failed`、またはコンパイルエラーが出たなら → **即座に `git reset --hard HEAD` を実行してコードを巻き戻す**。
  - 実行結果（成功/ロールバック）を `docs/log.md` または専用ログに 1 行追記する。
* **求めるもの**:
  - この一連の防壁ループを実行する、**コピペで動く完全な Python スクリプトコード**。

---

## 3. 要求成果物（コードのみ・解説は最小限）

1. **`.github/workflows/gameci.yml` の全文**
2. **`scripts/auto_runner.py` に組み込む自己修復・ロールバック防壁関数の全文（Python）**
