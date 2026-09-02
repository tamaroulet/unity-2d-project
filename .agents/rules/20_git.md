---
trigger: always_on
---

# git 操作

## 1. 許可するコマンド

以下のみを実行してよい。

- `git status`
- `git diff`
- `git log`
- `git pull --rebase`
- `git add <パス>`
- `git commit`
- `git push`

## 2. 禁止するコマンド

以下を実行してはならない。必要になった場合は停止して報告する。

- `git push --force`、`git push -f`
- `git reset --hard`
- `git rebase -i`
- `git clean`
- `git checkout --`、`git restore` によるファイルの破棄
- ブランチの削除
- `gh-pages` ブランチへの一切の操作

理由: いずれも履歴または未コミットの作業を破壊する。
本プロジェクトは工程記録が第一目的であり、履歴の破壊は成果物の損失に当たる。
`gh-pages` は公開ブランチであり、手動デプロイのみとする。

## 3. `git add .` の禁止

`git add .` および `git add -A` を禁止する。パスを明示する。

理由: 空フォルダの `.meta` が誤ってステージされる事故が2回発生している。
Git は空ディレクトリを追跡しないため、`.meta` のみがコミットされ、
別環境で GUID の不整合が発生する（`CodingSpec` 13節）。

## 4. 自律コミットとプッシュの基準

各ステップ（機能実装・テスト作成・アセット構成・ルール改訂）の完了時、以下の検証をすべて満たした時点で自律的に `git commit` および `git push origin main` を実行する。

1. **テスト検証**: Unity-MCP `run_tests`（EditMode）が 100% Green（失敗 0件）であること
2. **Living Spec 同期**: 変更された型・asmdef・ルールに対応する `docs/spec/` および `.agents/rules/` が同期更新されていること
3. **現在地更新**: `docs/STATUS.md` および `docs/log.md` が最新の状態に更新されていること

上記 3 点を満たさない状態でのコミット、または未検証のコードのプッシュを禁止する。

## 5. pull の実行

作業開始時と `push` の前に `git pull --rebase` を実行する。

理由: ドキュメントは GitHub の Web UI で編集されるため、
リモートが先行している状態が常態である。

## 6. コミットメッセージ

- 1行目は `<種別>: <要約>` の形式とする。種別は `docs` / `chore` / `feat` /
  `fix` / `test` から選ぶ
- 2行目を空け、3行目以降に変更内容を箇条書きで記述する
- 日本語で記述する