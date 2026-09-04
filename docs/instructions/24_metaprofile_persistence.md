# 指示書 24: メタプロフィールの永続化（周回をブラウザ越しに残す）

```
ROLE: Executor
BRANCH: main
判断: 人間との直接協議。指示書 19 §5-1 からの持ち越し（2 サイクル保留）
前提: 20 / 21 / 22 / 23 完了（4a46d63 時点で機械判定 全 PASS、EditMode 114、PlayMode 3/3）
```

## §0 着手前に 5 分でやること（Claude 枠リセット後）

指示書 22 の検証 5・6 が未実施のまま残っている。**本サイクルより先にこれを通すこと。**
通れば以降のサイクルで Claude の消費が下がるため、先にやるほど得になる。

```powershell
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
Remove-Item logs/claude_session_id.txt
powershell -ExecutionPolicy Bypass -NoProfile -File scripts/invoke_claude_safe.ps1 -Prompt "say ok"
```

1 回目で `logs/claude_session_id.txt` が作られ、2 回目が `--resume` で走り、
3 回目（ID 削除後）がフォールバックで**止まらずに**走ること。
標準出力をそのまま `docs/handoff/2026-09-05-03/04-result.md` に追記する。

---

## 目的

人間の最初の報告は「クリアしてもポイントが持ち越されない」だった。
指示書 19 で可視化と消費導線を通し、20 で敗北側にも通した。**残るのは 1 点。**

```
GameFlowController.cs:50  private MetaProfileState _metaProfile = new MetaProfileState();
```

メモリ上にしか無い。**ブラウザをリロードした瞬間に全部消える。**
WebGL で配信している以上、これは「持ち越されない」そのものである。

`MetaProfileState.cs:6` は既に責務を書いている。

> PlayerPrefs への保存・復元は上位層（GameFlow 等）の責務とし、

書いてあるだけで、実装が無い。それを閉じる。

---

## 1. 実装方針

### A. 保存先は `PlayerPrefs`。ファイル I/O は使わない

WebGL では `System.IO` でのファイル書き込みが期待どおりに動かない。
`PlayerPrefs` は WebGL で IndexedDB にマップされ、`PlayerPrefs.Save()` で確定する。
**`PlayerPrefs` + `JsonUtility` で行う。**

### B. シリアライズ用 DTO を作る

`MetaProfileState` は `record` で `IReadOnlyList<int>` を持つ。**`JsonUtility` はどちらも扱えない。**
変換用の `[Serializable]` クラスを新規に置く。

`Game/Assets/Features/MetaProgression/Scripts/MetaProfileDto.cs`（新規）

```csharp
[Serializable]
public sealed class MetaProfileDto
{
    public int Version = 1;
    public int AvailableMetaPoints;
    public int TotalEarnedMetaPoints;
    public int TotalRunsCompleted;
    public int[] UnlockedIds = Array.Empty<int>();
}
```

`Version` は必ず持たせる。将来フィールドが増えたとき、
**版が違う保存データを黙って読んで壊すのを防ぐ**ためである。
読み込み時に `Version` が想定外なら、**復元せず新規プロフィールを返す**（例外を投げない）。

### C. 保存・復元は静的ユーティリティに置く

`Game/Assets/Features/MetaProgression/Scripts/MetaProfileStore.cs`（新規）

```csharp
public static class MetaProfileStore
{
    public static MetaProfileState Load();            // 失敗時は new MetaProfileState()
    public static void Save(MetaProfileState profile);
}
```

- `MonoBehaviour` にも `ScriptableObject` にもしない。**シーン参照を増やさない**
- `MetaPointResolverSO` に入れない。あれは「内部状態を持たない純粋関数」と
  クラスコメントで宣言されている（`:16-31`）。I/O を混ぜない
- `PlayerPrefs` キーは 1 本。`"Game.MetaProfile"` とする
- `Save` の末尾で必ず `PlayerPrefs.Save()` を呼ぶ（WebGL の IndexedDB フラッシュに必要）
- `Load` は `PlayerPrefs.HasKey` が false、JSON が壊れている、`Version` が不一致、
  のいずれでも**例外を出さず新規プロフィールを返す**

### D. `GameFlowController` から 3 箇所だけ呼ぶ

`Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs`

1. `Awake()`（`:83-86`）で `_metaProfile = MetaProfileStore.Load();`
   **`Start()` ではなく `Awake()`。** `Start()` は `_autoStartOnPlay` で `StartGame()` を
   呼ぶため、そこでは既にロード済みでなければならない
2. `FinalizeRun`（`:348` 付近）の `_metaProfile` 更新直後に `MetaProfileStore.Save(_metaProfile);`
3. `TryPurchaseMetaUnlock` の成功時、`_metaProfile` 更新直後に同じく `Save`

**`StartGame()` では保存しない。** 周回開始時に `_metaProfile` は変化しない。

### E. リセット手段を用意する

`Game/Assets/Editor/` に `Tools/Clear Meta Profile` を足す。
`PlayerPrefs.DeleteKey("Game.MetaProfile")` + `PlayerPrefs.Save()` のみ。

