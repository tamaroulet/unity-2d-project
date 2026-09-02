# 調査レポート #1 追補：項目1の差し替え
 
追補実施：2026-09-01 16:16 UTC（JST 2026-09-02 01:16）／第2回実行
対象：`claude/Research-01.md` の「項目1：Unity Editor MCP の接続不全」
性質：**前回レポートの項目1・結論および切り分け手順を差し替えるものです。** 項目2・3・4 に変更はありません。
 
本追補を独立ファイルとした理由：指示書は「成果物はレポート1つのみ」としていますが、既存レポートは全文を再送信して置換する方式でしか更新できず、逐語引用（規約原文・公式マニュアル原文）を多数含むため、全文再生成は転記事故により原文引用を損なう危険があります。原本を無改変で保全することを優先しました。統合が必要な場合は本ファイルの内容で項目1を置き換えてください。
 
---
 
## 項目1：Unity Editor MCP の接続不全（差し替え版）
 
### 結論
 
断定不可です。ただし前回レポートが最有力候補とした「Antigravity 用設定のキーが `url` になっている」は、**configurator のソースコード確認により否定されました**。現行の unity-mcp は Antigravity 向けに `HttpUrlProperty = "serverUrl"` を書き出します。
 
現時点の最有力候補は、**Antigravity 向けクライアント登録が2種類存在し、書き出し先ディレクトリが異なること**です。`Antigravity 2.0` は `~/.gemini/config/mcp_config.json`、`Antigravity IDE` は `~/.gemini/antigravity-ide/mcp_config.json` を対象とします。誤った側を Configure すると、IDE が読む設定ファイルに unityMCP エントリが存在せず、接続に至りません。
 
公式 README のクライアント一覧に Antigravity は含まれません（本日再確認済み。公式ドキュメントサイト側には記載あり）。
 
### 根拠
 
#### (A) configurator ソースコードの直接確認
 
GitHub の tree ページおよびコード検索は robots.txt により、`api.github.com` は 403 により取得できませんでした。そのためファイル一覧の特定には jsDelivr のパッケージ API（GitHub のタグを参照する CDN）を用い、**ファイル本体は `raw.githubusercontent.com` から直接取得**しています。したがってソースコードの記述は一次情報です。
 
`MCPForUnity/Editor/Clients/Configurators/` 配下に、Antigravity 関連のファイルが **2つ** 存在します。
 
| ファイル | クライアント表示名 | 設定ファイルパス | HTTP URL のキー |
|---|---|---|---|
| `AntigravityConfigurator.cs` | `"Antigravity 2.0"` | `~/.gemini/config/mcp_config.json`（Windows / Mac / Linux 共通） | `"serverUrl"` |
| `AntigravityIdeConfigurator.cs` | `"Antigravity IDE"` | `~/.gemini/antigravity-ide/mcp_config.json`（Windows / Mac / Linux 共通） | `"serverUrl"` |
 
原文引用（`AntigravityConfigurator.cs`）:
 
```csharp
public class AntigravityConfigurator : JsonFileMcpConfigurator
```
 
```csharp
HttpUrlProperty = "serverUrl",
```
 
```csharp
Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "config", "mcp_config.json")
```
 
同ファイルの導入判定（`IsInstalled`）は `Path.Combine(home, ".gemini", "config")` および `Path.Combine(home, ".gemini", "antigravity")` の存在を見ています。
 
原文引用（`AntigravityIdeConfigurator.cs`）:
 
```csharp
public class AntigravityIdeConfigurator : JsonFileMcpConfigurator
```
 
```csharp
name = "Antigravity IDE",
```
 
```csharp
HttpUrlProperty = "serverUrl",
```
 
```csharp
Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity-ide", "mcp_config.json")
```
 
```csharp
Directory.Exists(Path.Combine(home, ".gemini", "antigravity-ide"));
```
 
この `IsInstalled` 判定は、取得結果によれば「Antigravity IDE has been installed and launched at least once」の判定シグナルとして記述されています。
 
`McpClientRegistry.cs` は個々の configurator を名前で列挙しておらず、`IMcpClientConfigurator` を実装する型を TypeCache により実行時に自動探索して登録します。したがって**上記2件は両方ともクライアント一覧に現れます**。
 
既存設定の読み取り側（`JsonFileMcpConfigurator`）は3キーを許容します。原文引用:
 
```csharp
var urlToken = unityObj["url"] ?? unityObj["serverUrl"] ?? unityObj["httpUrl"];
```
 
読み取りが3キー許容である一方、書き出しは `HttpUrlProperty = "serverUrl"` に固定されています。
 
#### (B) 前回レポートの記述に対する訂正
 
前回レポートは、公式 Docs の HTTP 設定サンプルが `"url"` キーであることと、Antigravity 公式が `url` を非対応と明記していることの不一致をもって、「設定キーの不一致が最有力候補」としていました。この推論は configurator の実装を確認しないまま行われたものであり、**実装は `serverUrl` を書いていたため、当該候補は成立しません**。
 
