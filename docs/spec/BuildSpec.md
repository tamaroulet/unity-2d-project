# ビルド・環境規約

本ドキュメントは、Unity プロジェクトのビルド設定および環境構成を定義する。
記載された設定は人間が Unity エディタ上で行う。AIコーディングエージェントは
これらの設定を変更しない。

対象: Unity 6.3 LTS (6000.3.23f1) / 2D / URP / Web ビルドターゲット

---

## 1. 公開先

| 項目 | 内容 |
|---|---|
| 公開先 | GitHub Pages（単独） |
| 名義 | 実名（tamaroulet） |
| unityroom | 不採用 |

理由: 本プロジェクトの第一目的は AI 委譲の工程記録である。工程記録（Issue / PR /
差分 / 作業ログ）と成果物が同一リポジトリに紐づく GitHub Pages が最適である。

---

## 2. ビルド設定

| 項目 | 値 |
|---|---|
| Compression Format | **Brotli** |
| Decompression Fallback | **有効** |
| WebAssembly 2023 機能セット | 有効 |
| Managed Stripping Level | **Minimal** |
| Reference Resolution | 1920 × 1080 |
| ビルド時 Canvas サイズ | 1280 × 720 |

理由（Decompression Fallback 有効）: GitHub Pages は静的ホスティングであり、
`Content-Encoding` ヘッダーを設定できない。圧縮ビルドをそのまま配信すると、
ブラウザが圧縮バイナリを非圧縮コードとして解釈し、ロードが停止する
（unrecognized magic number）。

副作用として WebAssembly ストリーミングが無効になり、起動時間が増加する。
ただし本プロジェクトは 3D モデルを含まないためコードサイズが小さく、
圧縮によるアセット転送時間の短縮が上回る。

理由（Stripping Minimal）: High にするとリフレクション依存の型が消失し、
実行時に例外でクラッシュする。Minimal であればこの問題自体が発生しない。

---

## 3. URP 設定

以下を無効化する。

| 項目 |
|---|
| Reflection Probes |
| Global Illumination |
| Depth Texture |

Shader Stripping（シェーダーバリアントの除去）を有効にする。

理由: Unity 6 は空のシーンでもビルドに 40 分以上、出力 40MB 超という報告がある。
2D の UI 主体のゲームでは 3D 用シェーダーバリアントが不要である。

---

## 4. GitHub Pages の設定

| 項目 | 内容 |
|---|---|
| `.nojekyll` | 公開ディレクトリのルートに空ファイルを配置 |
| ビルド名 | プロジェクト名と一致させる |
| デプロイ方法 | ローカルビルド後、`gh-pages` ブランチへ手動プッシュ |

理由（`.nojekyll`）: GitHub Pages は内部で Jekyll を実行し、アンダースコア始まりの
ファイルを除外する。`loader.js` が 404 になる事例の要因として報告されている。

理由（手動デプロイ）: GitHub Actions による自動デプロイは、Personal ライセンスの
手動アクティベーションで公式ポータルの UI が隠蔽されているという報告があり、
DOM 操作による回避が必要とされる。型フェーズで着手するにはリスクとコストが
見合わない。

---

## 5. GitHub Pages の制限

| 項目 | 値 |
|---|---|
| 1ファイル上限 | 100MB（50MB で警告） |
| リポジトリ容量 | 1GB（ソフトリミット） |
| 帯域 | 月 100GB |
| MIME type | `.wasm` は標準対応。追加設定不要 |

---

## 6. カスタム Web テンプレート

`Assets/WebGLTemplates/` にカスタムテンプレートを作成し、高DPI対応を行う。

`index.html` 内の JavaScript を修正し、Canvas の物理ピクセルサイズを
`window.devicePixelRatio` 倍に拡大した上で、CSS の論理ピクセルサイズを
元のサイズに固定する。

理由: 高DPI環境（Retina、Windows のスケーリング 150% 等）では、CSS ピクセルと
物理ピクセルの乖離により、UI とテキストが全体的にぼやける。

---

## 7. 導入するパッケージ

| パッケージ | 用途 |
|---|---|
| PrimeTween | トゥイーン |
| UniTask | 非同期処理 |
| Unity-MCP (CoplayDev) | Unity エディタ操作（導入済み） |

DOTween は導入しない。

---

## 8. フォント

| 項目 | 内容 |
|---|---|
| フォント | Noto Sans JP 等をプロジェクトに同梱 |
| TMP フォントアセット | 必要な文字を事前に含めた SDF アセットを生成 |

システムフォントへのフォールバックは Web で機能しないため使用しない。

---

## 9. `record` 型のための追加ファイル

`Game/Assets/Core/` 配下に以下を配置する。

    namespace System.Runtime.CompilerServices
    {
        public static class IsExternalInit { }
    }

理由: `record` と `init` セッターには `IsExternalInit` 型が必要だが、
これは .NET 5 以降にしか存在せず Unity は未対応である。自前で宣言することで
回避できる（Unity 公式マニュアルに記載）。

`public` とするのは、`Game.Core` 以外のアセンブリで `record` を定義する際に
必要なためである。`internal` にすると宣言したアセンブリ内でしか見えず、
`Game.Features.*` や `Game.Tests.*` で `init` セッターがコンパイルできない
（CS0518）。

---

## 10. 第1週の環境検証チェックリスト

| # | 作業 | 状態 |
|---|---|---|
| 1 | `IsExternalInit` の配置と `record` のコンパイル確認 | |
| 2 | WebAssembly 2023 の有効化 | |
| 3 | URP Asset の不要機能を無効化 | |
| 4 | Compression Format = Brotli、Decompression Fallback = 有効 | |
| 5 | 空プロジェクトを GitHub Pages に公開し、起動時間を実測 | |
| 6 | `.nojekyll` の配置 | |
| 7 | カスタム Web テンプレート作成（高DPI対応） | |
| 8 | PrimeTween / UniTask の導入 | |
| 9 | Noto Sans JP の同梱と TMP フォントアセット生成 | |
| 10 | asmdef の作成（Core / Features / UI） | |

2〜4はビルド設定のため、5の検証時にまとめて実施すると効率的である。

---

## 11. 未確認事項

| 項目 | 内容 |
|---|---|
| Decompression Fallback の起動時間増加幅 | Unity 6.3 環境での定量的なベンチマークが存在しない。第1週の実測で確認する |
| JetBrains 学生ライセンスの商用利用範囲 | 本番プロジェクトの前提に影響する。規約原文の確認が必要 |
