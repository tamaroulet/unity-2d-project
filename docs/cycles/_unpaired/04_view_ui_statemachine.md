# 第3週 指示書：ビュー層・UI・ターン進行ステートマシン統合

本指示書は、第1週（Command）および第2週（Event / Ending）で完成したドメインロジックを、
MonoBehaviour、uGUI、および EventChannelSO を用いて画面上でプレイ可能な状態に統合するための規約・手順を定義する。

---

## 1. 全体の方針とアーキテクチャ

CodingSpec 4節「アーキテクチャ」の3層分離を厳守する。

| 層 | 実装 | 責務 |
|---|---|---|
| **ロジック層** | `GameState`, 各 `ResolverSO`（第1・2週完了済み） | 純粋関数による計算・判定（変更しない） |
| **通信層** | `EventChannelSO<T>`（第2週完了済み） | ScriptableObject 経由のイベント通知 |
| **進行管理層** | `GameFlowController`（MonoBehaviour） | ターン進行、状態遷移、Resolver と Channel のオーケストレーション |
| **ビュー層** | `StatusView`, `CommandButtonView`, `EventDialogView`, `EndingView` | イベントを購読して画面更新、ユーザー操作を通知 |

**【厳守事項】**
- MonoBehaviour は計算ロジック（パラメータの加算・判定等）を持たず、必ず Resolver SO に委譲する。
- MonoBehaviour 同士の直接参照（Singleton / `FindObjectOfType` / 相互の `GetComponent`）を禁止する。通信は `EventChannelSO` を経由する。
- UI は uGUI（Canvas: 1920×1080, Scale with Screen Size, Expand）を使用する。

---

## 2. ステップ構成

| Step | 名称 | 概要 | 対象アセンブリ |
|---|---|---|---|
| **Step 7** | ゲーム進行マネージャー | ターン進行、状態遷移、Resolver と Channel の統合 | `Game.Features.GameFlow` |
| **Step 8** | UI ビューコンポーネント | パラメータ表示、コマンドボタン、イベント・エンディング画面 | `Game.UI` |
| **Step 9** | シーン構築 & アセット結合 | `MainGame.unity` シーンの自動構成と通し動作検証 | Unity シーン |

---

## 3. Step 7：ゲーム進行マネージャー（GameFlowController）

### 3-1. 作成するファイル

* アセンブリ: `Game.Features.GameFlow`（参照: `Game.Core`, `Game.Features.Command`, `Game.Features.Event`, `Game.Features.Ending`）
* 配置ディレクトリ: `Assets/Features/GameFlow/Scripts/`

| ファイル | 内容 |
|---|---|
| `GamePhase.cs` | `enum GamePhase { TurnStart, ShowingEvent, WaitingInput, ExecutingCommand, TurnEnd, GameOver, GameClear }` |
| `GameFlowController.cs` | MonoBehaviour。ゲーム進行ステートマシン |
| `GameFlowControllerTests.cs` | EditMode / PlayMode テスト（状態遷移・イベント通知の検証） |

### 3-2. `GameFlowController` の設計要件

* **保持する ScriptableObject 参照（インスペクター設定用）**:
  - `GameRulesSO _gameRules`
  - `CommandResolverSO _commandResolver`
  - `GameEventCatalogSO _eventCatalog`
  - `EventResolverSO _eventResolver`
  - `EndingRulesSO _endingRules`
  - `EndingResolverSO _endingResolver`
  - `GameStateEventChannelSO _gameStateChannel`（状態変化通知用）
  - `GameEventFiredChannelSO _eventFiredChannel`（イベント発生通知用）
  - `EndingDecidedChannelSO _endingDecidedChannel`（エンディング通知用）
* **主なメソッド**:
  - `StartGame()`: `_gameRules.CreateInitialState()` で初期状態を生成し、`_gameStateChannel.Raise(state)` を呼んで `TurnStart` へ。
  - `ExecuteCommand(CommandSO command)`: コマンド選択時に呼ばれ、`_commandResolver.Resolve` を実行して状態更新・通知。
  - `AdvanceTurn()`: ターンを進め、最終ターン到達時は `_endingResolver.Resolve` を呼んで `_endingDecidedChannel.Raise()`。

---

## 4. Step 8：UI ビューコンポーネント（Game.UI）

### 4-1. 作成するファイル

* アセンブリ: `Game.UI`（参照: `Game.Core`, `Game.Features.Command`, `Game.Features.Event`, `Game.Features.Ending`, `Game.Features.GameFlow`, `Unity.TextMeshPro`, `UnityEngine.UI`）
* 配置ディレクトリ: `Assets/UI/Scripts/`

| ファイル | 内容 |
|---|---|
| `StatusView.cs` | `GameStateEventChannelSO` を購読し、Stamina, Skill, Mental, Turn テキスト/ゲージを更新 |
| `CommandButtonView.cs` | コマンドボタン。押下時に `GameFlowController.ExecuteCommand` を呼び出す |
| `EventDialogView.cs` | `GameEventFiredChannelSO` を購読し、イベント発生時にダイアログを表示 |
| `EndingView.cs` | `EndingDecidedChannelSO` を購読し、リザルト画面を表示 |

---

## 5. Step 9：シーン構築 & アセット結合

* Unity-MCP を使用して `Assets/Scenes/MainGame.unity` を構成。
* UI Canvas、EventSystem、Main Camera を配置。
* 各 ScriptableObject アセットの生成とインスペクター参照のバインド。

---

## 6. 各 Step の完了条件

- Test Runner の EditMode / PlayMode テストが全件通過すること。
- コンパイルエラー・新規コンパイル警告が 0 件であること。
