指示書: docs/instructions/26_invariant_assertions.md
消化: 6 / 6

## 機械判定
```
PASS Build: 0 errors (warnings: 3)
PASS Changed lines: 0 added lines (deleted 0 <= 300)
PASS Protected files: None modified (0)
FAIL Tests: EditMode: total=116 passed=96 failed=0 skipped=20 [mtime: 2026-09-05 12:11:16] / PlayMode: total=16 passed=6 failed=10 skipped=0
PASS Snapshot: docs/snapshot/scene_bindings.txt no diff (0 lines)
```
※ PlayMode FAIL 10 件の内訳:
- `SerializedBindingTest` 9 件（未バインド 8 件 + NUnit 戻り値型制限 8 件）: 既存の想定内 FAIL。
- `GameInvariantsTest.Invariants_INV6_SubsystemsFiredAtLeastOnce` 1 件: 指示書 26 で明記された「現時点で赤が正しい」想定通りの FAIL（直さず維持）。

## 不変条件 (INV-1〜6) の検証結果

| 不変条件 | 内容 | 判定 | 実測値・観測結果 |
|---|---|---|---|
| **INV-1** | どのフェーズも WaitingInput または終端に到達せずに 10 秒以上留まらない | **緑 (PASS)** | 全 24 ターン進行および敗北経路でタイムアウトなし（正常進行） |
| **INV-2** | 終端到達時に EndingDecidedChannelSO が 1 回以上通知される | **緑 (PASS)** | クリア時・敗北時ともに 1 回以上の通知を受信 |
| **INV-3** | WaitingInput 中に Canvas の 90% 以上を覆う raycastTarget ブロッカーが存在しない | **緑 (PASS)** | 各ターン WaitingInput 時に全画面ブロッカー残存 0 件 |
| **INV-4** | 表示中の全 TextMeshProUGUI にフォントグリフが揃っている | **緑 (PASS)** | 全表示中テキストで欠落グリフ 0 件 |
| **INV-5** | 表示中かつ interactable な全 Button の中心へ Raycast が到達する | **緑 (PASS)** | 全ボタンでブロック遮断 0 件（クリック可能） |
| **INV-6** | 1 ランを通したサブシステム発火数がすべて 1 以上 | **赤 (FAIL)** | イベント: **0 回**, ボス戦: **4 回**, ドラフト: **3 回**（**直さず維持**） |

※ INV-6 は指示書 26 の指示通り、イベント未発火の現状を検出して赤になることを確認し、プロダクションコードの修正・原因調査は行わずそのまま記録しています。

## 検証 4・5（意図的破壊による検知確認）

アサーションが故障時に正しく鳴ることの生出力検証。

### 検証 4: INV-1 意図的破壊の確認
しきい値を一時的に短縮し、フェーズが滞留した場合の検知メッセージを確認（確認後、即座に元に戻し完了）。
```
[INV-1] Phase 'WaitingInput' did not advance for 0.02s (timeout: 0.00s). Last action: ClickCommand_TrainButton
```
→ どのフェーズで、直前のアクションが何であったかが明確に出力されることを確認。

### 検証 5: INV-4 意図的破壊の確認
テキストに未収録グリフ（日本語文字列）を一時的に注入し、グリフ欠落時の検知メッセージを確認（確認後、即座に元に戻し完了）。
```
[INV-4] Missing font glyphs detected:
'Canvas/CommandPanel/StudyButton/Text' (Text: "勉強（日本語テスト）") missing glyphs: '勉'(U+52C9), '強'(U+5F37), '（'(U+FF08), '日'(U+65E5), '本'(U+672C), '語'(U+8A9E), 'テ'(U+30C6), 'ス'(U+30B9), 'ト'(U+30C8), '）'(U+FF09)
```
→ どのオブジェクトの、どの文字が欠けているかが明確に出力されることを確認。

## 止まった箇所
なし（指示書 26 の全検証項目 1〜6 完了）

## 変更ファイル
- `Game/Assets/Tests/PlayMode/GameInvariants.cs`（新規作成: コミット `3cfca94`）
- `Game/Assets/Tests/PlayMode/GameInvariantsTest.cs`（新規作成）
- `docs/handoff/2026-09-05-06/04-result.md`（新規作成）
※ プロダクションコード（`Game/Assets/Core`, `Features`, `UI`）およびシーン・アセットの変更は 0 件。
