# 指示書 13: UI Toolkit 移行パイロット（EventDialogView 1 本）

```
ROLE: Executor
CONSTRAINT: NO_ARCH_CHANGE / NO_PROTECTED_FILE_EDIT / NO_ALTERNATIVE_PROPOSAL
RULES: .agents/rules/00_rules.md
SCOPE: EventDialogView のみ。他の 7 本には触れない
```

---

## 0. なぜやるか（背景・議論の対象ではない）

自律開発の最大の障害は **UI の結線が `.unity`（AI が編集禁止のバイナリ相当）にある**ことである。
現状の実測値:

| | 現在 | UI Toolkit 移行後 |
|---|---|---|
| 結線点 | **`[SerializeField]` 47 個**（8 View 合計） | UXML の `name` 属性（テキスト） |
| 人間の Inspector 作業 | 47 箇所 | 画面あたり `UIDocument` 1 個 ＝ 約 7 個 |
| AI が編集できるか | **不可** | **可能** |

`com.unity.modules.uielements` は既に `Game/Packages/manifest.json` にある。
**パッケージ追加は不要**（追加しようとすると保護対象違反で隔離される）。

本指示書は 8 本のうち **1 本だけ**を移行し、経路全体が通ることを確認するパイロットである。
残り 7 本は本パイロットの結果を見てから別指示書で行う。**先回りして他の View を触らないこと。**

## 1. なぜ EventDialogView を選ぶか

`docs/log.md` の記録どおり、`EventDialogView` は
`_titleText` / `_bodyText` / `_okButton` / `_gameFlowController` の 4 つが
**シーン上で未結線のまま放置されている**。つまり現時点で動いていない。

**壊れているものを移行するので、退行のリスクがない。** パイロットに最適である。

## 2. 触れてはならないファイル

1 行でも変更したら成果物ごと隔離される。

```
.github/workflows/**   .claude/**   .agents/rules/**
scripts/nightly_gate.py   scripts/morning_report.py
scripts/nightly_baseline.json   scripts/auto_runner.py
Game/Packages/manifest.json   Game/Packages/packages-lock.json
.mcp.json   Game/.mcp.json
```

加えて `00_rules.md` により、以下も**あなたは編集できない**:

- `.unity` / `.prefab` / `.asset` / `.meta` / `.asmdef`（人間が Unity エディタで行う）
- 新規 `.asmdef` の作成は violation

## 3. UXML の書き方（幻覚対策・必読）

`scripts/nightly_gate.py` に UXML lint が入っている。以下は **violation として隔離される**。

| 禁止 | 理由 |
|---|---|
| `<div>` `<span>` `<p>` `<a>` `<img>` `<input>` `<h1>` 等の HTML タグ | UI Toolkit のタグではない。Unity がパースできない |
| `<Image>` `<Text>` `<Toggle>` 等の uGUI 名称（`ui:` 無し） | 同上 |
| `style="display: block"` 等の CSS 固有プロパティ | USS は Flexbox のみ |
| `xmlns:ui="UnityEngine.UIElements"` の宣言が無い UXML | Unity がパースできない |

**使えるタグ**: `ui:UXML` / `ui:VisualElement` / `ui:Label` / `ui:Button` / `ui:Image` /
`ui:TextField` / `ui:ScrollView` / `ui:Toggle`（すべて `ui:` 接頭辞つき）

ファイルは必ずこのヘッダから始めること:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ui:UXML xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xmlns:ui="UnityEngine.UIElements">
</ui:UXML>
```

## 4. タスク

- [x] `Game/Assets/UI/UXML/EventDialog.uxml` を新規作成する。`EventDialogView.cs` が持つ表示要素（タイトル・本文・OK ボタン）に対応する `ui:Label` × 2 と `ui:Button` × 1 を、ルートの `ui:VisualElement` の下に配置すること。C# から参照する要素には必ず PascalCase の `name` 属性を付ける（`TitleLabel` / `BodyLabel` / `OkButton`）。§3 のヘッダから始めること。
- [x] `Game/Assets/UI/USS/EventDialog.uss` を新規作成し、最低限のレイアウト（中央寄せ・パネル背景・余白）を書く。**Flexbox のみを使うこと**（`display: block` や `float` は USS に存在しない）。既存 uGUI の見た目に厳密に合わせる必要はない。モックである。
- [x] `Game/Assets/UI/Scripts/EventDialogViewUI.cs` を新規作成する。**既存の `EventDialogView.cs` は削除も改変もしないこと**（比較対象として残す）。新クラスは `UIDocument` から `rootVisualElement.Q<Label>("TitleLabel")` の形で要素を解決し、`EventDialogView.cs` と同じイベント購読・表示ロジックを実装すること。`[SerializeField]` は `UIDocument` への参照 1 個と、イベントチャンネル SO 参照のみに留める。
- [x] 要素の解決に失敗した場合（`Q<T>()` が null）、`Debug.LogError` で**どの name が解決できなかったかを明示して**早期に落とすこと。黙って null のまま進むと、現在の未結線問題と同じ状態を再生産する。
- [x] ランタイムコードに `#if UNITY_EDITOR` / `AssetDatabase` / `UnityEditor` / `Find` 系を入れないこと（`00_rules.md`・ゲートで自動検出される）。

