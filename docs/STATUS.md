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
| **第2週 Step 5** | イベント定義と条件評価 | **進行中（計画・実装中）** | |
| **第2週 Step 6** | エンディング判定 | 未着手 | |

---

## 2. 次にやること

1. **Step 5（イベント定義と条件評価）の実行**:
   - `Assets/Features/Event/Scripts/` 配下のクラス群実装
   - `GameState.cs` への `FiredEventMask` フィールド追加（唯一の許可された Core 変更）
   - `EventResolverSO`（純粋関数）の実装
   - EditMode 単体テスト（`EventResolverSOTests` 9項目）の実行・検証

---

## 3. ブロッカー・未確認事項

| # | 項目 | 内容 | 状態 |
|---|---|---|---|
| 1 | `Assets/Features/Event/Scripts/` フォルダ | 人間による作成が必要（存在確認済み） | 解決済み |
| 2 | `Game.Features.Event.asmdef` | 存在確認済み | 解決済み |
