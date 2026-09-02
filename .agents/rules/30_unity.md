---
trigger: always_on
---

# Unity エディタ操作

## 1. 経路の限定

`Game/Assets/` 配下のアセットに対する以下の操作は、Unity-MCP 経由のみで行う。

- 移動
- リネーム
- 削除
- フォルダの作成

シェルコマンド（`mv` / `rm` / `cp` / `mkdir` / `New-Item` / `Move-Item` /
`Remove-Item`）による `Game/Assets/` 配下の操作を全面的に禁止する。

理由: Unity はすべてのアセットに GUID を記録した `.meta` ファイルを持ち、
プレハブやシーンはパスではなく GUID で参照している。
シェルで本体のみを移動すると `.meta` が取り残され、GUID 参照が広範囲に破損する。

## 2. `.meta` ファイル

`.meta` ファイルを直接作成・編集・削除してはならない。
Unity が生成するものであり、手で触る対象ではない。

## 3. 禁止事項

| # | 禁止事項 | 理由 |
|---|---|---|
| 1 | `.asset` の生成 | 人間が Unity エディタで行う |
| 2 | シーン・プレハブへの直接テキスト編集 | Unity-MCP 経由で行う |
| 3 | `Assets/Resources/` の使用 | `CodingSpec` 13節 |
| 4 | 空フォルダの作成 | `CodingSpec` 13節。Git が追跡しない |
| 5 | プロジェクト設定ファイルの変更 | ビルド設定は人間が行う（`BuildSpec`） |

## 4. コンパイル・テスト確認

`.cs` や `.asmdef` の変更後、Antigravity は Unity-MCP の `refresh_unity` を呼び出してエディタの同期を行う。
Auto Refresh は無効化されているため、手動・MCP 経由のリフレッシュが必須である。

コンパイル確認および Test Runner の実行は Unity-MCP の `run_tests` 経由で行い、得られた客観的な実行結果（Passed / Failed）に基づいて合否を判定する。推測や未確認での完了宣言を禁止する。