# 実装規約

本ドキュメントは、AIコーディングエージェントが実装を行う際に遵守すべき規約を定義する。
各規約には理由を併記している。理由を理解した上で適用すること。

対象: Unity 6.3 LTS (6000.3.23f1) / 2D / URP / Web ビルドターゲット

---

## 1. 命名規則

### C#

| 対象 | 規則 | 例 |
|---|---|---|
| クラス・メソッド・ファイル名 | PascalCase | `PlayerStatus`, `StartTurn()` |
| private / protected フィールド | 先頭にアンダースコア1つ | `_currentStamina` |
| public プロパティ | PascalCase | `CurrentStamina` |
| ローカル変数・引数 | camelCase | `turnCount` |
| 定数 | PascalCase | `MaxTurn` |

`m_` および `s_` プレフィックスは使用しない。

### メソッド命名

- メソッド名は必ず動詞から始める（`AdvanceTurn()`, `ApplyCommand()`）
- イベントハンドラは `On` + 名詞 + 過去分詞（`OnTurnEnded()`）
- UIクリックハンドラは `On` + 対象 + `Click`（`OnStartClick()`）

理由: 関数が「自発的に実行するアクション」なのか「外部からの通知を受け取る
ハンドラ」なのかを、静的解析および他のエージェントが文字列ベースで判別できる
ようにするため。

### 接尾辞による役割の固定

| 接尾辞 | 用途 |
|---|---|
| `Config` / `Data` | ScriptableObject のクラス定義 |
| `Controller` | MonoBehaviour |
| `Manager` | シーン単位の統括 |
| `View` | 表示のみを担当する UI コンポーネント |

理由: 名前から継承元を決定的に推論できるようにするため。

### ファイル・ディレクトリ

- すべて PascalCase
- スペース、ハイフン、@、ユニコード文字を使用しない
- 区切りが必要な場合はアンダースコアのみ
- `Assets/` から数えて最大3階層まで

理由: シェル上でのパス解決時にエスケープ漏れによる構文エラーが発生すること、
および4階層以上では相対パスの推論ミスが発生しやすいため。

---

## 2. フォルダ構成

機能別構成を採用する。種類別（Scripts / Prefabs / Materials で分ける）は使用しない。

Game/Assets/
├── Core/
│   ├── Rendering/
│   ├── Audio/
│   └── Data/
├── Features/
│   ├── GameState/
│   ├── Command/
│   ├── Turn/
│   ├── Event/
│   └── Ending/
├── UI/
│   ├── Common/
│   └── Screens/
├── Art/
│   ├── Characters/
│   └── UI/
└── WebGLTemplates/

各 Feature 内は以下の構成とする。

Features/<機能名>/
├── Scripts/
├── Instances/
└── README.md

理由: 種類別構成では、1つの機能を修正する際に複数のディレクトリを横断して
検索する必要が生じる。機能別構成では変更の影響範囲が単一のツリー内に収まる。

`README.md` には設計意図と公開インターフェースのみを記載する。実装の詳細は
書かない（コードと二重管理になるため）。

---

## 3. アセンブリ定義