ただし以下は依然として有効です。
 
- Antigravity 公式ドキュメントの原文引用（本日再取得）:
> When declaring remote SSE, Streamable HTTP, or websocket-based MCP connections, you must define the `serverUrl` field.
 
> Legacy fields like `url` or `httpUrl` are not supported.
 
- したがって、設定ファイルを**手書きした場合**、または `url` キーを書く旧版・フォーク（前回レポートが補助情報として挙げた `choej2303/unity-mcp-ide` は `url` を使用）を用いた場合には、依然としてキー不一致が原因になり得ます。
#### (C) パスの食い違い（新たに判明した争点）
 
Antigravity 公式ドキュメントは設定ファイルの場所を次のように記載します（原文引用）:
 
> globally at `~/.gemini/config/mcp_config.json` (or locally in your workspace under `.agents/mcp_config.json`)
 
一方 unity-mcp は `AntigravityIdeConfigurator` において `~/.gemini/antigravity-ide/mcp_config.json` を対象としています。両者は食い違っており、**どちらを実際の Antigravity IDE が読むかは未確認**です。unity-mcp 側がこの configurator を別途用意している事実は、少なくとも「`~/.gemini/config/` を読まない Antigravity のビルドが存在する」ことを示唆しますが、これは実装の存在からの推論であり確認されたものではありません。
 
#### (D) バージョン
 
- GitHub リポジトリページおよび公式 Docs の Releases ページは、いずれも最新リリースを **v10.0.0（2026-06-30）** と表示します。
- jsDelivr のバージョン一覧には **10.1.0 / 10.1.2** が存在し、10.1.2 のファイル一覧を実際に取得できました。GitHub Release として公開されていないタグか、Releases 側の更新漏れかは未確認です。
- `AntigravityIdeConfigurator.cs` は **10.0.0 の時点で既に存在**します（同バージョンのファイル一覧で確認）。9.7.1 のファイル一覧は取得タイムアウトのため未確認です。
- 前回レポートが引用した v9.7.1（2026-05-24）の変更「fix(clients): point Antigravity at ~/.gemini/config/ after the 2.x migration」は、`AntigravityConfigurator`（= `Antigravity 2.0`）側の修正に対応します。
#### (E) 切り分け手順（差し替え）
 
前回レポートの手順表を、以下で置き換えます。上から順に実行してください。
 
| # | 確認内容 | 結果の意味 |
|---|---|---|
| 1 | Unity の `Window → MCP for Unity` を開き、クライアント一覧に `Antigravity 2.0` と `Antigravity IDE` の**両方**が表示されるか確認する | 両方あるなら、どちらを Configure したかが争点。片方しかないならパッケージが古い（10.0.0 未満の可能性）。バージョンを確認する |
| 2 | `%USERPROFILE%\.gemini\antigravity-ide\mcp_config.json` と `%USERPROFILE%\.gemini\config\mcp_config.json` の**両方**について、存否と unityMCP エントリの有無を確認する | エントリが片方にしかない場合、IDE が読んでいる側に無ければこれが原因。`~/.gemini/antigravity-ide/` ディレクトリの存在は、unity-mcp が「Antigravity IDE が起動済み」と判定する条件そのもの |
| 3 | ワークスペース側 `.agents/mcp_config.json` の有無と内容を確認する | Antigravity 公式が示すもう一つの読み取り先。ここに古い定義が残っていると、グローバル側を直しても反映されない |
| 4 | 該当ファイル内の unityMCP エントリのキーが `serverUrl` か確認する | 現行 configurator は `serverUrl` を書くため、`url` / `httpUrl` になっているのは手書き設定、旧版、またはフォーク使用の場合。Antigravity 公式が非対応と明記しているキーである |
| 5 | Unity Package Manager で MCP for Unity のバージョンを確認する | v9.7.1 未満なら `Antigravity 2.0` 側の書き出し先が旧パスである（v9.7.1 の修正内容）。10.0.0 未満なら `Antigravity IDE` エントリの有無を要確認 |
| 6 | Unity 側のトランスポート設定（HTTP / stdio）とステータスが `Connected` かを確認する | HTTP で待受していないのにクライアント側が HTTP 設定になっている、またはその逆であれば設定不整合 |
| 7 | `mcpforunity://instances` を叩ける状態か、ステータスパネルにインスタンスが表示されるかを確認する | 表示されなければ Unity 側 Bridge が待受していない。接続経路以前の問題 |
| 8 | `netstat -ano \| findstr :8080` でポート 8080 の占有を確認する | 別プロセスが占有していれば競合。Wiki は Tailscale 等との競合を名指ししている。Unity 側でポートを変更する |
| 9 | ポート変更後、Windows Defender ファイアウォールの受信規則を確認する | 規則がなければブロックされている可能性がある。Wiki の `New-NetFirewallRule` 例に従う |
| 10 | Antigravity の `/mcp`（MCP Manager）でステータス表示と接続ログを確認し、手動リロードを実行する | 公式が「active, disconnected, or loading」の表示とログ閲覧、手動リロードを提供すると記載している。ここでのログが一次証拠になる |
| 11 | Antigravity 側で MCP のトグル／プラグインが有効か確認する | 公式が「Antigravity は auto-configuration 後に自分の設定側で有効化が必要」と明記している |
| 12 | 解消しない場合、トランスポートを stdio に切り替えて再試行する | stdio で通れば HTTP／SSE 経路の問題（Issue #430 と同型）。stdio でも通らなければ uv のパス解決を疑う |
| 13 | `where uv` で `uv.exe` が複数存在しないか確認し、`%LOCALAPPDATA%\Microsoft\WinGet\Links\uv.exe` に固定する | 複数存在は公式 Troubleshooting が名指しする Windows の失敗要因 |
| 14 | それでも解消しない場合、Issue #430 に環境を添えて追記するか新規 Issue を起票する | #430 は Closed だが原因・修正の記載がなく、公開情報からは追跡できない |
 
