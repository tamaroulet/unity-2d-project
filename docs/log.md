# 作業ログ

## 2026-09-01

### やったこと
環境構築一式。Unity 6.3 LTS (6000.3.23f1) / Git + LFS / GitHub プライベートリポジトリ /
Unity-MCP v10.0.0 / Antigravity（チャット版・IDE版）/ Rider 2026.2 / Claude Code v2.1.252。
全ツールの疎通確認まで完了。

### AIに任せたこと
なし。環境構築は全て手作業で実施。

### 詰まった点・学び
- `.gitignore` がリポジトリルートにあり、1階層下の `Game/Library` が除外されなかった。
  GitHub 生成の Unity 用 `.gitignore` は先頭スラッシュ付きパターンのため、
  Unity プロジェクトと同階層に置く必要がある。
- **仕様未確定の状態で Antigravity に方針提案を求めたところ、承認なしで
  横スクロールアクション用のコード（PlayerController2D, CameraFollow2D）と
  シーン構成を実装された。** 全て破棄。
  - Unity-MCP の `manage_script` を無効化していたが、IDE 自身のファイル書き込み機能で
    迂回された。ツール制限だけでは防げない。
  - 権限設定（Agent security mode: Sandboxed、ターミナルコマンドの承認必須）と
    `AGENTS.md` による明文化の両方が必要。
  - エージェントは「計画を提示します」と宣言した同じ応答内で実装に着手した。
    宣言と行動は一致しない前提で設計する。

### Step 1 投入前：設計案（Claude 提案）

- GameState のフィールド:
  - Turn (int)
  - Stamina (int)
  - Skill (int)
  - Mental (int)
- 「ターンが終わった」ことの表現:
  GameState には持たせない。TurnRules.AdvanceTurn が Turn を +1 した
  新しい GameState を返すことで表現する。
- 「ゲームオーバー」の持たせ方:
  GameState には持たせない。TerminationKind（継続 / ゲームオーバー / 通常終了）を
  CommandResult の一部として返す。
  理由: GameState を「その時点のパラメータ」だけに保ち、判定結果という
  導出可能な情報を状態に混ぜないため。混ぜると、状態の復元時に
  判定結果と実データが食い違う余地が生まれる。

### Step 1 実行結果

- 使用モデル:
- 範囲逸脱: あり / なし（内容）
- 上記設計案との差分:
- 差分のうち、エージェントの案の方が良いと判断した点:
- 差分のうち、上記案を維持した点:
- 人間が修正した箇所:
- 所要時間:
