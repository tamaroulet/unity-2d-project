# 調査レポート #1：第2週着手前の未確認事項
 
調査実施日：2026-09-01
調査手段：Web 検索および Web ページ取得のみ。ローカルリポジトリへのアクセスは行っていません。
本レポートは全4項目に着手し、いずれも完結しています。
 
---
 
## 項目1：Unity Editor MCP の接続不全
 
### 結論
 
断定不可です。ただし最有力の候補は、Antigravity の MCP 設定における HTTP 接続キーの不一致です。unity-mcp 公式ドキュメントが示す HTTP 設定サンプルは `"url"` キーを使用していますが、Antigravity 公式ドキュメントは `serverUrl` を必須とし `url` を明確に非対応と記載しています。
同一構成（Windows / HTTP / Antigravity）での既知報告は Issue #430 として存在しますが、確定原因および修正内容はページ上に記載がなく未確認です。
公式 README のクライアント一覧に Antigravity は含まれません（公式ドキュメントサイト側には記載あり）。
 
### 根拠
 
#### (1) 公式に列挙されているクライアント一覧
 
README（`https://github.com/CoplayDev/unity-mcp`）本文で名指しされているクライアントは以下です。
 
> Claude Desktop & Code, Cursor, VS Code, Windsurf, Cline, Gemini CLI
 
README の全文を取得して確認した結果、**"Antigravity" の語は README に一度も現れません**。
 
一方、公式ドキュメントサイトの Install ページには Antigravity が 2 箇所に登場します。原文引用は以下です。
 
> **Cursor, Antigravity, OpenClaw** still require enabling an MCP toggle or plugin in their own settings after auto-configuration.
 
> ### HTTP (default — Cursor, Windsurf, Antigravity, VS Code, Cline, etc.)
 
同ページの前提条件（Prerequisites）に列挙された MCP クライアントは以下であり、ここにも Antigravity は含まれません。
 
> **An MCP client** — Claude Desktop, Claude Code, Cursor, VS Code Copilot, GitHub Copilot CLI, Windsurf, Cline, OpenClaw, and more
 
また Getting Started 概要ページの自動設定対象クライアント列挙（Claude Desktop, Claude Code, Cursor, VS Code, Windsurf, Cline, Codex, Qwen, Gemini CLI, Copilot CLI, OpenClaw）にも Antigravity は含まれません。
「サポート対象」と「動作確認済み（tested）」を区別する明示的な文言は、いずれのページにも存在しません（未確認）。
 
#### (2) 設定キーの不一致（一次情報どうしの突き合わせ）
 
unity-mcp 公式 Install ページの HTTP 設定サンプル（原文引用）:
 
```json
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```
 
Antigravity 公式 MCP ドキュメントの記述（原文引用）:
 
> When declaring remote SSE, Streamable HTTP, or websocket-based MCP connections, you must define the `serverUrl` field. Legacy fields like `url` or `httpUrl` are not supported.
 
Antigravity の設定ファイルパス（原文引用）:
 
- Global: `~/.gemini/config/mcp_config.json`
- Workspace-local: `.agents/mcp_config.json`
トップレベルキーは `mcpServers` で Claude Desktop と共通です。stdio 系の `command` / `args` / `env` も共通です。相違はリモート接続時の URL キーのみです。
 
以上は公式2文書の記述内容の差異として提示するものであり、「これが本件の原因である」と確認できたわけではありません。
 
#### (3) 同一症状の既知報告
 
| 項目 | 内容 |
|---|---|
| 番号 | #430 |
| タイトル（原文） | `[ANTIGRAVITY BUG] Persistent "Context Canceled" / Client Disconnect despite Healthy Server Logs` |
| 起票日 | 2025-12-05 |
| 状態 | Closed |
| 環境 | Windows / mcp-for-unity-server (FastMCP 2.13.1) / HTTP `localhost:8080/mcp` / Google Deepmind Antigravity Agent |
| エラー（原文） | `connection closed: calling "resources/list": client is closing: standalone SSE stream: failed to reconnect (session ID: <SESSION_ID>): connection failed after 5 attempts: Get "http://localhost:8080/mcp": context canceled` |
| サーバ側ログ | 初回リクエストは 200 OK / 202 Accepted で成功 |
| 確定原因 | ページ上に記載なし（**未確認**） |
| 解決内容 | ページ上に記載なし（**未確認**） |
 
本件の前提「サーバープロセス単体は手動実行で正常に起動する」と、#430 の「サーバログは健全だがクライアントが切断する」は一致します。
 
その他の切断関連 Issue は以下です。
 
- #691 `Unity MCP server disconnects frequently`（2026-02-06、Closed）。Windows / VS Code の Claude Code 拡張 / v9.0.7。ログ上のエラーは `OSError: [WinError 64] The specified network name is no longer available.`（`localhost:8082` の accept 中）。解決記述なし（未確認）。
- #773 `[Bug] Generated Claude Desktop config doesn't work (v9.4.6, Windows, Stdio)`（2026-02-18、Closed）。Unity のエディタウィンドウが生成した JSON では動作せず、クライアント UI から `uv --directory <extension-path> run windows-mcp` を手動登録すると動作したとの報告。確定原因の記載なし（未確認）。
- #156 `unityMCP failed - Feature [Claude Code Integration]`（2025-07-09、Closed）。Mac / Claude Desktop で "server disconnected"。原因・解決の記載なし（未確認）。
Discussions 一覧および GitHub の Issue 検索 URL は robots.txt により取得できず、網羅的な検索は実施できていません（未確認）。
 
#### (4) 観点別に文書化されている原因
 
**Python 環境（uv / uvx / パス解決）**
 
- 前提要件は「Unity 2021.3 LTS or newer」「Python 3.10+ with `uv`」です。
- 公式 Troubleshooting に、Windows で `uv.exe` が複数箇所に存在すると起動に失敗しうること、WinGet Links shim のパス `%LOCALAPPDATA%\Microsoft\WinGet\Links\uv.exe` に固定すると安定する旨の記載があります。
- リリース履歴に「fix: resolve UV path override not being detected in System Requirements」（v9.0.8 / 2026-01-19）、「Add --offline to uvx launches for faster startup」（v9.4.7 / 2026-02-21）があります。
- Windows 固有のクォート規則に関する記述は未確認です。
**設定ファイルの記法**
 
- 上記 (2) のとおりです。Claude Desktop は HTTP 非対応であり常に stdio である旨が Transports ページに記載されています。
- 絶対パスの要否について、macOS では Finder 起動アプリがシェルの PATH を継承しないため絶対パス指定が必要との記載が Wiki にあります。Windows についての同種の記述は未確認です。
**Unity 側 Bridge の状態**
 
- `Window → MCP for Unity` からサーバの起動/停止、トランスポート切替（HTTP / stdio）、クライアント再設定が可能である旨が記載されています（原文引用）。
> Window → MCP for Unity to start/stop the server, switch transport (HTTP vs stdio), or reconfigure clients.
 
- README には `Window → MCP for Unity → Configure All Detected Clients` の記載があります。
- Troubleshooting の "No Unity Instances Found" 項に、MCP ステータスパネルが `Connected` かを確認すること、`mcpforunity://instances` でテスト可能であること、迷ったらクライアントを再起動することが記載されています。
- デフォルトポートは **8080**、HTTP エンドポイントは `http://localhost:8080/mcp` です。
- Unity パッケージと Python サーバの内部的な相互発見機構の仕様は、公式ドキュメント上に記述が見当たらず未確認です。
**ポート競合およびファイアウォール**
 
- Wiki の WSL2 手順内に「change port to 8090 (default 8080 conflicts with services like Tailscale)」との記載があり、**デフォルトの 8080 が他サービスと競合しうる**ことが明記されています。
- 同手順にファイアウォール規則の例 `New-NetFirewallRule -DisplayName "Unity MCP Server" -Direction Inbound -LocalPort 8090 -Protocol TCP -Action Allow` があります。
- リリース履歴に「harden localhost resolution and reload transport resilience on Windows」（v9.4.6 / 2026-02-15）、「fix(stdio): retry same port on bind race instead of silent fallback」があります。
**Unity 6.3 / 6000.3.x 固有の非互換**
 
- サポート範囲は README で「Unity 2021.3 LTS through 6.x」、Install ページで「Unity 2021.3 LTS or newer」と記載されており、**6000.3.23f1 は範囲内**です。6.3 系を除外する記述はありません。
- 6.3 固有の既知問題として、Unity AI Assistant パッケージ併用時に `System.Collections.Immutable` のバージョン不整合でコンパイルエラーが発生する旨が Wiki と Troubleshooting に記載されています。注記は原文で「This is a Unity assembly resolution issue, not specific to MCP for Unity.」です。ただし**これはコンパイルエラーであり、接続断の原因として文書化されているものではありません**。
**クライアント実装差**
 
- 原文引用：「Claude Code, VS Code, Windsurf, Cline, and the CLI clients auto-connect after configuration.」
- 原文引用：「**Cursor, Antigravity, OpenClaw** still require enabling an MCP toggle or plugin in their own settings after auto-configuration.」
- リリース v9.7.1（2026-05-24）に「fix(clients): point Antigravity at ~/.gemini/config/ after the 2.x migration」があります。すなわち **v9.7.1 未満では、自動設定機能が Antigravity 用の設定を旧パスへ書き出していました**。最新版は v10.0.0（2026-06-30）です。
#### (5) 切り分け手順
 
上から順に実行してください。各手順は「確認内容」と「結果の意味」で構成します。
 
| # | 確認内容 | 結果の意味 |
|---|---|---|
| 1 | Unity Package Manager で MCP for Unity のバージョンを確認する | v9.7.1 未満であれば、自動設定が Antigravity の旧設定パスへ書き出している可能性がある（v9.7.1 の修正内容）。v9.7.1 以降なら手順2へ |
| 2 | `~/.gemini/config/mcp_config.json`（Windows では `%USERPROFILE%\.gemini\config\mcp_config.json`）が存在するか、および `.agents/mcp_config.json` の有無を確認する | どちらも存在しなければ Antigravity は設定自体を読んでいない。自動設定の書き出し先が誤っている |
| 3 | 上記ファイル内の unityMCP エントリのキーを確認する | HTTP 接続で `"url"` になっていれば、Antigravity 公式が非対応と明記しているキーである。`"serverUrl"` でなければならない。これが最有力の候補 |
| 4 | Unity の `Window → MCP for Unity` を開き、サーバのトランスポート設定（HTTP / stdio）とステータスが `Connected` かを確認する | HTTP で待受していないのにクライアント側が HTTP 設定になっている、またはその逆であれば設定不整合 |
| 5 | Unity 側で `mcpforunity://instances` を叩ける状態か、ステータスパネルにインスタンスが表示されるかを確認する | 表示されなければ Unity 側 Bridge が待受していない。サーバ／クライアントの経路以前の問題 |
| 6 | ポート 8080 を他プロセスが使用していないか確認する（`netstat -ano \| findstr :8080`） | 別プロセスが占有していれば、Wiki が名指しする Tailscale 等との競合パターン。Unity 側でポートを変更する |
| 7 | ポート変更後、Windows Defender ファイアウォールで当該ポートの受信規則を確認する | 規則がなければブロックされている可能性がある。Wiki の `New-NetFirewallRule` 例に従う |
| 8 | Antigravity の `/mcp` で対話型 MCP Manager を開き、live status ring と接続ログを確認する。手動リロードを実行する | Antigravity 公式が「active, disconnected, or loading」のステータス表示とログ閲覧、手動リロードを提供すると記載している。ここでのログが一次証拠になる |
| 9 | Antigravity の設定で MCP トグル／プラグインが有効になっているか確認する | 公式が「Antigravity は auto-configuration 後に自分の設定側で有効化が必要」と明記している。無効なままなら接続されない |
| 10 | 手順3〜9で解消しない場合、トランスポートを stdio に切り替えて再試行する | stdio で通れば HTTP／SSE 経路の問題（Issue #430 と同型）。stdio でも通らなければ uv のパス解決を疑う（手順11へ） |
| 11 | `where uv` で `uv.exe` が複数箇所に存在しないか確認し、`%LOCALAPPDATA%\Microsoft\WinGet\Links\uv.exe` に固定する | 複数存在は公式 Troubleshooting が名指しする Windows の失敗要因 |
| 12 | それでも解消しない場合、Issue #430 に環境を添えて追記するか新規 Issue を起票する | #430 は Closed だが原因・修正の記載がなく、公開情報からは追跡できない |
 
