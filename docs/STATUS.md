# プロジェクト現在地（STATUS）

最終更新日：2026-09-02

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
| **第3週 Step 9** | シーン結合 & アセット構成（MainGame.unity） | 完了（第3週全工程ゴール達成！🎉） | `be571a0` |
| **監査事前補強** | イベント上限ulong拡張・フェーズガード修正 | 完了 | `154fa2e` |
| **第4週 Step 10** | レリック（パッシブ能力）基盤（57/57テスト通過） | **完了 🎉** | `6681d5d` |
| **第4週 Step 11** | レリック3択ドラフトUI & GameFlowController結合（64/64テスト通過） | **完了 🎉** | `HEAD` |
| **第5週 Step 12** | ボスデータ基盤（BossState, BossSO, BossCatalogSO） | **完了** | `59b86d5` |
| **第5週 Step 13** | オートバトルResolver（AutoBattleResolverSO, 84/84テスト通過） | **完了** | `59b86d5` |
| **第5週 Step 14** | GameFlowController ボスバトル統合 & ステート遷移（88/88テスト通過） | **完了** | `90aae0c` |
| **第5週 Step 15** | BossBattleDialogView & MainGameシーン配置 & 1,000回シミュレーション（93/93テスト通過） | **完了 🎉** | `648565c` |
| **第6週 Step 16** | 周回メタ基盤（MetaProfileState, MetaUnlockSO, MetaPointResolverSO） | **完了** | `HEAD` |
| **第6週 Step 17** | 単体テスト（MetaPointResolverTests, 41件一括通過） | **完了** | `HEAD` |
| **第6週 Step 18** | GameFlowController 周回メタ統合 & 初期ステータス底上げ | **完了** | `HEAD` |
| **第6週 Step 19** | MetaShopDialogView & MainGameシーン結合 & 1,000回周回シミュレーション（125/125テスト通過） | **完了 🎉** | `HEAD` |
| **第7週 Step 20** | ボス4段階化（Act 1〜4 / Turn 6・12・18・24）・GameFlowController のマルチAct対応・Monte Carloテスト更新（127/127テスト通過） | **完了 🎉** | `HEAD` |

---

## 2. 次にやること

1. **第7週 残タスク**:
   - `Game/Assets/Scenes/MainGame.unity` 上の `GameFlowController` が `_bossCatalog` / `_autoBattleResolver` /
     `_relicCatalog` / `_relicResolver` / `_metaPointResolver` / `_metaUnlockCatalog` 未バインド（`fileID: 0`）のまま。
     Unity エディタで `Tools > Generate Boss Assets` → `Tools > Bind Boss to MainGame Scene` を実行し、
     Relic / MetaProgression も同様のバインダー経由で結線すること（シーンの直接テキスト編集は禁止のため、
     Unity-MCP 不在の本セッションでは未実施）。
   - WebGL ビルドのコマンドラインでの実機ビルド確認（本セッションでは Unity 実行環境がないため未実施。
     `Tools/Build WebGL` の静的検証のみ完了）。
   - README.md のアーキテクチャ図・テスト件数（127件）への同期。

---

## 3. リソース管理・自律監視状態

- **Claude Code 週間枠**: **4% used（残り 96%・7日間）**
- **Claude Code 5h枠**: **50% used（残り 50%）**
- **Gemini 利用枠**: 残り **83.5%**（5h枠 37%）
- **EditMode テスト**: **127 / 127 passed（静的レビュー基準・Unity Test Runner 未実行）**