| アセンブリ | 依存先 | 責務 |
|---|---|---|
| `Game.Core` | なし（UnityEngine のみ） | 不変状態（`GameState`）、共通インターフェース、`EventChannelSO<T>` 基盤 |
| `Game.Features.Command` | `Game.Core` | コマンドデータ（`CommandDataSO`）および効果解決純粋関数（`CommandResolverSO`） |
| `Game.Features.Event` | `Game.Core` | ランダム・条件付きイベント（`GameEventSO`, `GameEventCatalogSO`）および評価解決（`EventResolverSO`） |
| `Game.Features.Ending` | `Game.Core` | エンディング定義（`EndingRuleSO`, `EndingRulesSO`）および判定解決（`EndingResolverSO`） |
| `Game.Features.Relic` | `Game.Core`, `Game.Features.Command` | レリック（パッシブ能力）定義（`RelicSO`, `RelicCatalogSO`）および効果合成純粋関数（`RelicResolverSO`） |
| `Game.Features.Boss` | `Game.Core`, `Game.Features.Command`, `Game.Features.Relic` | ボス定義（`BossSO`, `BossCatalogSO`）、不変戦闘状態（`BossState`）および1ターン自動解決純粋関数（`AutoBattleResolverSO`） |
| `Game.Features.GameFlow` | `Game.Core`, `Game.Features.Command`, `Game.Features.Event`, `Game.Features.Ending`, `Game.Features.Relic` | ゲーム進行ステートマシン MonoBehaviour（`GameFlowController`） |
| `Game.UI` | `Game.Core`, `Game.Features.Command`, `Game.Features.GameFlow`, `Game.Features.Relic`, `Unity.TextMeshPro`, `UnityEngine.UI` | 表示・入力ビューコンポーネント群（`StatusView`, `CommandButtonView`, `EventDialogView`, `EndingView`, `RelicDraftDialogView`） |
| `Game.Tests.EditMode` | 上記全アセンブリ, `UnityEngine.TestRunner`, `UnityEditor.TestRunner` | NUnit 単体テスト群（100% Green 維持） |

Feature 間の直接参照は `GameFlowController` のみに限定し、兄弟 Feature 同士（例: `Event` と `Ending`）の直接参照は禁止する（疎結合の維持）。

理由: 不適切な結合を含むコードが書かれた場合、コンパイラが参照エラーを返す。
指示文による制約より、コンパイラによる強制の方が確実である。

---

## 4. アーキテクチャ

3層に分離する。

| 層 | 実装 |
|---|---|
| ロジック | `record` 型の `GameState` と、純粋関数としての `CommandResolverSO` |
| 通信 | `EventChannelSO`（ScriptableObject 経由のイベント） |
| ビュー | イベントを購読するだけの薄い MonoBehaviour |

ビュー層はロジックを持たない。イベントを購読して画面を更新し、ボタンが
押されたら SO の関数を呼ぶだけの層とする。

理由: ロジックと UI が直接参照を持たないことで、両者を独立したタスクとして
実装できる。また、ロジックが MonoBehaviour のライフサイクルに依存しないため、
Test Runner 上で単体テストが可能になる。

---

## 5. `record` 型の使用制約

Unity のシリアライズ機構は C# の `record` に対応していない。
公式マニュアルに明記されている制約である。

| 対象 | `record` の可否 |
|---|---|
| `GameState`（シリアライズしない実行時の状態） | 使用可 |
| ScriptableObject のクラス定義 | **使用禁止。**通常のクラスで実装する |
| MonoBehaviour のフィールド型 | **使用禁止** |

`record` と `init` セッターを使用するには、`System.Runtime.CompilerServices.IsExternalInit`
を自前で宣言する必要がある。`Game.Core` アセンブリ内に配置済みである。

#### `GameState` の標準定義（不変レコード）
```csharp
public record GameState
{
    public int CurrentTurn { get; init; }
    public int Stamina { get; init; }
    public int Skill { get; init; }
    public int Mental { get; init; }
    public ulong FiredEventMask { get; init; } // 64ビット。イベントID 0〜63対応
    public IReadOnlyList<int> AcquiredRelicIds { get; init; } = Array.Empty<int>(); // 所持レリックID一覧
}
```

理由: ScriptableObject を `record` で定義すると、Inspector に何も表示されず、
`.asset` として保存しても値が永続化されない。エラーは出ないため、原因の
特定に時間を要する。

---

## 6. データ駆動

| 項目 | 規約 |
|---|---|
| 調整値（コマンドの効果値、パラメータ上限、イベント発火条件） | ScriptableObject のフィールドに持たせる |
| C# 側へのハードコード | 禁止 |
| 直接参照（`GetComponent` / Singleton） | 禁止。`EventChannelSO` を経由する |

理由: 調整値をデータ側に置くことで、コード変更なしにバランス調整が可能になる。
また、機能の削減時にデータ側の値を変えるだけで対応できる。

---

## 7. 確率的要素

型フェーズでは実装しない。コマンドの効果は固定値とする。