## 5. 人間に依頼する作業（あなたはやらない）

以下は `.unity` の編集にあたるため **人間が Unity エディタで行う**。
あなたは「何をしてほしいか」を完了報告に列挙するだけでよい。

- `EventDialogPanel` に `UIDocument` コンポーネントを追加し、`EventDialog.uxml` を割り当てる
- `EventDialogViewUI` コンポーネントを追加し、`UIDocument` とイベントチャンネル SO をアサインする

## 6. 検証（実コマンドの出力を報告に貼る）

| # | コマンド | 期待 |
|---|---|---|
| 1 | `dotnet build Game/Game.sln -v q --nologo` | **0 エラー**（所要 2〜3 秒） |
| 2 | `grep -c 'xmlns:ui' Game/Assets/UI/UXML/EventDialog.uxml` | `1` |
| 3 | 下記 Python（UXML lint 自己チェック） | `violations 0` |
| 4 | `git status --porcelain` | 保護対象・`.unity` が 1 件も現れない ★必須 |
| 5 | `git diff --stat` | 300 行未満 |

検証 3 のコマンド:

```bash
python -c "
import sys,pathlib; sys.path.insert(0,'scripts')
import nightly_gate as g
bad=[]
for p in pathlib.Path('Game/Assets').rglob('*.uxml'):
    t=p.read_text(encoding='utf-8')
    if not g.UXML_NAMESPACE.search(t): bad.append((str(p),'xmlns:ui 無し'))
    for line in t.splitlines():
        for pat,msg in g.UXML_RULES:
            if pat.search(line): bad.append((str(p),msg))
print('violations', len(bad)); [print(' -',b) for b in bad]
"
```

**`dotnet build` は 2〜3 秒で終わる。編集のたびに回すこと。**
Unity batchmode（30 分）を待つ必要はない。

## 7. 停止条件

以下に該当したら手を止めて報告する（`00_rules.md`「停止条件」）。

- `.unity` / `.prefab` / `.asset` を編集しないと先へ進めなくなった（人間の作業である）
- 保護対象ファイル（§2）を変更する必要が生じた
- パッケージの追加が必要だと判断した（`manifest.json` は保護対象。**追加は禁止**）
- 新しい `.asmdef` が必要だと判断した（**禁止**）
- 同じエラーの修正を 2 回試みて直らない
- `git diff --stat` が 300 行を超えた

## 8. 完了報告

`docs/workflow/TRIAD_PROTOCOL.md` §2-④ の形式に従う。

```
指示書: docs/instructions/13_uitoolkit_pilot.md
実施: Tasklist 完了（または N で停止）
--- コマンド出力 ---
（§6 の検証 1〜5 の生の出力をそのまま貼る）
---
人間に依頼する Unity エディタ作業:
（§5 の内容を具体的に列挙）
---
停止条件への抵触: なし / あり（内容）
```

コミットのみ行い、**push はしない**。

## 9. スコープ外

| 項目 | 理由 |
|---|---|
| 他の 7 View の移行 | 本パイロットの結果を見てから別指示書で行う |
| `EventDialogView.cs`（旧）の削除 | 比較対象として残す。撤去は移行完了後の判断 |
| `UILayoutBuilder.cs` の改修 | UI Toolkit 移行が全 View に及んだ時点で構造ごと見直す |
| PlayMode テストの追加 | 指示書 12 の結線検証と統合するため、そちらで扱う |
| 見た目の作り込み | モック段階。レイアウトが破綻していなければよい |