**これが無いとテストのやり直しができない。** 前周の保存データが残ったまま
2 周目の初期ステータスを見ることになり、検証が成立しなくなる。

---

## 2. やってはいけない代替

次のファイルは保護対象。触らない。

```
docs/instructions/*.md（本書以外）  README.md  docs/spec/**  docs/decisions/**
.agents/rules/00_rules.md  .claude/hooks/guard.js  .github/workflows/**
scripts/**  Game/Packages/manifest.json  .mcp.json
```

- `.unity` / `.prefab` / `.asset` / `.meta` のテキスト編集は禁止。**`.unity` を読まない**
  （結線の確認は `docs/snapshot/scene_bindings.txt`）
- **本サイクルでシーンは変更しない。** 新しい `[SerializeField]` を 1 つも足さない
- `MetaProfileState` の既存 4 プロパティを変更しない（DTO 側で吸収する）
- `MetaPointResolverSO` を変更しない
- 新規 `EventChannelSO` / 新規 `ScriptableObject` / 新規 asmdef を作らない
- `Application.persistentDataPath` や `File.WriteAllText` を使わない（WebGL で機能しない）
- セーブデータの暗号化・難読化をしない（本サイクルの目的外）
- テストを通すためにプロダクションコードへ分岐や自己修復を足さない

---

## 3. 検証

| # | 内容 | 期待 |
|---|---|---|
| 1 | `scripts/mechanical_check.ps1` | 全項目 `PASS`。生出力を貼る（`STALE` を出さないためテストを先に流すこと） |
| 2 | EditMode テストを 1 件追加 | `MetaProfileState` ⇄ `MetaProfileDto` の往復で 4 プロパティが一致すること。**純粋計算なので EditMode で可**（規約適合） |
| 3 | EditMode テストをもう 1 件 | 壊れた JSON / 未知の `Version` を渡すと、例外を投げず新規プロフィール相当が返ること |
| 4 | `Tools/Clear Meta Profile` を実行 → Play | `POINTS: 0` から始まること |
| 5 | **1 周クリアしてポイントを貯め、Play を停止** | 停止前の `AvailableMetaPoints` を控える |
| 6 | **もう一度 Play する（Clear は押さない）** | HUD の `POINTS:` が 5 の値のままであること。**これが本サイクルの本体** |
| 7 | ショップでアンロックを買って Play 停止 → 再 Play → 新規周回 | 買った分だけ初期ステータスが上がっていること。`Game Started!` の Console 行を 2 回分並べて貼る |
| 8 | **WebGL ビルドを作り、ブラウザで 1 周 → リロード → 再訪** | ポイントが残っていること。**ここまで確認して初めて完了**（規約 DoD 3） |
| 9 | `Tools/Generate Scene Snapshot` 後の `git diff docs/snapshot/` | **0 行**（シーンを変更していない証拠） |
| 10 | `git status --porcelain` | 保護対象 0 件 |

検証 4〜8 は**実際のマウス操作**で行う。どの手段を使ったか明記する。

> 検証 8 を「ローカルで動いたので同じはず」で代替しないこと。
> `PlayerPrefs` の WebGL 挙動（IndexedDB への非同期フラッシュ）は
> エディタ実行と挙動が変わりうる、本サイクルで唯一の未知である。

---

## 4. 停止条件

- 1 タスクで 300 行を超える変更、または 3 コミットに到達した
- WebGL でだけポイントが残らない
  （**推測で `PlayerPrefs.Save()` を撒かない。観測結果を持って手を止め、人間に報告する**）
- `JsonUtility` が DTO を扱えず、別のシリアライザが必要になった
- シーンに差分が出た（検証 9 が 0 行にならない）
- 新規 SO / シリアライズフィールドを足さないと実装できないと判断した
- 保護対象ファイルの変更が必要になった

---

## 5. 報告と、次サイクルへ送るもの

`docs/handoff/2026-09-05-05/04-result.md` に規約 §5 の様式で書く。
先頭に `## 機械判定`（`mechanical_check.ps1` の生出力）を置く。

まだ片付いていないもの。**次サイクル以降で扱う。**

- `ResolveUnlockedRelics` のドラフト反映（`MetaPointResolverSO.cs:189-223` が未呼び出し。
  ただしカタログに `UnlockRelic` 種のデータが 0 件なので、データ追加とセットで行う）
- ショップの内部 ID 表示（`Unlock_Stat_Stamina_01`）を読める名前へ。Editor スクリプト経由
- `docs/webgl/` 13MB の扱い（Pages の配信元かを確認してから）
- `Game/Assets/Settings/Lit2DSceneTemplate.scenetemplate` 3.85MB が使われているかの確認
- 未バインド残件の棚卸し（`Tools/Report Unbound Serialized Fields` の内訳）
- 指示書 23 検証 6 の結果次第：ターン 12 のイベントが発火していなかった場合、
  `EventResolverSO` の発火条件の調査

コミットのみ行い、push はしない。
