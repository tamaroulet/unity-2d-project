# 第7週 指示書：総合リバランス・UI/UXポリッシュ・Webビルド検証・リリース

## 1. 目的と責務
全システム（育成コマンド・イベント・エンディング・レリック3択・Act 1〜4ボスバトル・周回メタ永続化）が統合された状態において、アセット実体の完全生成、シーンバインド、全自動テスト（127件・1,000回シミュレーション）、WebGL ビルド自動化、GitHub Pages 公開準備、および成果資料の最終同期を完了する。

---

## 2. 実施タスク一覧（Checklist）

### 2.1 マルチActボスデータ生成 & シーン完全バインド
- [x] `Tools/Generate Boss Assets` を実行し、Act 1〜4（`Boss_Act1_01`, `Boss_Act2_01`, `Boss_Act3_01`, `Boss_Act4_01`）のアセット実体および `BossCatalog.asset` を完全生成する。
- [x] `Tools/Bind Boss to MainGame Scene` を実行し、`MainGame.unity` シーンの `GameFlowController` に `_bossBattleTurns = {6, 12, 18, 24}`, `_bossCatalog`, `_autoBattleResolver` をバインドする。
- [x] `Tools/Bind Meta Progression to MainGame Scene` を実行し、メタプログレッション関連アセットおよび UI の完全接続を確認する。

### 2.2 全自動単体テスト・シミュレーション検証
- [x] Unity-MCP `refresh_unity` を実行し、コンパイルエラーゼロを確認する。
- [x] EditMode 単体テスト全件（127件）を実行し、**127 / 127 passed（100% Green）** を達成する。
- [x] `GameMonteCarloSimulationTests`（4連戦ボス戦＋レリック＋周回メタを含む 1,000 周回・24,000 ターン以上のシミュレーション）が例外ゼロで安定完走することを確認する。

### 2.3 WebGL ビルド自動化と実ビルド検証
- [ ] `WebGlBuildScript.cs`（`Tools/Build WebGL`）を実行し、`Builds/WebGL` に WebGL ビルド成果物を生成する。
- [ ] 出力ファイル（`index.html`, `Build/*.wasm`, `Build/*.data`, `Build/*.framework.js`）が正常に生成されたことを確認する。

### 2.4 GitHub Pages 公開整備
- [ ] WebGL ビルド成果物を GitHub Pages 公開用ディレクトリ（`docs/webgl/` 等）またはデプロイ用ブランチに配置する。
- [ ] ブラウザ上で実際に動作することを確認する。

### 2.5 仕様書・成果物ドキュメントの最終同期
- [ ] `docs/STATUS.md` を更新し、第7週全工程完了・テスト 127 件通過・クォータ実績を記録する。
- [ ] `docs/log.md` に第7週の実施記録、評価指標（書き込み範囲逸脱 0、差し戻し 0 等）を記録する。
- [ ] `README.md` に完成版の機能概要、WebGL プレイ情報、最新の 7 層アセンブリアーキテクチャ図を反映する。
- [ ] 全成果物を Git コミット＆プッシュする。