### 未確認の残件
 
- Issue #430 のクローズ理由、修正コミット、修正バージョン。Issue ページを取得したがコメント本文が取得結果に現れず、GitHub の Issue 検索 URL は robots.txt により取得不可でした。
- Issue #691 / #773 / #156 のメンテナ回答および修正内容。
- Discussions の全体一覧および Discussion #174 の本文。
- unity-mcp の Antigravity 用 configurator が実際に書き出すキーが `url` か `serverUrl` か。ソースコードに到達できませんでした。公式 Docs のサンプルが `url` であることのみ確認しています。**手順3が最有力候補である根拠は「公式2文書の記述不一致」であり、configurator の実装を確認したものではありません。**
- Unity パッケージと Python サーバの内部ディスカバリ機構の仕様。
- Unity 6.3（6000.3.23f1）を名指しした接続非互換の記述。README / Install / Releases / Wiki / Troubleshooting を検索しましたが、6.3 系の記述は `System.Collections.Immutable` 衝突のみでした。
- Antigravity 公式 MCP ドキュメントのページ日付（ページ上に日付表示なし）。
- 公式 UI 上でのポート変更手順（Wiki の WSL2 手順に「change port to 8090」とあるのみ）。
- Antigravity 側に「接続失敗 / disconnected」専用のトラブルシューティング節は存在しません。
### 出典
 
一次情報（公式リポジトリ・公式ドキュメント）
 
- https://github.com/CoplayDev/unity-mcp （最新リリース v10.0.0 / 2026-06-30 と表示）
- https://raw.githubusercontent.com/CoplayDev/unity-mcp/main/README.md （日付表示なし）
- https://coplaydev.github.io/unity-mcp/getting-started/install （日付表示なし）
- https://coplaydev.github.io/unity-mcp/getting-started （日付表示なし）
- https://coplaydev.github.io/unity-mcp/architecture/transports （日付表示なし）
- https://coplaydev.github.io/unity-mcp/guides/troubleshooting （日付表示なし）
- https://coplaydev.github.io/unity-mcp/releases （最新 v10.0.0 / 2026-06-30、v9.7.1 / 2026-05-24、v9.4.7 / 2026-02-21、v9.4.6 / 2026-02-15、v9.0.8 / 2026-01-19）
- https://github.com/CoplayDev/unity-mcp/wiki/3.-Common-Setup-Problems （日付表示なし）
- https://github.com/CoplayDev/unity-mcp/issues/430 （2025-12-05）
- https://github.com/CoplayDev/unity-mcp/issues/691 （2026-02-06）
- https://github.com/CoplayDev/unity-mcp/issues/773 （2026-02-18）
- https://github.com/CoplayDev/unity-mcp/issues/156 （2025-07-09）
- https://antigravity.google/docs/ide/mcp （日付表示なし）
- https://antigravity.google/docs/mcp/ （日付表示なし）
補助情報（一次情報が見つからない箇所の参考。判定根拠には用いていません）
 
- https://developer.android.com/games/engines/unity/unity-mcp-antigravity （Last updated 2026-07-21 UTC）。ただしこれは Unity 純正の "Unity MCP Server"（Unity AI Assistant パッケージ）の手順であり、CoplayDev/unity-mcp のものではありません。
- https://github.com/choej2303/unity-mcp-ide （Antigravity 対応を謳うフォーク。設定形式は `url` を使用。最終コミット日は未確認）
- https://github.com/gajzzs/unity-mcp-antigravity-fix （フォーク。README にフォーク固有の変更点の説明は見つからず）
---
 
## 項目2：ジェネリック ScriptableObject の IL2CPP 制約
 
### 結論
 
**条件付きで安全**です。`GameStateEventChannelSO : EventChannelSO<GameState>` は閉じたジェネリックインスタンス化がコンパイル時に確定し、Unity 6.3 の IL2CPP 制限ページが列挙する「コンパイル時に決定できない5ケース」のいずれにも該当しません。
`GameState` が `record class`（参照型）であるため、参照型のコード共有が適用されます。Managed Stripping Level = Minimal はユーザ作成コードを一切除去しません。
条件は「型引数に値型を使う派生を作らないこと」「リフレクションによる動的生成を行わないこと」「Web ビルドで実機確認を行うこと」です。
 
### 根拠
 
#### (1) ジェネリック型・メソッドの AOT 制約（公式）
 
Unity 6.3 LTS（6000.3）マニュアル「IL2CPP limitations」（ページ表示：Built on 2026-08-26）の原文引用です。
 
> For generic types and methods, the compiler must determine which generic instances are used because different generic instances might require different code.
 
> However, IL2CPP shares code for usages of reference types, so the same code is used for `List<object>` and `List<string>`.
 
コンパイル時にジェネリックインスタンスを決定できないケースとして、同ページは以下を列挙しています。
 
> - Creating a new generic instance at runtime: `Activator.CreateInstance(typeof(SomeGenericType<>).MakeGenericType(someType));`
> - Invoking a static method on a generic instance: `typeof(SomeGenericType<>).MakeGenericType(someType).GetMethod("AMethod").Invoke(null, null);`
> - Invoking a static generic method: `typeof(SomeType).GetMethod("GenericMethod").MakeGenericMethod(someType).Invoke(null, null);`
> - Some calls to generic virtual functions that can't be inferred at compile time.
> - Calls with deeply nested generic value types, such as `Struct<Struct<Struct<...<Struct<int>>>>`.
 
該当した場合の挙動として、フォールバックコードが生成されるが「this code is slower because it can't determine a type's size, or whether it's a reference or value type」と記載されています。
 
制約の推奨（原文引用）:
 
> If the generic argument will always be a reference type, add the `where: class` constraint.
 
> If the generic argument will always be a value type, add the `where: struct` constraint.
 
同ページは `where: class` を「using reference type sharing, which causes no performance degradation」と説明しています。
 
**本件への適用**：`GameStateEventChannelSO : EventChannelSO<GameState>` は非ジェネリックの具体型サブクラスであり、`EventChannelSO<GameState>` という閉じたインスタンス化がコンパイル時に静的に確定します。上記5ケースのいずれにも該当しません。
 
#### (2) 値型と参照型の差
 