ただし、`CommandResolverSO.Execute()` は後から乱数を挿入できる構造を維持すること。
効果値は SO のフィールドから読むこと。

理由: 乱数が介在すると、不具合の原因が乱数のブレなのかロジックのエラーなのかを
切り分けられない。また、単体テストの生成が困難になる。

---

## 8. 非同期処理

| 項目 | 規約 |
|---|---|
| 非同期処理 | **UniTask を使用する** |
| `System.Threading.Tasks` の直接使用 | 禁止 |
| `Task.Result` / `Task.Wait()` | **全面禁止** |
| `Task.Run()` | **禁止** |
| `async void` | 禁止 |

理由: Web ビルドは単一スレッドで動作する。メインスレッドで `.Result` により
同期ブロックすると、タスクの完了通知がメインスレッドに到達できず、
**エラーログを出さずに完全にフリーズする**。エディタ上ではマルチスレッドが
有効なため正常に動作し、ビルドするまで発覚しない。

`async void` 内で発生した例外は、待機されないまま握り潰され、
エラー追跡が不可能になる。

---

## 9. アニメーション

| 項目 | 規約 |
|---|---|
| トゥイーン | **PrimeTween を使用する** |
| DOTween | 使用禁止 |
| 手動補間（Coroutine + Lerp） | 禁止 |
| フレームレート依存の処理 | 禁止。`Time.deltaTime` を使用する |
| 目標フレームレート | 60fps |

理由（DOTween 禁止）: リフレクションに依存しており IL2CPP のストリッピングと
相性が悪い。Transform のトゥイーン1つあたり約732バイトの GC アロケーションが
発生し、Web ビルドの GC 特性（フレーム終端でのみ実行）と組み合わさると
フレームドロップを起こす。ビルドサイズも約2.7MB増加した計測報告がある。

理由（フレームレート非依存）: 144Hz 環境で 2.4 倍速になる。ゲージの増減演出や
テキスト送り速度が破綻する。

---

## 10. 音声

| 用途 | Load Type |
|---|---|
| 短い SE | Decompress On Load |
| BGM | Decompress On Load |

| 項目 | 規約 |
|---|---|
| BGM の同時ロード | 1曲のみ。シーン遷移時に前の曲をアンロードする |
| SE の総数 | 10種以内 |
| AudioSource | オブジェクトプールで使い回す。都度生成・破棄を禁止 |
| ボイス | 型フェーズでは実装しない |
| Autoplay 対策 | タイトル画面に「クリックして開始」を必ず挟む |

理由（Streaming 不使用）: Chrome で再生インスタンスが約1,000回を超えると
WebMediaPlayer が枯渇し、すべての音声再生が停止する。

理由（Compressed In Memory 不使用）: Web ビルドでは全音声が AAC に強制変換される。
AAC のブロック境界のパディングにより、短い SE の終端でポップノイズが発生する。

理由（同時ロード制限）: Decompress On Load はメモリ上に非圧縮 PCM として展開する。
2MB のクリップが 200MB に肥大化した報告がある。

理由（Autoplay 対策）: ブラウザの Autoplay Policy により、ユーザー操作なしに
音は鳴らない。タイトルで BGM が自動再生される構成は Web では成立しない。

---

## 11. UI

| 項目 | 規約 |
|---|---|
| UI システム | uGUI（Canvas）。UI Toolkit は使用しない |
| 座標のハードコード | 禁止。アンカーとオフセットで配置する |
| UI カメラでの FXAA | 使用禁止 |
| カメラスタック | 型フェーズでは使用しない（単一カメラ構成） |
| テキスト入力（TMP_InputField） | 型フェーズでは実装しない |
| システムフォントへの依存 | 禁止 |
| 文字列連結 | `StringBuilder` を使用する。ループ内の `+=` を禁止 |

### 解像度・Canvas

| 項目 | 値 |
|---|---|
| アスペクト比 | 16:9 固定 |
| Reference Resolution | 1920 × 1080 |
| UI Scale Mode | Scale With Screen Size |
| Screen Match Mode | Expand |

### ピボット

