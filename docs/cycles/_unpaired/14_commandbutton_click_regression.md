# 指示書 14: CommandButtonView のクリック不達を直す

```
ROLE: Executor
BRANCH: main（未コミットの変更あり。捨てずに続きから作業する）
```

## 目的

シーン再生成後、PlayMode テストが落ちている。Study ボタンのクリックがハンドラに届かない。

```
Failed  MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions
        Expected log did not appear: [CommandButtonView] Clicked button for command: Study
```

テスト中の全ログは 3 行のみで、`[CommandButtonView] Clicked...` が**一度も出ていない**。
例外もゼロ。テストの前段（ボタン発見・interactable・pointerClick 受理）は通過している。
つまり `Button.onClick` にリスナーが登録されていない可能性が高い。

## 既に潰した仮説（再調査しないこと）

`MainGame.unity` を再生成前（コミット `138d1b9`）と比較し、以下は**すべて同一**と実測済み。

- `StudyButton` の GameObject 名
- 同 GameObject 上の component 構成と型（RectTransform / CanvasRenderer / Image / Button / CommandButtonView）
- `CommandButtonView._button` の参照先（同一 GameObject の Button を指している）
- `CommandButtonView._command`（Study）
- `m_Enabled`（Button も CommandButtonView も 1）
- GameObject のアクティブ状態

唯一の差は `m_EditorClassIdentifier` が `Game.UI::` から `Game::` に変わったこと。
これは asmdef 統合の反映であり、正しい状態。

## スコープ

- 触ってよい: `Game/Assets/UI/Scripts/CommandButtonView.cs`、`Game/Assets/Editor/UILayoutBuilder.cs`
- 触ってはいけない: `.agents/rules/**`、`scripts/nightly_gate.py`、`scripts/auto_runner.py`、
  `scripts/morning_report.py`、`scripts/nightly_baseline.json`、`.github/workflows/**`、
  `.claude/**`、`Game/Packages/manifest.json`、`.mcp.json`
- `.unity` / `.prefab` / `.asset` / `.meta` はテキスト編集しない。
  変更が要るなら `Game/Assets/Editor/` の Editor スクリプト経由で行う（`00_rules.md` 参照）
- 他の View や UI Toolkit パイロットには手を出さない

## 進め方

**推測でコードを直さないこと。** まず `CommandButtonView.OnEnable` と `OnCommandClick` の
先頭に一時的な `Debug.Log` を入れ、PlayMode を 1 回回して「どこまで到達しているか」を確定させる。

- `OnEnable` のログが出ない → コンポーネントが有効化されていない
- `OnEnable` は出るが `_button` が null → 参照の解決に失敗している
- 両方出るのに `OnCommandClick` が出ない → リスナー登録の問題

**同じ修正を 2 回試して直らなければ、手を止めて報告すること**（`00_rules.md` 停止条件）。

原因が特定できたら診断用の `Debug.Log` は削除する。

## 検証

| # | コマンド | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー（2〜3 秒） |
| 2 | PlayMode テスト実行 | 1 passed / 0 failed |
| 3 | `git status --porcelain` | 保護対象ファイルが 1 件も無い |

Unity を開いているなら Unity-MCP の `run_tests` を使ってよい（数秒で済む）。
閉じているなら batchmode（1 回 2 分）。

## 報告

```
指示書: docs/cycles/_unpaired/14_commandbutton_click_regression.md
原因: （何が起きていたか）
修正: （何を変えたか）
--- 検証の生出力 ---
（上の 1〜3 の出力をそのまま）
---
停止条件への抵触: なし / あり（内容）
```

コミットのみ行い、push はしない。