Unity 公式マニュアルは**参照型側のみ明示**しています（上記の "IL2CPP shares code for usages of reference types"）。「値型は共有できず完全特殊化が必要」という直接の文はマニュアル上に見当たりません（未確認）。間接的な記述は "different generic instances might require different code" と "the value types can be different sizes" です。
 
Unity スタッフによる明示的発言（公式ドキュメントではなくフォーラム）。JoshPeterson（Unity Technologies）、2020-03-02（原文引用）:
 
> The restrictions for generic virtual methods occur when the generic arguments are value types (e.g `int`,`double`, etc.). Any generic virtual methods with reference type generic arguments will work fine.
 
同氏は別スレッド（2020-05-08 / 05-11）で留保も付けています（原文引用）:
 
> We've made some progress on improving the support for generic virtual methods in IL2CPP. However, there are still case where they won't work.
 
> Unfortunately, we cannot statically determine this by looking at the code. You will need to test the code at run time to be sure.
 
「参照型は 1 word で同一サイズのため単一実装を共有できるが、値型は実行時までサイズが不明」という一般則の出典は Mono Project のドキュメントであり、**Unity 公式マニュアルの記述ではありません**。
 
Unity 公式ブログ（Josh Peterson、2021-12-15）は、従来の generic sharing が "in cases where T is a reference type (string, object, etc.)" に限定されていたこと、コンパイル時に対象メソッドを決定できない場合のエラーが `Attempting to call method...for which no ahead of time (AOT) code was generated.` であることを述べています。
 
#### (3) 実際に報告されている失敗事例
 
| # | 出典 | 日付 | トリガー | 分類 |
|---|---|---|---|---|
| A | Unity Discussions "Runtime AOT compilation errors of WebGL release build" | 2022-03-29 | Zenject DI の `PlaceholderFactory` をリフレクション経由で生成。WebGL Release。`link.xml` に `preserve="all"` を追加して解決 | リフレクションによる実行時インスタンス化 |
| B | Unity Discussions "No ahead of time (AOT) code was generated" | 2021-03-06 (Unity 2020.2) | `dictionary.Values.AsParallel().ForAll(...)`。PLINQ ＋ 値型引数 `System.Int32`。Unity 2020.3.7 で修正との staff 回答 | 値型引数＋実行時解決 |
| C | Unity Discussions "ExecutionEngineException using IL2CPP" | 2015-11-24 (Unity 5.1〜5.3 RC) | enum（値型）配列に対する `ICollection<T>.CopyTo` のジェネリック仮想呼び出し。Unity 5.3.2 で解消 | 値型引数のジェネリック仮想メソッド |
 
エラー文の原文引用（事例B）:
 
> ExecutionEngineException: Attempting to call method 'System.Linq.Parallel.ForAllOperator`1<...>::WrapPartitionedStream<System.Int32>' for which no ahead of time (AOT) code was generated.
 
**検索した範囲では、「ジェネリック基底クラスを具体的な参照型で継承した非ジェネリックサブクラス」というパターン自体が AOT 失敗を起こしたという報告は 1 件も見つかりませんでした。** 見つかった失敗はすべて、リフレクション／`MakeGenericType`／DI コンテナによる実行時インスタンス化、または値型引数が絡むものです。ただし「報告が見つからない」ことは「起きない証明」ではなく、この点は未確認として扱います。
 
#### (4) `System.Action<T>` をイベントとして持つ構成
 
`System.Action<T>` を ScriptableObject の event フィールドに置くことに起因する IL2CPP 固有の問題を記述した公式ドキュメント・信頼できる報告は発見できませんでした（未確認）。IL2CPP limitations ページの制限項目は Threads / Reflection / Exception Filters / Serialization / Generic Types and Methods / Calling Managed Methods from Native Code / Unsupported .NET APIs であり、delegate・event に関する項目は存在しません。
 
`Action<GameState>` は `GameState` が参照型のため、上記 (1) の参照型コード共有が適用されるインスタンス化です。
 
シリアライズとの相互作用について、Unity 6.3 の「Serialization rules」ページがシリアライズ可能とする型は、プリミティブ、enum、fixed-size buffer、Unity 組込み型、`[Serializable]` 付き構造体、`UnityEngine.Object` 派生への参照、`[Serializable]` 付きクラス、およびそれらの配列／`List<T>` です。**delegate / event 型はこのリストに含まれません**。したがって `public event System.Action<T> OnRaised;` は Unity のシリアライザの対象外であり、シリアライズ経路で AOT 問題を引き起こす余地はありません。ただし「event はシリアライズされない」という明文の記述は 6.3 の当該ページには見当たらず、リストからの帰結です。
 
なお `[Serializable]` について同ページは「is not automatically inherited by subclasses and must be applied to all classes in a class hierarchy.」と記載しています。`EventChannelSO<T>` に `T` 型の `[SerializeField]` を置く場合はこれが関係しますが、提示されたコードには `[SerializeField]` がないため現状は無関係です。
 
#### (5) Managed Stripping Level = Minimal
 
Unity 6.3 LTS（6000.3）「Configure managed code stripping」（ページ表示：Built on 2026-08-26）の原文引用です。
 
> **Disabled:** Unity doesn't remove any code. This setting is only available for the Mono scripting backend and is the default setting in that case.
 
> **Minimal:** Unity searches only the `UnityEngine` and the .NET class libraries for unused code. Unity doesn't remove any user-written code. This setting is the least likely to cause unexpected runtime behavior.
 
> **Low:** Unity searches for unused code in all `UnityEngine` and .NET class libraries. It also searches user-written assemblies, but only if none of their types are referenced in scenes included in the Player build.
 
> **Medium:** Unity partially searches all assemblies to find unused code. This setting applies a set of rules that strips more types of code patterns to reduce the build size.
 
> **High:** Unity performs an extensive search of all assemblies to find unused code. At this setting, Unity prioritizes size reduction more than code stability and removes as much code as possible.
 
**Minimal では「Unity doesn't remove any user-written code」と明記されており**、`EventChannelSO<T>` / `GameStateEventChannelSO` / `GameState` / `Raise` / `OnRaised` はすべてユーザアセンブリのコードであるため、stripping の対象外です。
 
`link.xml` が必要になる条件について、「Preserving code」ページ（Built on 2026-07-23）の原文引用です。
 
> Use the `[Preserve]` attribute when you want to preserve both a type and its default constructor. If you want to keep one or the other but not both, use a `link.xml` file.
 
