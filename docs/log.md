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

### Step 1 投入（1回目）
- 結果: 実装に着手せず停止。指示書 #2 と AGENTS.md 第1節の矛盾を自力で検出
- 検出内容: 指示書が即時実行を求める一方、AGENTS.md は計画提示と承認の分離を義務化
- 判定: 適合。Antigravity の事故（宣言と実装の同一応答内実行）とは対照的
- 対応: 指示書 #2 第6節を修正し、計画提示を前提とする手順に変更
- 原因: 指示書作成時に AGENTS.md との整合性が確認されていなかった（指示書側の欠陥）

### Step 1 計画レビュー
- 範囲逸脱: なし（作成対象は Assets/Core/Data/ 配下の .cs 4本のみ）
- 差し戻し: 1件。CommandEffect の readonly record struct が C# 10 機能であり
  Unity 6 の既定（C# 9）でコンパイル不可
- 承認した設計判断:
  - StaminaDelta と StaminaCost の分離（Rest が +30/消費0 のため必要）
  - EvaluateTermination をフェーズ引数なしとし、Resolver 側で2回呼ぶ構成
- 設計案との差分: TerminationKind の持たせ方は事前案と一致。
  CommandEffect のフィールド分割は事前案になく、エージェント側の指摘が正しかった
- 所感: 計画段階で規約違反を自力検出した点、および指示書の記述不足
  （Stamina の増減と消費量の区別）を計画段階で埋めた点は、委譲先として良好

### Step 1 実行結果（2026-09-01）
- 使用モデル: Sonnet 5
- 範囲逸脱: なし（Assets/Core/Data/ 配下の .cs 4本のみ）
- 禁止 API の参照: なし
- コンパイル: 通過。record + IsExternalInit が Unity 6.3 で動作することを実測
- 差し戻し: 1件（readonly record struct → readonly struct。C# 10 機能のため）
- 設計案との差分:
  - 一致: TerminationKind を GameState に持たせず戻り値側に置く構成
  - 差分: CommandEffect に StaminaDelta と StaminaCost を分離。
    事前案にはなく、エージェント側の指摘が正しかった（Rest が +30 / 消費0 のため）
- 所要時間:
- コミット: e79c0a4

### Step 2 計画レビュー
- 範囲逸脱: なし
- エージェントが検出した問題: CodingSpec 1節（Config/Data 接尾辞）と
  4節（SO 接尾辞）の規約内部矛盾。指示書との矛盾として報告されたが、
  実態は規約自身の不整合
- 判断: SO 接尾辞を正とし、CodingSpec 1節を改訂（ADR 発行）
- 差し戻し: 1件。GameRulesSO に CreateInitialState() を追加。
  初期状態の組み立て場所が未定義のままだと、第3週にビュー層へ漏れるため
- 所感: 2回連続で規約の不備を計画段階で検出。指示書と規約の整合確認が
  人間側の工程として不足している

### Step 2 コンパイルエラー（CS0518）
- 症状: Game.Features.Command の CommandResult で init セッターが
  コンパイル不可
- 原因: IsExternalInit を Game.Core に internal で宣言していたため、
  別アセンブリから参照できなかった
- 対応: internal → public に変更。BuildSpec 第9節も修正
- 責任: 指示書・規格側の設計漏れ。エージェントの実装は計画通り
- 教訓: アセンブリ分割とアクセス修飾子の組み合わせは、
  単一アセンブリでの動作確認では検出されない
