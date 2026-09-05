# Brain 層を Unity の外からビルド・テストできるようにする

## 決定日
2026-09-05

## ステータス
提案

## 背景

`docs/strategy/AUTONOMOUS_STUDIO_TARGET.md` §7 の Phase 1 の入口である。

実測（同 §2）で、**このプロジェクトのボトルネックは Unity の起動だけ**だと確定している。

```
EditMode  116 件 / 0.88 秒          ← 純粋計算
PlayMode    3 件 / Unity 起動込み    ← 分単位
2026-09-05 04:30  agy が PlayMode 系タスクに 30 分かけて exit=1
```

一方で、ランタイム C# 3,785 行のうち **`using UnityEngine` を含まないファイルが 20 本以上**
既に存在する（`GameState` / `TurnRules` / `CommandEffect` / `BossState` / `MetaProfileState` 等）。
**Brain の中身は既にある。Unity の外から触れないだけである。**

もう 1 つ、今日判明した事実がある。

```
$ git ls-files "Game/*.csproj" "Game/*.sln"
(空)
```

**`Game.sln` と各 `.csproj` は git 管理下に無い。Unity が生成している。**
つまり現在の `dotnet build Game/Game.sln` は「Unity が一度動いた PC でしか成立しない」。
機械判定のビルド項目も同じ前提に乗っている。

## 決定内容

**Unity の外に、手書きで git 管理する .NET プロジェクトを 1 組追加する。
Unity 側の構成（asmdef 1 枚、シーン、フォルダ配置）は一切変更しない。**

```
Game.Core/
  Game.Core.csproj          ← 手書き・git 管理・netstandard2.1
Game.Core.Tests/
  Game.Core.Tests.csproj    ← 手書き・git 管理・net8.0 + NUnit
Game.Core.sln
```

### ソースは複製せず、リンク参照する

`Game.Core.csproj` は自前のソースを持たない。**Unity 側の既存ファイルをそのまま取り込む。**

```xml
<Compile Include="../Game/Assets/Core/**/*.cs" />
<Compile Include="../Game/Assets/Features/**/Scripts/*.cs" Exclude="...(UnityEngine 依存のもの)" />
```

- ファイルの実体は `Game/Assets/` の 1 箇所のみ。**コピーも DLL も生成しない**
- Unity から見た構成は今日と 1 バイトも変わらない
- `dotnet test Game.Core.Tests` は **Unity が入っていない PC でも動く**

### 境界の強制はビルドが行う

> **`Game.Core.csproj` に含まれるファイルに `using UnityEngine` を書いたら、`dotnet build` が落ちる。**

規約の文章ではなく、コンパイラが物理的に弾く。`00_rules.md` の
「違反は物理的にブロックする。文章で守らせるのではない」に沿う。

`scripts/mechanical_check.ps1` に判定項目を 1 行追加する。

```
PASS/FAIL Core: dotnet build Game.Core.sln → 0 errors
```

### asmdef 規約は変更しない

**新しい asmdef を作らない。** `00_rules.md`「ランタイムアセンブリは `Game.asmdef` 1 つ」は
そのまま維持される。追加するのは Unity の外の csproj であり、Unity のアセンブリではない。

規約に足すのは次の 1 行のみとする。

> `Game.Core.csproj` に含まれるファイルは `UnityEngine` に依存してはならない。
> 依存の有無は `dotnet build Game.Core.sln` が判定する。

## 検討した代替案

### 案 A: `Game.Core.asmdef`（`noEngineReferences: true`）を Unity 内に作る

**却下。** 2 つの理由による。

1. `00_rules.md` が asmdef の新設を明示的に禁じており、規約改訂が必要になる
2. **目的を達成しない。** asmdef が生成する csproj は Unity の生成物であり git 管理外なので、
   結局「Unity が動いた PC でしか `dotnet test` できない」という現状の制約が残る

### 案 B: Pure .NET のクラスライブラリを作り、DLL を `Assets/Plugins/` へコピーする

外部研究（`parallel_dev_brain_body_research.txt` 選択肢 D）が推す形。**却下。**

- ソースの実体が 2 箇所になるか、ビルド成果物が Unity 側に入る
- **stale DLL 事故**が起きる。C# を直したのに Unity 側が古い DLL を見ている、という
  最も切り分けの難しい故障を持ち込む
- コピーのタイミングを人間か CI が管理することになり、目標 1（人間の手数を減らす）に逆行する

リンク参照（採用案）は同じ利点を、この欠点なしで得られる。

### 案 C: 何もしない（現状維持）

**却下。** Phase 2 以降（Brain 層の AI 並列化、GameCI、Asset-as-Code）が全部この上に乗る。
ここを飛ばすと、重い Unity を並べる方向にしか進めない。

## 結果と影響

### 得られるもの

- ロジックの検証が **Unity 起動なし**で完結する。実装 AI が Domain Reload と
  Library ロックとライセンスから解放される
- Brain 層に限れば実装 AI を並列に増やせる（目標 3 の前提）
- 境界が**コンパイルエラーとして**可視化される。レビューで人が見つける必要が無い
- `csproj` が git 管理下に入るので、クリーンな環境でもビルド検証が再現する

### 代償と注意

- **`Exclude` の一覧が育つ。** `Features/**/Scripts/*.cs` には `*SO.cs` のような
  UnityEngine 依存ファイルが混在している。当面は除外リストで凌ぎ、
  Resolver のロジック切り出し（ADR 0002）が進むにつれて縮む
- **二重ビルドになる。** Unity 側と `Game.Core` 側で同じファイルが 2 回コンパイルされる。
  実害は無いが、コンパイルエラーが 2 箇所から出る
- テストが 2 系統になる（`dotnet test` と Unity Test Runner）。
  **Brain のテストは `dotnet test` 側に寄せ、EditMode からは移す**方針とする

### 実施しない範囲

本 ADR は「外から触れるようにする」までである。次は含まない。

- Resolver のロジックを SO から出すこと（**ADR 0002**）
- 既存 EditMode テストの移設（ADR 0002 の後）
- Brain 層での AI 並列化（Phase 2）

### 着手条件

`docs/strategy/AUTONOMOUS_STUDIO_TARGET.md` §7 のとおり、**Phase 0 の出口条件を満たしてから。**
すなわち指示書 24（永続化）が完了し、人間がブラウザで 1 周できることを確認した後とする。