> You can include a .xml file named `link.xml` in your project to preserve a list of specific assemblies or parts of assemblies.
 
「How code stripping affects content」ページ（Built on 2026-06-05）には次の記載があります。
 
> AssetBundle or Addressables content-only build can't use types that have been stripped out of a Player build.
 
したがって `link.xml` が必要になるのは、(a) stripping レベルが Low 以上でユーザコードが探索対象になる場合、(b) リフレクション経由でしか参照されない型がある場合、(c) AssetBundle / Addressables のコンテンツのみのビルドで、Player ビルド側から strip された型を使う場合です。**Minimal かつ本件の構成では、link.xml は不要です。**
 
#### (6) ジェネリックコード生成オプション
 
`EditorUserBuildSettings.il2CppCodeGeneration`（Unity 6.3 ScriptReference、Built on 2026-08-27。ただし当該プロパティは Obsolete と表示され、`PlayerSettings.SetIl2CppCodeGeneration` / `GetIl2CppCodeGeneration` の使用が推奨）の記述です。
 
- `OptimizeSpeed`: 「generates code that is optimized for runtime performance. **This is the default** and the behavior in previous versions of Unity.」
- `OptimizeSize`: 「generates a universal version of each generic type and method. **This avoids some restrictions OptimizeSpeed has when executing generic code.**」
つまり Full Generic Sharing による万能版生成はデフォルトでは有効ではありません。ジェネリック起因の AOT 例外が実際に発生した場合、`OptimizeSize`（Optimize for code size and build time）への切替が公式に文書化された緩和手段です。
 
`--generic-virtual-method-iterations` については、Unity 6.3 の公式マニュアル内に記述を発見できませんでした（未確認）。
 
#### (7) Web プラットフォーム固有
 
Unity 6.3「Web technical limitations」（Built on 2026-08-26）の原文引用です。
 
> Web is an AOT platform, so it doesn't allow dynamic generation of code using `System.Reflection.Emit`. This is the same on all other IL2CPP platforms, iOS, and most consoles.
 