| 対象 | ピボット |
|---|---|
| 立ち絵 | 下端中央（0.5, 0）。基準高さ 1080px |
| ボタン・パネル | 中央（0.5, 0.5） |
| ゲージ・バー | 左中央（0, 0.5） |
| テキスト | 左上（0, 1） |

理由（UI Toolkit 不使用）: USS とC#コードが分離するため、コンテキストの消費が
増え、推論ミスの原因になる。

理由（FXAA / カメラスタック回避）: Unity 6 の URP Rendergraph において、
カメラスタックと FXAA を併用すると UI がレンダリングされない不具合が
報告されている（UUM-110338）。

理由（システムフォント禁止）: Web のサンドボックス制約により
`Font.GetOSInstalledFontNames()` は 0 個を返す。日本語が豆腐になる。

理由（TMP_InputField 回避）: Web ビルドで日本語 IME が機能せず、
Unity 6 ではスペースキーが入力できない不具合も報告されている。

理由（StringBuilder）: Web の GC はスタックが空になるフレーム終端でのみ実行
される。フレーム内の文字列連結ループが OOM を起こす。

---

## 12. セーブデータ

| 項目 | 規約 |
|---|---|
| API | PlayerPrefs |
| 保存後 | **必ず `PlayerPrefs.Save()` を明示的に呼ぶ** |
| 対象ブラウザ | Chrome / Firefox / Edge |
| Safari | 動作保証外 |

理由: PlayerPrefs は IndexedDB に仮想化される。`SetString()` だけでは
非同期フラッシュが完了する前にタブが閉じられ、データが消失する。

Safari は ITP により、7日間アクセスがないとストレージを警告なく全削除する。
クライアント側の回避策は存在しない。

---

## 13. ファイル操作の制約

| 項目 | 規約 |
|---|---|
| アセットの移動・リネーム・削除 | **Unity エディタ経由（Unity-MCP）のみ** |
| シェルコマンド（`mv` / `rm` / `cp`）によるアセット操作 | **禁止** |
| `Assets/Resources/` の使用 | 禁止 |
| 空フォルダの作成 | 禁止 |
| `.cs` の書き込み | Claude Code 経由のみ |

理由（シェルコマンド禁止）: Unity はすべてのアセットに GUID を記録した
`.meta` ファイルを持ち、プレハブやシーンはパスではなく GUID で参照している。
シェルで本体のみを移動すると `.meta` が取り残され、GUID 参照が広範囲に破損する。

理由（Resources 禁止）: 文字列パスによる動的ロードは、ファイル移動時に
コード側が自動更新されず、実行時になって初めて NullReferenceException が発生する。

理由（空フォルダ禁止）: Git は空ディレクトリを追跡しない。`.meta` のみが
コミットされ、別環境で GUID の不整合ループが発生する。

---

## 14. 禁止事項の一覧

| # | 禁止事項 |
|---|---|
| 1 | `Assets/Resources/` の使用 |
| 2 | シェルコマンドによるアセット操作 |
| 3 | 空フォルダの作成 |
| 4 | `Task.Result` / `Task.Wait()` / `Task.Run()` / `async void` |
| 5 | DOTween の使用 |
| 6 | 座標・調整値のハードコード |
| 7 | UI カメラでの FXAA、カメラスタック |
| 8 | システムフォントへの依存 |
| 9 | TMP_InputField の使用 |
| 10 | ScriptableObject / MonoBehaviour への `record` 型の使用 |
| 11 | UI Toolkit の使用 |
| 12 | Feature 同士の横方向の依存 |

## 15. AI生成の明示

エージェントが生成した `.cs` ファイルの先頭に、以下の形式で1行を記述する。

    // SPDX-AI-Disclosure: ai-generated

値は以下から選ぶ。

| 値 | 意味 |
|---|---|
| `none` | AI の関与なし |
| `ai-assisted` | 人間が書き、AI が補助した |
| `ai-generated` | AI が生成し、人間がレビューした |

本プロジェクトでは、エージェントが生成したファイルは `ai-generated` とする。

出典: https://github.com/ggfevans/ai-disclosure （CC0、任意規約）
