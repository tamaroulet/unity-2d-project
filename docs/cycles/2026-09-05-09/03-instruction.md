# 指示書 30: Code-first Bootstrap（第 1 段：EndingPanel 1 枚のみ）

```
ROLE: Executor
BRANCH: main
判断: docs/decisions/0002-code-first-bootstrap.md
性質: ADR 0002 の段階 1〜3 のみ。パネル 1 枚で成立を確認する。残りは次サイクル
```

## 目的

残る唯一の構造的欠陥が Inspector 結線である。3 サイクル連続で同じ罠を踏み、
未バインドが 15 件残り、シーンが 10.6 万トークンある。

`UILayoutBuilder` は既に UI 全体をコードから構築しており、シーンはその出力をキャッシュしているだけである。
**キャッシュをやめて実行時に構築すれば、結線という工程そのものが消える。**

**本サイクルでは `EndingPanel` 1 枚だけを移す。** 成立を確認してから広げる。

---

## 1. 実装方針

### A. `Bootstrapper` を作る

`Game/Assets/UI/Scripts/UiBootstrapper.cs`（新規、ランタイム）

- シーンの `Canvas` を `[SerializeField]` で受け取る
- `Awake()` で対象パネルを構築し、View に依存を渡す
- **`UnityEditor` 名前空間を使わない**（規約。WebGL で消滅する）

### B. `EndingPanel` の構築をランタイムへ移す

`UILayoutBuilder.SetupEndingPanel` と `BindEndingViewSceneReferences` の内容を、
`UnityEditor` 依存を外してランタイム側へ移植する。

- `new GameObject` / `AddComponent` / `RectTransform` 設定はそのまま動く
- `SerializedObject` / `FindProperty` による結線は**直接代入または `Bind()` 呼び出し**に置き換える
- `AssetDatabase.LoadAssetAtPath` は `Resources.Load` に置き換える
  - `EndingDecidedChannel.asset` を `Assets/Resources/` 配下へ移す必要がある。
    **移動は Editor スクリプト経由で行う**（`.asset` のテキスト編集は禁止）

### C. シーンから `EndingPanel` を消す

Editor スクリプトで `EndingPanel` を削除する。`EndingView` は `UIViews` に残してよい
（`Bootstrapper` から `Bind()` で結線する）。

**`UILayoutBuilder` からも `SetupEndingPanel` / `BindEndingViewSceneReferences` を削除する。**
2 系統が残ると、どちらが正か分からなくなる。

### D. 他のパネルには触らない

`StatusPanel` / `CommandPanel` / `EventDialogPanel` / `RelicDraftDialogPanel` /
`BossBattleDialogPanel` / `MetaShopDialogPanel` は**現状のまま**。次サイクルで 1 枚ずつ移す。

---

## 2. やってはいけないこと

- `.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集。**`.unity` を読まない**
- ランタイムコードでの `UnityEditor` / `AssetDatabase` の使用
- ランタイムコードでのシーン内検索（型名・名前・パス検索）
- `EndingView.cs` のロジック変更（`Bind()` の引数追加は可）
- 他のパネルの移行。**1 枚だけ**
- 新規 `EventChannelSO` / 新規 asmdef / Addressables
- 不変条件・機械判定・スナップショットの仕組みへの変更
- `docs/cycles/2026-09-05-08/` への追記（別サイクル）

保護対象は従来どおり（`docs/laws/**` `docs/decisions/**` `.agents/rules/**`
`.github/workflows/**` `scripts/**` `Game/Packages/**`）。

---

## 3. 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `scripts/mechanical_check.ps1` | 生出力。`Snapshot` は差分ありで正常（シーンが変わるため） |
| 2 | PlayMode テスト | INV-1〜5 が緑を維持。INV-6 は赤のまま（既知） |
| 3 | スナップショットの差分 | `EndingPanel` が消え、`EndingView` の `_panelRoot` 等が実行時結線に変わったこと |
| 4 | 未バインド件数 | 実行前 15 件から**減っていること**。生出力を貼る |
| 5 | **ターン 24 クリアをマウスで確認** | エンディングが出て、RESTART からショップへ進めること |
| 6 | **敗北をマウスで確認** | `GAME OVER` が出て、同じく復帰できること |
| 7 | WebGL ビルド | 成功し、ブラウザで 5 と 6 が再現すること |
| 8 | `git status --porcelain` | 保護対象 0 件 |

**5〜7 は実際のマウス操作で行う。** 3 は「結線が消えた」ことの証拠なので必ず貼る。

---

## 4. 停止条件

- 追加 300 行を超えた
- `Resources.Load` でチャンネル SO が取得できない
- 実行時構築したパネルで INV-1〜5 のいずれかが赤くなる
- WebGL でのみ動作が変わる
- 同じ失敗を 2 回直して直らない

## 5. 報告

`docs/cycles/2026-09-05-09/04-result.md`。先頭に `## 機械判定`（生出力）。
コミットのみ。push はしない。
