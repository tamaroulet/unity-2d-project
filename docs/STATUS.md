# プロジェクト現在地（STATUS）

最終更新日：2026-09-03

---

## 1. 進行状況

| フェーズ / Step | 対象 | 状態 | コミット |
|---|---|---|---|
| **Phase 0** | 環境構築・ツール疎通 | 完了 | |
| **第1週 Step 1** | Core の型定義 | 完了 | `e79c0a4` |
| **第1週 Step 2** | Command の実装 | 完了 | `2fe407e` |
| **第1週 Step 3** | EditMode テスト（11件通過） | 完了 | |
| **環境検証** | Web ビルド・GitHub Pages 公開 | 完了 | |
| **第2週 Step 4** | イベントチャンネル基盤（16/16テスト通過） | 完了 | `233f083` |
| **第2週 Step 5** | イベント定義と条件評価（25/25テスト通過） | 完了 | `ec34748` |
| **第2週 Step 6** | エンディング判定（32/32テスト通過） | 完了 | `cdeb9fe` |
| **第3週 Step 7** | ゲーム進行マネージャー（38/38テスト通過） | 完了 | `9e45bec` |
| **第3週 Step 8** | UI ビューコンポーネント（47/47テスト通過） | 完了 | `c4d332d` |
| **第3週 Step 9** | シーン結合 & アセット構成（MainGame.unity） | 完了 | `be571a0` |
| **監査事前補強** | イベント上限ulong拡張・フェーズガード修正 | 完了 | `154fa2e` |
| **第4週 Step 10** | レリック（パッシブ能力）基盤（57/57テスト通過） | 完了 | `6681d5d` |
| **第4週 Step 11** | レリック3択ドラフトUI & GameFlowController結合（64/64テスト通過） | 完了 | `HEAD` |
| **第5週 Step 12** | ボスデータ基盤（BossState, BossSO, BossCatalogSO） | 完了 | `59b86d5` |
| **第5週 Step 13** | オートバトルResolver（AutoBattleResolverSO, 84/84テスト通過） | 完了 | `59b86d5` |
| **第5週 Step 14** | GameFlowController ボスバトル統合 & ステート遷移（88/88テスト通過） | 完了 | `90aae0c` |
| **第5週 Step 15** | BossBattleDialogView & MainGameシーン配置 & 1,000回シミュレーション（93/93テスト通過） | 完了 | `648565c` |
| **第6週 Step 16** | 周回メタ基盤（MetaProfileState, MetaUnlockSO, MetaPointResolverSO） | 完了 | `HEAD` |
| **第6週 Step 17** | 単体テスト（MetaPointResolverTests, 41件一括通過） | 完了 | `HEAD` |
| **第6週 Step 18** | GameFlowController 周回メタ統合 & 初期ステータス底上げ | 完了 | `HEAD` |
| **第6週 Step 19** | MetaShopDialogView & MainGameシーン結合 & 1,000回周回シミュレーション（125/125テスト通過） | 完了 | `HEAD` |
| **第7週 Step 20** | ボス4段階化（Act 1〜4）・マルチAct対応・127件テスト（100% Green） | 完了 | `814cc19` |
| **第7週 Step 21** | 単純図形UIワイヤーフレーム配置（UILayoutBuilder）＆シーン完全バインド | 完了 | `814cc19` |
| **第7週 Step 22** | WebGL ローカルビルド確定（WASM 8.4MB / Data 4.5MB）＆ AutoRunner 常駐 | 完了 | `54704c5` |

---

| **Gate 4** | PlayMode 曳光弾 `SmokeTest.cs`（MainGame 実ロード＋実 uGUI クリック） | 完了（100% Passed） | `36cb67b` |
| **Gate 5** | 手放し自動化インフラ（GameCI / 夜間安全ハーネス / 朝刊レポート）配備 | 完了（実証テスト合格） | `bb1f3e9` |

---

## 2. 次にやること

1. **モック通しプレイの開通（基本図形のまま完走）**:
   - `GameFlowController` で一時停止する `ShowingRelicDraft`（レリック3択）の配線を完了させ、24ターン〜ボス戦〜エンディングまで一気通貫で動く動的モックを完成させる。
2. **AI主導ゲーム開発フレームワークの体系化資料まとめ**:
   - 今回実証された「PlayMode CI ✕ 隔離ハーネス ✕ Triad Protocol」を、次回作（2.5Dアクション等）で即座に流用できる汎用テンプレートとしてドキュメント化する。
3. **人間ディレクターの夜間ルーティン**:
   - 就寝前に Unity エディタを終了する（夜間ランナーが Library ロックで停止するのを防ぐため）。

---

## 3. リソース・自動化状態（最新実測値）

- **EditMode テスト**: **127 / 127 passed（100% Green）**
- **PlayMode テスト**: **1 / 1 passed（SmokeTest 100% Green）**
- **安全ハーネス**: `scripts/nightly_gate.py`（不正コード検知時の自動隔離・ロールバック実証済み）
- **朝刊レポート**: `scripts/morning_report.py`（Task Scheduler `UnityProject_MorningReport` 06:10 登録済み）
- **夜間自律ランナー**: Task Scheduler（`UnityProject_AutoRunner` 毎日 01:00〜06:00、30分間隔）登録済み
- **引き継ぎマスターガイド**: `docs/workflow/ONBOARDING.md` 整備完了
