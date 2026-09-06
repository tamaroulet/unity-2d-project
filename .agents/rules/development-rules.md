---
trigger: always_on
---

# 開発ルール（宣言的制約のみ）

このファイルが唯一のルールである。手続き（If-Then の対処法）は書かない。
違反は `.claude/hooks/guard.js` と CI が物理的にブロックする。文章で守らせるのではない。

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
- `.unity` / `.prefab` / `.asset` を **AI が読み込むことも禁止**（Read / 広い grep /
  `git show` での本文表示）。`MainGame.unity` は単体で 10 万トークンを超え、
  1 回読むだけでセッション枠を食い潰す。結線の確認手段は次の 3 つだけ。
  - `Tools/Report Unbound Serialized Fields`（`SceneBindingReport`）の出力
  - `04-result.md` に貼られた実測値
  - フィールド 1 件を名指しする `grep -n "_fieldName:" -m 5`（前後の文脈は取らない）

  コミット差分の確認は `git show --stat` まで。`.unity` の本文差分は表示しない。
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

## ここに書かないもの

席の割り当て・停止条件・報告様式・知識の蓄積・文体は、この作業場の共通規約にある。
  C:/dev/harness/docs/decision-axes.md
  C:/dev/harness/docs/report-format.md
  C:/dev/harness/config/execution-paths.json
  C:/dev/CLAUDE.md
対応は docs/rules-migration-map.md にある。
