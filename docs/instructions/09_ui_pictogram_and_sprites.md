# 第8週 指示書：UI幾何学ピクトグラム・スプライト自動生成＆ビジュアル仮置き

## 1. 目的と責務
AI画像生成や外部イラスト素材を使わず、純粋な C# プロシージャル描画によって、視認性の高い幾何学単純図形スプライト（ピクトグラムアイコン、ゲージ枠、ボスエンブレム）を生成し、`MainGame.unity` の UI コンポーネントへ完全に割り当てる。

---

## 2. 実施タスク一覧（Checklist）

### 2.1 幾何学ピクトグラム・スプライトの自動生成
- [x] `ProceduralSpriteGenerator.cs` を作成し、`Assets/UI/Sprites/` に以下の単純図形スプライト（PNG / Sprite）を生成する。
  - `Icon_Stamina.png`（雷・エネルギーマーク）
  - `Icon_Skill.png`（本・ダイヤモンドマーク）
  - `Icon_Mental.png`（ハート・クロスライン）
  - `Icon_Attack.png`（剣・クロスブレード）
  - `Icon_Shield.png`（盾・ガードクレスト）
  - `Icon_Study.png`（ペン・アカデミックアイコン）
  - `Icon_Train.png`（ダンベル・パワーアイコン）
  - `Icon_Rest.png`（カップ・リフレッシュアイコン）
  - `Icon_Relic.png`（多面体クリスタル）
  - `Boss_Emblem_Act1.png` 〜 `Act4.png`（幾何学ボス紋章 256x256）
  - `Frame_Card.png`（角丸四角カード枠）
  - `Bar_Fill.png`（なめらかなゲージ塗り用スプライト）

### 2.2 UI パネルへのスプライト割り当てとシーン更新
- [x] `UILayoutBuilder.cs` を更新し、`StatusPanel`, `CommandButtonsPanel`, `BossBattleDialogPanel`, `RelicDraftDialogPanel`, `MetaShopDialogPanel` の各 `Image` に上記スプライトをアサインする。
- [x] `Tools/Setup Complete UI Layout (Simple Shapes)` を実行して `MainGame.unity` を更新・保存する。

### 2.3 単体テスト・シミュレーション検証
- [x] EditMode / PlayMode テストを batchmode で実行し、failed 0 件を確認する。
  - 実測値: EditMode **113 件収集 / 94 passed / 0 failed / 19 skipped（`[Explicit]` 凍結分）**、PlayMode **1 / 1 passed**。
  - 指示書に書いていた「127 件 100% Green」は `[Explicit]` 凍結分を含む古い値であり、実測に合わせて訂正した。

### 2.4 ドキュメント同期とGitコミット
- [x] `docs/STATUS.md` および `docs/log.md` を更新し、Git コミットする（push は夜間安全ハーネスが担当）。
