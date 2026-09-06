# 自律継続実行環境（Auto Runner）

## 概要
エージェントセッションが終了しても、Windows Task Scheduler が定期的に `auto_runner.py` を起動し、次のタスクを自動的に前倒し実行する仕組み。

## アーキテクチャ

```
Windows Task Scheduler (30分間隔)
  └─ python scripts/auto_runner.py
       ├─ 1. scripts/get_all_quotas.ps1 でクォータ確認
       ├─ 2. docs/status.md を読み取り、次のタスクを特定
       ├─ 3. Gemini 25% 未満 → Claude 優先モードフラグ
       ├─ 4. Antigravity Python SDK (google-antigravity) でエージェント起動
       │     └─ Agent.chat() でプロンプト実行
       ├─ 5. SDK 失敗時 → agy CLI フォールバック
       └─ 6. logs/auto_runner/ にログ出力
```

## ファイル構成

| ファイル | 役割 |
|---|---|
| `scripts/auto_runner.py` | SDK を使用した自律実行メインスクリプト |
| `scripts/register_scheduled_task.ps1` | Windows Task Scheduler へのタスク登録 |
| `logs/auto_runner/` | 実行ログ出力ディレクトリ |

## セットアップ手順

### 1. SDK インストール（済）
```powershell
pip install google-antigravity
```

### 2. Windows Task Scheduler 登録
```powershell
# 管理者権限の PowerShell で実行
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/register_scheduled_task.ps1
```

### 3. 手動テスト実行
```powershell
python scripts/auto_runner.py
```

### 4. 管理コマンド
```powershell
# 手動で即時実行
schtasks /run /tn "UnityProject_AutoRunner"

# 状態確認
schtasks /query /tn "UnityProject_AutoRunner" /v

# 削除
schtasks /delete /tn "UnityProject_AutoRunner" /f

# 間隔変更（例: 15分間隔）
powershell -File scripts/register_scheduled_task.ps1 -IntervalMinutes 15
```

## 動作フロー

1. Task Scheduler が 30分ごとに `auto_runner.py` を起動
2. クォータを確認し、Gemini/Claude の使い分けを決定
3. `docs/status.md` から次の未完了 Step を特定
4. Antigravity SDK の `Agent.chat()` でエージェントを起動
5. エージェントが自律的に実装・テスト・コミット・プッシュを実行
6. 結果を `logs/auto_runner/` に記録
7. 45分の実行時間制限で強制終了（次回サイクルで再試行）