> Managed (C#) threads aren't supported due to the lack of a multithreaded garbage collection feature in WebAssembly.
 
本件のコードはスレッドも `Reflection.Emit` も使用しないため、これらの制限には抵触しません。
 
#### (8) 「条件付きで安全」の条件
 
- **型引数に値型を使う派生を作らないこと。** `EventChannelSO<int>` や `EventChannelSO<SomeStruct>` を作った時点で (2) の値型特殊化の議論に入ります。安全側に倒す場合、Unity マニュアルが明示的に推奨する `where T : class` 制約を付けます（「using reference type sharing, which causes no performance degradation」）。
- **`Activator.CreateInstance` / `MakeGenericType` / `MakeGenericMethod` でチャンネルを動的生成しないこと。** 確認できた全失敗事例の共通トリガーです。DI コンテナにチャンネル型の生成を任せる場合も同じ経路に入ります。
- **`.asset` を Addressables / AssetBundle で配信する場合**、(5) の記述に留意すること。Minimal ではユーザコードが strip されないため、現構成では問題になりにくい状態です。
- **Web ビルドで実機確認を行うこと。** JoshPeterson の「we cannot statically determine this by looking at the code. You will need to test the code at run time to be sure.」が公式に示されている唯一の確定手段です。
### 未確認の残件
 
- Unity Issue Tracker の該当バグチケット本文。`issuetracker.unity3d.com/issues/il2cpp-*` は 302 で `not-found` にリダイレクトされ、過去のチケットの存在を検証できませんでした。
- `--generic-virtual-method-iterations` の公式記述。6000.3 マニュアル内に発見できず、存在・意味・デフォルト値すべて未確認です。
- `System.Action<T>` を ScriptableObject の event フィールドに置くことの IL2CPP 固有問題。「問題が文書化されていない」のであって「問題がないと文書化されている」ではありません。
- オープンジェネリックな `ScriptableObject` から直接 `.asset` を生成できるかについての公式記述。6.3 マニュアルに明示なし。本件は具体型サブクラスを使うため実害はありません。
- 「event フィールドはシリアライズされない」の明文。6.3 の serialization rules ページはシリアライズ可能型を列挙するのみで、delegate / event を除外する明文はありません。
- Unity 6.3 における Full Generic Sharing の現状を記述した公式ページ。参照したブログは 2021-12-15 / Unity 2022.1 beta 時点のものです。
- 「ジェネリック基底クラス＋参照型引数＋具体型サブクラス」というパターンでの失敗報告がゼロであることは、非存在の証明ではありません。
### 出典
 
- https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-restrictions.html （Unity 6.3 LTS / ページ表示 Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping-configure.html （Unity 6.3 LTS / Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping-content.html （Unity 6.3 LTS / Built on 2026-06-05）
- https://docs.unity3d.com/6000.3/Documentation/Manual/managed-code-stripping-preserving.html （Unity 6.3 LTS / Built on 2026-07-23）
- https://docs.unity3d.com/6000.3/Documentation/Manual/unity-linker.html （Unity 6.3 LTS / Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html （Unity 6.3 LTS / Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-technical-overview.html （Unity 6.3 LTS / Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/Manual/il2cpp-introduction.html （Unity 6.3 LTS / Built on 2026-08-26）
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUserBuildSettings-il2CppCodeGeneration.html （Unity 6.3 LTS / Built on 2026-08-27）
- https://docs.unity3d.com/2020.1/Documentation/Manual/script-Serialization.html （Unity 2020.1）
- https://unity.com/blog/engine-platform/il2cpp-full-generic-sharing-in-unity-2022-1-beta （2021-12-15）
- https://discussions.unity.com/t/generic-virtual-methods-on-il2cpp-how-it-works/232067 （2020-03-02。フォーラム＝補助情報。ただし発言者は Unity Technologies のスタッフ）
- https://discussions.unity.com/t/il2cpp-aot-generic-virtual-methods-no-more-a-problem/789746 （2020-05-08 / 05-11。同上）
- https://discussions.unity.com/t/no-ahead-of-time-aot-code-was-generated/831569 （2021-03-06。補助情報）
- https://discussions.unity.com/t/runtime-aot-compilation-errors-of-webgl-release-build/876520 （2022-03-29。補助情報）
- https://discussions.unity.com/t/executionengineexception-using-il2cpp/606487 （2015-11-24。補助情報）
- https://www.mono-project.com/docs/advanced/runtime/docs/gsharedvt/ （Mono Project。Unity 公式ではない。更新日不明）
---
 
## 項目3：JetBrains 学生ライセンスの商用利用範囲
 
### 結論
 
規約は「Product（＝Rider 等のソフトウェア自体）の商用目的での使用」を明確に禁止しています。原文は `use the Product for any commercial purposes.` です。
一方、**開発した成果物（work product / output）に言及する条項は契約中に一切存在しません**。したがって「Rider で開発したゲームを後に有償配布・広告収益化すること」が規約違反に当たるかは、**契約文言のみからは断定不可**です。
学生資格を失った時点で「immediately discontinue use」が求められますが、それまでに作成した成果物の扱いに関する条項は存在しません。
 
### 根拠
 
#### (1) 準拠文書の特定
 
- 文書名（原文）: `Toolbox Subscription Agreement for Students and Teachers`
- URL: https://www.jetbrains.com/legal/docs/toolbox/license_educational/
- 版・発効日（原文引用）: `Version 4.2, effective as of May 01, 2024`
- 節構成: 1 PARTIES / 2 DEFINITIONS / 3 GRANT OF RIGHTS / 4 ACCESS TO PRODUCTS / 5 SUBSCRIPTION RENEWAL / 6 FEEDBACK / 7 THIRD PARTY SOFTWARE / 8 WARRANTY LIMITATIONS / 9 DISCLAIMER OF DAMAGES / 10 TERM AND TERMINATION / 11 TEMPORARY SUSPENSION / 12 EXPORT REGULATIONS / 13 GENERAL
Section 13 GENERAL が組み込むと述べる文書（原文引用）:
 
> The following documents are part of ('incorporated into') this Agreement: the JetBrains Privacy Notice
 
**支配しない文書（混同注意）**：`JETBRAINS USER AGREEMENT` Version 2.0（effective April 16, 2025、https://www.jetbrains.com/legal/docs/toolbox/user/）は教育用契約からその名称で参照されていません。同文書 3.2 の以下の文言は**トライアル／フリーミアムに関する条項であり、教育ライセンスには適用されません**（原文引用）。
 
> You may use the Product for free for any commercial or non-commercial purposes: (a) during the Trial Period; and (b) if the Product supports a freemium mode, also after the end of the Trial Period in the freemium mode.
 
#### (2) 商用利用を制限する条項の原文引用
 
Section 3.2（許諾本文、原文引用）:
 
> JetBrains grants Customer the non-exclusive and non-transferable right to use each Product covered by the Subscription for non-commercial, educational purposes only
 
続く文言（原文引用）:
 
> (including conducting academic research or providing educational services)
 
期間（原文引用）: `for a period of one (1) year as stipulated below`
 
Section 3.2(A) 許可事項（原文引用）:
 
> **You may:**
> (i) install and use any version of the Product covered by the Subscription and listed at https://www.jetbrains.com/student on any number of Machines and on any operating system supported by the Product; use such software for non-commercial, educational purposes only, including conducting academic research or providing educational services; and
> (ii) make one copy of the Product solely for archival, security, and/or backup purposes.
 
Section 3.2(B) 禁止事項（原文引用）:
 
> **You may not:**
> (i) allow the same Subscription to be used concurrently by another user apart from yourself;
> (ii) rent, lease, reproduce, modify, adapt, create derivative works of, distribute, sell, or transfer the Product;
> (iii) provide a third party with access to the Product or your JetBrains Account, or the right to use the Product;
> (iv) reverse engineer, decompile, disassemble, modify, translate, or make any attempt to discover the source code of, the Product;
> (v) remove or obscure any proprietary or other notices contained in the Product;
> (vi) use the Product for any commercial purposes.
 
**(vi) が本件の中核条項です。**
 
#### (3) 「Educational Purposes」「commercial purposes」の定義の有無
 
**独立した定義は存在しません。** Section 2 (DEFINITIONS) で定義されている語は `Affiliate` / `Agreement` / `EAP Version` / `JetBrains Account` または `JBA` / `Machine` / `Product` / `Product Version` / `Redistributable Product` / `Subscription` / `Subscription Confirmation` のみであり、`Educational Purposes`・`commercial purposes`・`non-commercial` はいずれも定義されていません。
 
唯一の手がかりは 3.2 内の非限定列挙（`including` は例示であり限定列挙ではありません）です。
 
> (including conducting academic research or providing educational services)
 
Section 1 (PARTIES) の学生の定義（原文引用）:
 
> 'student' is an individual who studies full-time at a recognized educational institution
 
#### (4) 有償配布・広告収益化の可否 ―― 断定不可
 
**断定できること**
 
- 3.2(B)(vi) は `use the Product for any commercial purposes.` を禁止します。`any` が付されており、商用目的での Product 使用に例外は設けられていません。
- 3.2 の許諾自体が `for non-commercial, educational purposes only` に限定されており、`only` により許諾範囲外の使用は無許諾使用となる構造です。
**断定できない理由（曖昧な文言の特定）**
 
1. **規制対象は「Product の使用」であって「成果物」ではありません。** 禁止文言の文法上の目的語は the Product（＝Rider）であり、Rider で作られたゲームではありません。契約中に work product / output / application / code you create に言及する条項は一切存在しません。「収益化されるゲームの開発行為＝Product の商用目的使用」と読むか、「Product 自体の商業的利用（再販・商用サービス提供等）のみを指す」と読むかが、文言上どちらとも決まりません。
2. **`commercial purposes` が未定義です。** 金銭の授受を伴えば商用なのか、事業活動としての性質を要するのかがテキスト上不明です。
3. **時点の問題が未規定です。** 開発時点では無償の個人プロジェクトで、後に有償化・広告掲載する場合、どの時点の「目的」で判定するかを定める文言が存在しません。
4. **例示が反対解釈を許しません。** `including conducting academic research or providing educational services` は例示であり、これに該当しない用途がすべて禁止されるとは文言上導けません。
**参考（契約外・拘束力なし。ただし発行者は JetBrains）**
 
- 「I'm a student, and I work part-time. Can I use my free educational license for my work, apprenticeship, or internship projects?」（最終更新 2023-05-19）。原文引用: `free educational licenses are to be used strictly for non-commercial educational purposes` / `you can purchase a personal subscription for commercial projects (monthly or annual)`。ただしこの記事が扱うのは雇用・インターン業務であり、個人開発物の収益化には直接言及していません。
- 「Do you offer free educational licenses for students and teachers?」（最終更新 2026-01-07 17:05）。原文引用: `educational licenses can't be used for commercial work`
**別制度との混同禁止**：JetBrains には別枠の「free non-commercial license」制度があり、その FAQ（最終更新 2026-08-13 16:36）は商用を `developing products and earning commercial benefits from your activities.` と定義し、非商用側に `learning and self-education, open-source contributions without earning commercial benefits, any form of content creation, and hobby development.` を挙げています。しかし当該 FAQ は教育ライセンスには適用されないと明示されており、教育ライセンスの解釈根拠にはできません。
 
#### (5) 失効条件と成果物の扱い
 
Section 3.5（原文引用）:
 
> Your access to and use of the Products is conditional on your status as a student or instructor. Customer: (A) agrees to immediately discontinue use of all Products covered by the Subscription if the Customer ceases to be a student or an instructor; and (B) warrants that the information Customer provides to JetBrains about the Customer's status as a student or instructor is complete and accurate.
 
すなわち学生・教員でなくなった時点で `immediately discontinue use`（直ちに使用中止）であり、猶予期間の定めはありません。
 
Section 5.1（原文引用）:
 
> Customer may renew its Product subscription for another year by submitting a written request to JetBrains 30 (thirty) days prior to the end of the Subscription period.
 
Section 5.2（原文引用）:
 
> If not agreed otherwise in writing between JetBrains and Customer, in the event of subscription renewal, the relationship between the parties shall be governed and amended (if applicable) by the terms and conditions of the subscription agreement covering use of the Product available at www.jetbrains.com on the day of subscription renewal.
 
つまり 1 年更新制で、更新は 30 日前の書面請求により行い、更新時は更新日時点の規約が適用されます（規約は将来変更されうる構造です）。
 
**成果物の帰属・ライセンス終了後の継続利用について：該当条項なし。**
確認した文書は `Toolbox Subscription Agreement for Students and Teachers`, Version 4.2, effective as of May 01, 2024 の全 13 節です。ユーザーが作成した work product / project / code / output の所有権、ライセンス期間終了後の継続利用可否、配布可否を定める文言は一切ありません。禁止条項も許可条項も存在しないため、この点は契約に規定がなく判断不能です。
 
### 未確認の残件
 
- Section 10 (TERM AND TERMINATION) の逐語確認。取得時に要約形で返却されたため原文引用ができていません。要約として確認できた範囲では「無権限使用・虚偽情報・Section 3.2 違反については 7 日通知で解除可」「存続条項は Sections 6, 7, 8, 9, 13」とされていますが、**逐語未確認**です。
- 3.2 本文の付随句が括弧付き `(including ...)` で、3.2(A)(i) 内が読点＋`including ...` として返された点。同一表現が 2 箇所に現れるのか、取得側の整形差かは未確認です。意味内容に差はありません。
- ページ取得は要約モデルを経由するため、引用の句読点レベルでの完全一致は保証できません。規約判断に用いる場合は原ページでの直接確認を推奨します。
- 指示にあった `https://sales.jetbrains.com/hc/en-us/articles/207154369` は 404 でした。現行の該当記事は下記の URL です。
### 出典
 
- https://www.jetbrains.com/legal/docs/toolbox/license_educational/ （Version 4.2, effective as of May 01, 2024）
- https://www.jetbrains.com/legal/docs/toolbox/user/ （JETBRAINS USER AGREEMENT Version 2.0, effective April 16, 2025。※教育ライセンスには適用されない参考）
- https://sales.jetbrains.com/hc/en-gb/articles/11564854623378-I-m-a-student-and-I-work-part-time-Can-I-use-my-free-educational-license-for-my-work-apprenticeship-or-internship-projects （最終更新 2023-05-19。補助情報）
- https://sales.jetbrains.com/hc/en-gb/articles/207241195-Do-you-offer-free-educational-licenses-for-students-and-teachers （最終更新 2026-01-07。補助情報）
- https://sales.jetbrains.com/hc/en-gb/articles/18950890312210-The-free-non-commercial-licensing-FAQ （最終更新 2026-08-13。別制度・参考）
---
 
## 項目4：GitHub Copilot Pro の利用上限
 
### 結論
 
現行制度は 1 つに特定できました。**2026-06-01 に request-based billing（premium requests）は usage-based billing（AI Credits）へ置き換えられ**、現行の Copilot Pro は月額 $10 で月間 1,500 AI credits（base 1,000 ＋ flex 500）です。
「月 300 premium requests」は、2026-06-01 以降もレガシーの年額課金に残留した既存契約者にのみ適用されるレガシー制度です。
なお **Student Developer Pack の学生は 2026-03-13 以降 Copilot Pro ではなく「Copilot Student」という別プラン**に移行しており、この点は前提の修正が必要です。
 
### 根拠
 
#### (1) 現行の Copilot Pro の上限
 
GitHub Docs「About individual Copilot plans」および「Plans for GitHub Copilot」の記載です。
 
| プラン | 価格 | Base credits | Flex allotment | 月間合計 AI credits |
|---|---|---|---|---|
| Copilot Pro | $10 USD / month | 1,000 | 500 | 1,500 |
| Copilot Pro+ | $39 USD | 3,900 | — | 7,000 |
| Copilot Max | $100 USD | 10,000 | — | 20,000 |
 
Copilot Pro の説明（原文引用）:
 
> For developers who want more flexibility, including unlimited completions and access to additional models.
 
クレジット単価は「1 AI credit = $0.01 USD」と記載されており、1,500 credits = $15 相当です。したがってご提示の「Pro = 月 $15 相当」はクレジット合計額としては一致しますが、**月額そのものは $10** です。
 
「Flex allotment」の定義（原文引用）:
 
> This is an additional monthly amount on top of your base credits. The flex allotment is a variable part of your included usage; it is designed to adapt as the economics of AI evolve, including model pricing, new models, and improvements in efficiency.
 
未使用分の繰り越しは不可（月末失効）です。当該ページに "premium request" の語は一切登場しないことを確認しています。
 
#### (2) premium requests と AI Credits の関係 ―― 置き換え
 
GitHub Docs「What changed with billing」（原文引用）:
 
> As of June 1, 2026, GitHub replaced request-based billing with usage-based billing
 
課金単位は「premium request units (PRU) × モデル倍率」から「消費トークンとモデル種別に基づく従量課金（AI credits）」へ変更されました。**併存ではなく置き換えです。**
 
発表記事は https://github.blog/changelog/2026-06-01-updates-to-github-copilot-billing-and-plans/ （公開日 2026-06-01）で、「usage-based billing for GitHub Copilot is now live for all users」と記載されています。同記事に "premium requests" の語は登場しません。
 
PRU から credit への換算式は公式には示されていません（未確認）。
 
#### (3) 施行日
 
**2026年6月1日**です。移行措置として、既存の年額プラン契約者は契約終了までレガシーの request-based billing に残留可能とされています。正式な移行期限ウィンドウは示されていません。
 
#### (4) 「月 300 premium requests」説の現在の地位
 
ご提示の URL `https://docs.github.com/en/copilot/managing-copilot/monitoring-usage-and-entitlements/about-premium-requests` は現在 `https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/copilot-requests` へリダイレクトされます。同ページ冒頭のバナー（原文引用）:
 
> This article only applies to Copilot Pro and Copilot Pro+ subscribers on an existing annual plan who remained on legacy premium request-based billing after June 1, 2026.
 
同ページ内の Copilot Pro の値は「300 per month」です。また「Billing for premium requests began on June 18, 2025, for all paid Copilot plans on GitHub.com, and on August 1, 2025, on GHE.com.」との記述があります。
 
すなわち **300/月は、2026-06-01 以降もレガシー年額課金に残った既存契約者にのみ有効**であり、新規・月額の Copilot Pro には適用されません。
 
#### (5) Student Developer Pack との差異 ―― 差異あり
 
**学生はもはや Copilot Pro を受け取りません。**
 
GitHub Education の記載（原文引用）:
 
> GitHub Copilot Student is available to verified students. The plan includes unlimited code completions and an allowance of GitHub AI Credits, plus limited chat and agent usage with models available through auto model selection only.
 
GitHub Docs「About individual Copilot plans」（原文引用）:
 
> Copilot Free and Copilot Student both have an allowance of AI credits and access to models through auto model selection only.
 
変更の発表は https://github.blog/changelog/2026-03-13-updates-to-github-copilot-for-students/ （公開日 2026-03-13）で、「Starting today, students with GitHub Education benefits are now on the new GitHub Copilot Student plan.」と記載されています。施行日は 2026-03-13 です。
 
**文書化されている差異**：コード補完は無制限だが、チャット／エージェント利用は限定的であり、モデルは auto model selection のみ（モデル選択不可）です。
 
ご提示の `get-free-access-to-copilot-pro` は教員および著名 OSS メンテナ向けのページであり、「free access to Copilot Pro」とのみ記載され、上限の差異についての記述はありません。学生向けは別ページ `free-access-with-copilot-student` に分離されています。
 
#### (6) 上限到達後の挙動
 
ハードストップでも自動従量課金でもなく、予算設定によるオプトイン制です。GitHub Docs「Usage-based billing for individuals」（原文引用）:
 
> If your included credits are exhausted, you can continue working by setting a budget for additional usage.
 
> additional usage may be capped, so to keep working, you'll need to pay off any additional usage you've already consumed.
 
上位プランへのアップグレードも選択肢として示されており、「The upgrade cost is only the price difference between your current plan and the new plan.」と記載されています。
 
レガシー側（300 PRU）の挙動は異なり、上限後も included models で継続可（レート制限あり）、追加は $0.04/request でした。新方式でこの「無料モデルへのフォールバック」に相当する記述は確認できませんでした。
 
#### (7) 両説の突き合わせ
 
| 説 | 出典 | 日付 | 現行性 |
|---|---|---|---|
| (A) 月 300 premium requests | docs.github.com …/request-based-billing-legacy/copilot-requests | 2026-06-01 基準でレガシーと自己申告 | 旧・限定適用 |
| (B) AI Credits 制 | plans / individual-plans / usage-based-billing-for-individuals、changelog | 2026-06-01 発効 | **現行** |
 
**より新しい出典は (B) です。** (A) の出典自体が「2026-06-01 以降のレガシー残留者限定」と明示しているため、両者は矛盾ではなく適用範囲の違いです。
 
### 未確認の残件
 
- premium request から AI credit への換算式。公式に示されていません。
- **Copilot Student の具体的な AI クレジット数**。公式ドキュメント上に数値の記載がありません。ご自身の請求画面での実確認が必要です。
- 新課金方式でクレジット枯渇後にベースモデルでの継続利用が可能かどうか。
- 参照した GitHub Docs 各ページの "Last updated" 日付。本文中に表示されず、確認できたのは「© 2026 GitHub, Inc.」のみでした。changelog 記事の公開日は確認済みです。
- https://github.blog/changelog/2026-08-28-upcoming-changes-to-github-copilot-policies-and-billing/ （2026-08-28）に今後の変更が予告されています。内容は本レポートでは精査していません。
### 出典
 
- https://docs.github.com/en/copilot/get-started/plans （日付表示なし）
- https://docs.github.com/en/copilot/concepts/billing/individual-plans （日付表示なし）
- https://docs.github.com/en/copilot/concepts/billing/usage-based-billing-for-individuals （日付表示なし）
- https://docs.github.com/en/copilot/reference/copilot-billing/models-and-pricing （日付表示なし）
- https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/what-changed-with-billing （本文に "As of June 1, 2026" と記載）
- https://docs.github.com/en/copilot/reference/copilot-billing/request-based-billing-legacy/copilot-requests （本文に "after June 1, 2026" と記載）
- https://github.blog/changelog/2026-06-01-updates-to-github-copilot-billing-and-plans/ （2026-06-01）
- https://github.blog/changelog/2026-03-13-updates-to-github-copilot-for-students/ （2026-03-13）
- https://github.blog/changelog/2026-08-28-upcoming-changes-to-github-copilot-policies-and-billing/ （2026-08-28）
- https://docs.github.com/en/copilot/how-tos/manage-your-account/get-free-access-to-copilot-pro （日付表示なし）
- https://docs.github.com/copilot/how-tos/manage-your-account/free-access-with-copilot-student （日付表示なし）
- https://education.github.com/pack （日付表示なし）
 