### 未確認の残件
 
- **Antigravity IDE 本体が実際に読むパス。** 公式ドキュメントは `~/.gemini/config/mcp_config.json` と `.agents/mcp_config.json` のみを記載し、`~/.gemini/antigravity-ide/` に言及しません。unity-mcp が当該パス用の configurator を持つ理由（IDE のバージョン差か、別製品の区別か）は特定できていません。
- `AntigravityIdeConfigurator` が追加されたバージョン。10.0.0 に存在することは確認済み、9.7.1 は取得タイムアウトのため未確認です。
- GitHub Releases の最新表示（v10.0.0 / 2026-06-30）と jsDelivr のタグ一覧（10.1.2 まで存在）の食い違い。どちらが正規の最新リリースかは未確認です。
- `Antigravity 2.0` という表示名が指す製品と、ユーザーが使用している「Antigravity IDE」の対応関係。名称からの推定は行っていません。
- Issue #430 のクローズ理由、修正コミット、修正バージョン（前回レポートから継続。Issue ページのコメント本文が取得結果に現れず、GitHub の Issue 検索は robots.txt により取得不可）。
- Issue #691 / #773 / #156 のメンテナ回答および修正内容（継続）。
- Discussions の全体一覧（継続）。
- Unity パッケージと Python サーバの内部ディスカバリ機構の仕様（継続）。
- Unity 6.3（6000.3.23f1）を名指しした接続非互換の記述（継続。該当なし）。
- ソースコードの取得は要約モデルを経由するため、引用は行単位では正確であっても、ファイル全体の文脈を検証したものではありません。判断に用いる場合は該当ファイルの直接確認を推奨します。
### 出典
 
一次情報（公式リポジトリのソースコード。`raw.githubusercontent.com` は GitHub の生ファイル配信）
 
- https://raw.githubusercontent.com/CoplayDev/unity-mcp/main/MCPForUnity/Editor/Clients/Configurators/AntigravityConfigurator.cs （main ブランチ／ページ内日付表示なし。2026-09-01 取得）
- https://raw.githubusercontent.com/CoplayDev/unity-mcp/main/MCPForUnity/Editor/Clients/Configurators/AntigravityIdeConfigurator.cs （同上）
- https://raw.githubusercontent.com/CoplayDev/unity-mcp/main/MCPForUnity/Editor/Clients/McpClientRegistry.cs （同上）
- https://raw.githubusercontent.com/CoplayDev/unity-mcp/main/MCPForUnity/Editor/Clients/McpClientConfiguratorBase.cs （同上）
- https://github.com/CoplayDev/unity-mcp （最新リリース v10.0.0 / 2026-06-30 と表示。README に "Antigravity" の語なしを本日再確認）
- https://coplaydev.github.io/unity-mcp/releases （v9.7.1 / 2026-05-24、最新 v10.0.0 / 2026-06-30）
- https://antigravity.google/docs/ide/mcp （ページ内日付表示なし。2026-09-01 取得）
ファイル一覧の特定に用いた二次配信（内容は当該リポジトリのタグ付きファイルそのもの）
 
- https://data.jsdelivr.com/v1/packages/gh/CoplayDev/unity-mcp （タグ一覧。10.1.2 が最新として表示）
- https://data.jsdelivr.com/v1/packages/gh/CoplayDev/unity-mcp@10.1.2?structure=flat
- https://data.jsdelivr.com/v1/packages/gh/CoplayDev/unity-mcp@10.0.0?structure=flat
- https://data.jsdelivr.com/v1/packages/gh/CoplayDev/unity-mcp@10.1.0?structure=flat
 