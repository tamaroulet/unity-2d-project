# 指示書 21: シーンの状態スナップショットを生成物として持つ

```
ROLE: Executor
BRANCH: main
判断: 人間との直接協議（Architect の読み取りコスト削減）。02-context.md なし
```

## 目的

`00_rules.md` が `.unity` の読み込みを禁止し、Architect が読めるファイルを 3 つに絞った。
**禁止しただけでは、代わりに読む安いものが存在しない。** それを作るのが本サイクルである。

現状、「何が何に結線されているか」を知る手段は `MainGame.unity`（約 10.6 万トークン）を
読むことしかない。既存の `SceneBindingReport` には 3 つの穴がある。

```
SceneBindingReport.cs:33  mb.GetType().Namespace == "Game.UI" だけを見ている
                          → GameFlowController（Game.Features.GameFlow）が対象外。
                            「未バインド 19 件」という数字は UI 層だけの数字だった
SceneBindingReport.cs:57  val == null のフィールドしか出さない
                          → 「結線されている先が何か」は一切わからない
SceneBindingReport.cs:68  Debug.Log のみ。Console を閉じたら消える
                          → コミットできない。差分も取れない
```

**結線されている先まで含めた一覧を、決定的な順序でファイルに吐き、コミットする。**
これが Architect の読む唯一のシーン情報になる。

---

## 実装方針（この形で直すこと。自分で別案に変えないこと）

### A. 出力先

`docs/snapshot/scene_bindings.txt`（リポジトリ直下の `docs/` 配下）。

**`Game/Assets/` の下に置かないこと。** Unity が TextAsset として取り込み、
`.meta` が増えて毎回ノイズになる。

### B. `SceneBindingReport.cs` に MenuItem をもう 1 つ足す

`Game/Assets/Editor/SceneBindingReport.cs`

`Tools/Generate Scene Snapshot` を追加する。既存の
`Tools/Report Unbound Serialized Fields`（`:15-70`）は**残す**（人間が Console で見る用）。

走査対象を広げる。`:33` の

```csharp
mb.GetType().Namespace == "Game.UI"
```

を

```csharp
mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("Game.")
```

に変える。**既存の MenuItem 側もこの条件に揃える。** 19 件 / 15 件という既報の数字は
UI 層だけを数えたものなので、本サイクル以降は数え直しになる。それでよい。

### C. 出力する内容

`Game.*` 名前空間の MonoBehaviour ごとに、次を 1 ブロックで出す。

```
<GameObject のシーンパス>  [active=true|false]
  <型名>
    _fieldName -> <結線先の名前> (<結線先の型名>)
    _fieldName -> <unbound>
```

- **`active` は必ず出す。** `MetaShopDialogView` が非アクティブなパネルに乗っていて
  `OnEnable` が走らなかった不具合を 2 サイクル続けて踏んでいる。この 1 語で見える。
- 対象フィールドは `[SerializeField]` または public で、型が `UnityEngine.Object` 派生のもの。
  既存 `:51-54` の判定をそのまま使う。
- 結線先が `ScriptableObject` のときはアセット名、シーン内オブジェクトのときは
  GameObject 名（コンポーネントなら `所属GameObject名/型名`）。

末尾にサマリを 1 行。

```
=== components: N / bound: N / unbound: N ===
```

### D. 決定的であること（最重要）

**同じシーンに対して 2 回走らせたら、1 バイト違わず同じファイルになること。**

- ブロックは GameObject のシーンパスでソートし、同一パス内は型名でソートする
- フィールドはリフレクションの返り順ではなく**フィールド名でソート**する
- **タイムスタンプ・実行者・Unity バージョンをファイルに書かない**（毎回差分が出る）
- 改行コードは `\n` に統一する

これが崩れると、差分がノイズで埋まってスナップショットの意味が消える。

### E. シーンを保存しないこと

既存 `:20-23` と同じく、必要なら `MainGame.unity` を開く。
**`EditorSceneManager.SaveScene` を呼ばない。** 生成物はテキストファイルだけである。

### F. `UILayoutBuilder` 実行後に必ず再生成する

`UILayoutBuilder.SetupCompleteLayout` の末尾から `Tools/Generate Scene Snapshot` の
本体メソッドを呼ぶ。シーンを作り直したのにスナップショットが古い、という状態を作らない。

---

## やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/instructions/*.md（本書以外）  README.md  docs/spec/**  docs/decisions/**
.agents/rules/00_rules.md  .claude/hooks/guard.js  .github/workflows/**
scripts/handoff.ps1  scripts/nightly_gate.py  scripts/auto_runner.py
Game/Packages/manifest.json  .mcp.json
```

- **シーンの中身を変えない。** 本サイクルは読み取り専用の生成物を作るだけである。
  未バインドのフィールドを見つけても**直さない**。数えるだけ
- `.unity` / `.asset` をテキスト編集しない
- スナップショットに GameObject の座標・サイズ・色を含めない（結線だけが対象。量が爆発する）
- 出力を JSON や YAML にしない。人間と AI が両方読むテキストにする
- 既存の `Tools/Report Unbound Serialized Fields` を削除しない
- CI（`.github/workflows/**`）や `nightly_gate.py` に検査を足さない（別サイクル）

---

## 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | 0 エラー（警告 3 件は既存） |
| 2 | `Tools/Generate Scene Snapshot` を実行 | `docs/snapshot/scene_bindings.txt` が生成される |
| 3 | **同じ操作をもう 1 回実行し `git diff --stat docs/snapshot/`** | **差分 0 行**。1 行でも出たら D が満たせていない |
| 4 | 生成物の行数と文字数 | `wc -lc docs/snapshot/scene_bindings.txt` の生出力。**30,000 文字を超えていないこと** |
| 5 | 生成物に `GameFlowController` のブロックがあること | 該当ブロックをそのまま貼る（B の名前空間修正の証拠） |
| 6 | 生成物に `MetaShopDialogView` のブロックがあること | 該当ブロックをそのまま貼る。`active=` の値と `_panelRoot` の結線先が読めること |
| 7 | サマリ行 | `=== components: N / bound: N / unbound: N ===` の生出力 |
| 8 | EditMode / PlayMode テスト | 実行前と同じか改善。件数を明記 |
| 9 | `git status --porcelain` | 保護対象ファイルが 1 件も無い。**`MainGame.unity` が変更されていないこと**（E の証拠） |

検証 3 と 9 は本サイクルの合否そのものである。**要約せず生出力を貼ること。**

---

## 停止条件

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- 2 回実行して同じ出力にならない原因が特定できない
- スナップショットが 30,000 文字を超え、削る基準を自分で決める必要が出た
- `MainGame.unity` に差分が出てしまい、原因がわからない
- 保護対象ファイルの変更が必要になった

---

## 報告

`docs/handoff/2026-09-05-02/04-result.md` に規約 §5 の様式で書く。

```
指示書: docs/instructions/21_scene_snapshot.md
再試行: N / 2

## 変更
（git diff --stat の生出力）

## C# コード変更行数（実測）
（git show --numstat の生出力と合計行数）

## 検証
（上の 1〜9 の生出力を全部）

## 停止条件への抵触
なし / あり（内容）
```

コミットのみ行い、push はしない。

---

## 次サイクルの前提になること

本サイクルが緑になった時点で、`02-context.md` は `MainGame.unity` を引用する代わりに
`docs/snapshot/scene_bindings.txt` の該当ブロックを貼ればよくなる。
Architect が読むシーン情報はこのファイルだけになる。
