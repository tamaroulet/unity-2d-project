# GATE4-01: Hour 4-8「最小曳光弾 1 本」確定指示書

- 発行: Claude Opus（アーキテクト） / 2026-09-03
- 承認: 人間ディレクター（案A 承認済み・`Game.Tests.PlayMode.asmdef` 新設承認済み）
- 上位規約: `docs/workflow/TRIAD_PROTOCOL.md` §2-③ / `.agents/rules/00_rules.md`
- 実行者: **人間（§1）→ Gemini（§2 → §3）→ Gemini or 人間（§4）**
- **この指示書に列挙されていないファイルは、読んでよいが書き込んではならない。**

---

## 0. 目的と前提

### 0-1. このターンで証明すること（これ以外は証明しない）

`MainGame.unity` を実際にロードし、**実際の uGUI クリック経路**で STUDY を 1 回実行し、

1. `TURN 1 → TURN 2` へ進行する
2. ゲージ（BarFill）が GameState に追随して変動する
3. 実行中の Exception / Error が **0 件**

を **PlayMode テスト 1 本**で自動証明する。これが Gate 4 の下流検証の土台になる。

### 0-2. 触ってよいファイル（完全な列挙・これ以外は書き込み禁止）

| # | 絶対パス | 担当 | 操作 |
|---|---|---|---|
| 1 | `C:\dev\unity-2d-project\Game\Assets\Tests\PlayMode\Game.Tests.PlayMode.asmdef` | **人間** | 新規作成（Unity エディタ操作） |
| 2 | `C:\dev\unity-2d-project\.agents\rules\00_rules.md` | Gemini | L18-L19 の置換 + L35 の直後に 1 行追加 |
| 3 | `C:\dev\unity-2d-project\Game\Assets\Tests\PlayMode\SmokeTest.cs` | Gemini | 新規作成（全文貼り付け） |

`docs/log.md` への記録は **Claude が行う**。Gemini は触るな。

### 0-3. Gemini への強制事項

- `.claude/hooks/guard.js` が稼働中である。**`.asmdef` / `.unity` / `.meta` / `.asset` への書き込みはツールレベルで拒否される。**
  §1 の asmdef 作成を Gemini が代行しようとしても必ず失敗する。**回避策を探すな。** §1 は人間の作業である。
- §2 → §3 の順に実行し、**§3 の完了時点で必ず停止して §5 の書式で報告する。**
- テストが赤になった場合、**テストを緑にするためにプロダクションコード（`Game/Assets` 配下の `Editor/`・`Tests/` 以外の `.cs`）を一切変更してはならない。**
  赤は情報である。出力をそのまま貼って停止せよ（`00_rules.md`「テスト」節）。

### 0-4. 検証済みの前提（Claude が実機ファイルで確認済み・再確認不要）

| 項目 | 実測値 |
|---|---|
| Unity | `6000.3.23f1`（`Game/ProjectSettings/ProjectVersion.txt`） |
| Build Settings Scene List | `Assets/Scenes/MainGame.unity` のみ / enabled: 1 / index 0 |
| `GameFlowController` | `_autoStartOnPlay: 1`、シリアライズ 17 スロットすべてアサイン済み |
| `GameRules.asset` | `_startTurn: 1` / `_initialStamina: 100` / `_initialSkill: 0` / `_initialMental: 50` / `_paramMin: 0` / `_paramMax: 100` / `_maxTurn: 24` |
| `Study.asset` | `_staminaDelta: -10` / `_skillDelta: 5` / `_mentalDelta: 5` / `_staminaCost: 10` |
| イベント | `Event_MidExam` のみ。`_triggerTurn: 12` → **TURN 1・2 では発火しない** |
| ボス戦ターン | `6, 12, 18, 24` → **TURN 1・2 では発生しない** |
| ボタン階層 | `Canvas/CommandPanel/StudyButton`（他 `TrainButton` / `RestButton`）、いずれも `m_Interactable: 1` |
| **ゲージの実体** | `StatusView._staminaGauge` / `_skillGauge` / `_mentalGauge` は **すべて `fileID: 0`（未アサイン＝Slider はシーンに存在しない）**。ゲージは `_staminaBarFill` / `_skillBarFill` / `_mentalBarFill`（`Canvas/StatusPanel/{Stamina,Skill,Mental}Group/BarBg/BarFill` の `RectTransform`）で表現され、`anchorMax.x` が充填率になる。 |
| `EventSystem` | シーンに存在・`m_IsActive: 1` |

> ⚠ **`Slider.value` を検証に使ってはならない。** Slider はこのシーンに 1 つも存在しない。
> 検証対象は `RectTransform.anchorMax.x` である。§3 のコードはこれに従っている。

### 0-5. 期待される数値（§3 のアサーションの根拠）

STUDY は「コスト 10 を満たすか判定 → `_staminaDelta` 等を加算 → クランプ → ターン +1」である
（`CommandResolverSO.Resolve` は `StaminaCost` を別途減算しない）。

| | TURN 1（クリック前） | TURN 2（クリック後） | BarFill `anchorMax.x` |
|---|---|---|---|
| Turn | 1 | **2** | — |
| Stamina | 100 | **90** | 1.00 → **0.90** |
| Skill | 0 | **5** | 0.00 → **0.05** |
| Mental | 50 | **55** | 0.50 → **0.55** |
| TurnText | `TURN 1 / 24` | **`TURN 2 / 24`** | — |
| Phase | `WaitingInput` | **`WaitingInput`** | — |

---

## 1. 【人間・先行必須】`Game.Tests.PlayMode.asmdef` の新設

**この作業が終わるまで Gemini は §3 に着手できない。**
`Tests/PlayMode/` に asmdef が無いまま `SmokeTest.cs` を置くと、既存の
`Game.Tests.EditMode.asmdef`（`includePlatforms: ["Editor"]`）の配下に取り込まれ、
**EditMode テストとして登録されてシーンロードに失敗する。**

### 1-A. 手順（Unity エディタ）

1. Project ウィンドウで `Assets/Tests` を右クリック → `Create > Folder` → 名前を **`PlayMode`** にする。
   （結果: `Assets/Tests/PlayMode/`）
2. `Assets/Tests/PlayMode` を右クリック → **`Create > Assembly Definition`** を選び、
   名前を **`Game.Tests.PlayMode`** にする。
   （`Create > Testing > PlayMode Test Assembly Folder` は使わない。余分なサンプル `.cs` が生成されるため）
3. 生成された `Game.Tests.PlayMode.asmdef` を選択し、Inspector で 1-B を設定して **Apply** を押す。

### 1-B. Inspector 設定値

| 項目 | 値 |
|---|---|
| Name | `Game.Tests.PlayMode` |
| Root Namespace | `Game.Tests.PlayMode` |
| Allow unsafe code | オフ |
| **Auto Referenced** | **オフ** |
| **Override References** | **オン** |
| Assembly References（Override References 有効時に現れる） | `nunit.framework.dll` を追加 |
| Assembly Definition References | `Game` / `UnityEngine.TestRunner` / `UnityEditor.TestRunner` / `Unity.TextMeshPro` / `UnityEngine.UI` の 5 つ |
| **Platforms** | **`Any Platform`**（＝ `includePlatforms` を空にする。ここを `Editor` にすると PlayMode で走らない） |
| Define Constraints | `UNITY_INCLUDE_TESTS` |

### 1-C. 保存後に `.asmdef` がこの JSON と一致すること（人間による目視確認）

```json
{
    "name": "Game.Tests.PlayMode",
    "rootNamespace": "Game.Tests.PlayMode",
    "references": [
        "Game",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

（比較対象: 既存の `Game/Assets/Tests/Game.Tests.EditMode.asmdef` との差分は
`name` / `rootNamespace` / **`includePlatforms` が `["Editor"]` ではなく `[]`** の 3 点のみである）

### 1-D. 完了条件

- `Window > General > Test Runner` の **PlayMode** タブに `Game.Tests.PlayMode` が
  （テストが 0 件でも）アセンブリとして現れる。
- コンパイルエラー 0。
- **ここでコミットし、Gemini に §2 を許可する。**

---

## 2. 【Gemini】`.agents/rules/00_rules.md` の改訂

対象: `C:\dev\unity-2d-project\.agents\rules\00_rules.md`
**この 2 箇所以外は 1 文字も変更しない。**

### 2-A. L18-L19 の置換

**before（現行の L18-L19。この 2 行を丸ごと削除する）:**

```
- ランタイムアセンブリは `Game.asmdef` **1 つ**。機能ごとの asmdef 分割は禁止。
  例外は `Game.Editor.asmdef`（Editor 専用）と `Game.Tests.asmdef`（テスト）の 2 つのみ。
```

**after（同じ位置に、この 6 行を挿入する）:**

```
- ランタイムアセンブリは `Game.asmdef` **1 つ**。機能ごとの asmdef 分割は禁止。
  例外は次の 3 枚のみ（Editor 1 枚 + テスト 2 枚）。これ以外の asmdef 新設は禁止。
  - `Game.Editor.asmdef` — Editor 専用（`includePlatforms: ["Editor"]`）
  - `Game.Tests.EditMode.asmdef` — EditMode テスト（`includePlatforms: ["Editor"]`）
  - `Game.Tests.PlayMode.asmdef` — PlayMode テスト（`includePlatforms: []`）
  テスト用 asmdef は EditMode / PlayMode で必ず別フォルダに置く。
```

### 2-B. L35 の直後に 1 行追加

**before（現行の L35。この行自体は変更しない）:**

```
- 結合の正しさは PlayMode テスト（`[UnityTest]`）でのみ証明する。
```

**after（上の行の直後に、次の 1 行を挿入する）:**

```
- PlayMode テストは `Game/Assets/Tests/PlayMode/` 配下にのみ置く。EditMode テストは `Game/Assets/Tests/` 直下に置く。
```

### 2-C. 完了条件

```powershell
Select-String -Path "C:\dev\unity-2d-project\.agents\rules\00_rules.md" -Pattern "Game.Tests.PlayMode.asmdef","Game.Tests.EditMode.asmdef","Tests/PlayMode/"
```

上記が **3 行以上ヒット**すること。ヒットしたらコミットして §3 へ進む。

---

## 3. 【Gemini】PlayMode 曳光弾テストの実装

対象: `C:\dev\unity-2d-project\Game\Assets\Tests\PlayMode\SmokeTest.cs`（**新規作成**）

**以下を 1 文字も変えずに全文貼り付ける。** 別解・簡略化・リファクタは禁止。

```csharp
// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Features.GameFlow;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Gate 4 の曳光弾（tracer bullet）。MainGame シーンを実際にロードし、実際の uGUI
    /// クリック経路で STUDY コマンドを 1 回実行して、TURN 1 → 2 の進行・ゲージ
    /// (BarFill) の変動・例外 0 件を 1 本で証明する。
    ///
    /// このテストが赤になっても、緑にするためにプロダクションコードへ分岐・自己修復・
    /// Find 系の再導入を行ってはならない（00_rules.md「テスト」節）。出力をそのまま
    /// 貼って停止し、人間に報告すること。
    ///
    /// ゲージは Slider ではなく RectTransform.anchorMax.x で表現されている
    /// （StatusView._staminaGauge 等はシーン上で未アサインであり、Slider は存在しない）。
    /// </summary>
    public class SmokeTest
    {
        private const string SceneName = "MainGame";
        private const string StudyButtonName = "StudyButton";
        private const float TimeoutSeconds = 10f;
        private const float FillTolerance = 0.001f;

        private readonly List<string> _capturedFailures = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _capturedFailures.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _capturedFailures.Add($"[{type}] {condition}\n{stackTrace}");
            }
        }

        [UnityTest]
        public IEnumerator MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions()
        {
            // ---------- Arrange: MainGame をロードし、入力待ちまで進める ----------
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(
                load != null,
                $"シーン '{SceneName}' のロードを開始できなかった。EditorBuildSettings の Scene List を確認せよ。");

            while (!load.isDone)
            {
                yield return null;
            }

            // Awake / OnEnable / Start を確実に 1 巡させる
            yield return null;

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow != null, "GameFlowController が MainGame シーンに存在しない。");

            StatusView status = UnityEngine.Object.FindFirstObjectByType<StatusView>();
            Assert.IsTrue(status != null, "StatusView が MainGame シーンに存在しない。");

            yield return WaitForCondition(
                () => flow.CurrentPhase == GamePhase.WaitingInput,
                () => $"起動から {TimeoutSeconds} 秒以内に GamePhase.WaitingInput へ到達しなかった。"
                      + $" 現在の Phase = {flow.CurrentPhase}");

            GameState before = flow.CurrentState;
            Assert.IsTrue(before != null, "StartGame 後に GameFlowController.CurrentState が null。");
            Assert.AreEqual(1, before.CurrentTurn, "初期ターンが 1 ではない。");
            Assert.IsTrue(
                status.LastDisplayedState != null,
                "StatusView が初期 GameState を受信していない。GameStateChannel の購読経路が切れている。");

            RectTransform staminaBar = GetSerializedField<RectTransform>(status, "_staminaBarFill");
            RectTransform skillBar = GetSerializedField<RectTransform>(status, "_skillBarFill");
            RectTransform mentalBar = GetSerializedField<RectTransform>(status, "_mentalBarFill");
            TextMeshProUGUI turnText = GetSerializedField<TextMeshProUGUI>(status, "_turnText");

            float staminaFillBefore = staminaBar.anchorMax.x;
            float skillFillBefore = skillBar.anchorMax.x;
            float mentalFillBefore = mentalBar.anchorMax.x;

            CommandButtonView studyView = FindCommandButtonByName(StudyButtonName);
            Button studyButton = studyView.GetComponent<Button>();
            Assert.IsTrue(studyButton != null, $"'{StudyButtonName}' に Button コンポーネントが無い。");
            Assert.IsTrue(studyButton.IsInteractable(), $"'{StudyButtonName}' が interactable でない。");

            Debug.Log(
                $"[SmokeTest] BEFORE Turn={before.CurrentTurn} Stamina={before.Stamina}"
                + $" Skill={before.Skill} Mental={before.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillBefore:F3}/{skillFillBefore:F3}/{mentalFillBefore:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            // ---------- Act: 実際の uGUI クリック経路をエミュレートする ----------
            bool accepted = ExecuteEvents.Execute(
                studyButton.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(accepted, $"'{StudyButtonName}' が pointerClick を受理しなかった。");

            yield return WaitForCondition(
                () => flow.CurrentState != null && flow.CurrentState.CurrentTurn >= 2,
                () => $"クリックから {TimeoutSeconds} 秒以内に TURN が 2 へ進まなかった。"
                      + $" 現在の Turn = {(flow.CurrentState != null ? flow.CurrentState.CurrentTurn : -1)},"
                      + $" Phase = {flow.CurrentPhase}");

            // UI 反映を確定させるため 1 フレーム待つ
            yield return null;

            // ---------- Assert ----------
            GameState after = flow.CurrentState;
            GameState displayed = status.LastDisplayedState;

            float staminaFillAfter = staminaBar.anchorMax.x;
            float skillFillAfter = skillBar.anchorMax.x;
            float mentalFillAfter = mentalBar.anchorMax.x;

            Debug.Log(
                $"[SmokeTest] AFTER  Turn={after.CurrentTurn} Stamina={after.Stamina}"
                + $" Skill={after.Skill} Mental={after.Mental}"
                + $" Fill(Sta/Skl/Mnt)={staminaFillAfter:F3}/{skillFillAfter:F3}/{mentalFillAfter:F3}"
                + $" TurnText=\"{turnText.text}\" Phase={flow.CurrentPhase}");

            Assert.Multiple(() =>
            {
                // 1. ターン進行
                Assert.AreEqual(2, after.CurrentTurn, "TURN が 2 になっていない。");
                Assert.AreEqual(
                    GamePhase.WaitingInput, flow.CurrentPhase, "TURN 2 開始後に入力待ちへ戻っていない。");

                // 2. STUDY の効果（Stamina 100→90 / Skill 0→5 / Mental 50→55）
                Assert.AreEqual(90, after.Stamina, "Stamina が 90 でない。");
                Assert.AreEqual(5, after.Skill, "Skill が 5 でない。");
                Assert.AreEqual(55, after.Mental, "Mental が 55 でない。");

                // 3. GameStateChannel 経由で View まで届いていること
                Assert.IsTrue(displayed != null, "StatusView が GameState を受信していない。");
                Assert.AreEqual(2, displayed.CurrentTurn, "StatusView が受信した Turn が 2 でない。");
                Assert.AreEqual(90, displayed.Stamina, "StatusView が受信した Stamina が 90 でない。");
                Assert.AreEqual(5, displayed.Skill, "StatusView が受信した Skill が 5 でない。");
                Assert.AreEqual(55, displayed.Mental, "StatusView が受信した Mental が 55 でない。");

                // 4. ゲージ（BarFill の anchorMax.x）が state に追随して変動したこと
                Assert.AreEqual(0.90f, staminaFillAfter, FillTolerance, "Stamina ゲージが 0.90 でない。");
                Assert.AreEqual(0.05f, skillFillAfter, FillTolerance, "Skill ゲージが 0.05 でない。");
                Assert.AreEqual(0.55f, mentalFillAfter, FillTolerance, "Mental ゲージが 0.55 でない。");
                Assert.Less(staminaFillAfter, staminaFillBefore, "Stamina ゲージが減っていない。");
                Assert.Greater(skillFillAfter, skillFillBefore, "Skill ゲージが増えていない。");
                Assert.Greater(mentalFillAfter, mentalFillBefore, "Mental ゲージが増えていない。");

                // 5. 画面ラベル
                Assert.AreEqual("TURN 2 / 24", turnText.text, "TurnText の表示が更新されていない。");

                // 6. 例外 0 件
                Assert.IsEmpty(
                    _capturedFailures,
                    "実行中に Error / Exception / Assert ログが発生した:\n"
                    + string.Join("\n", _capturedFailures));
            });

            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// predicate が真になるまで毎フレーム待つ。TimeoutSeconds を超えたら失敗する。
        /// </summary>
        private static IEnumerator WaitForCondition(Func<bool> predicate, Func<string> timeoutMessage)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;

            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(timeoutMessage());
                }

                yield return null;
            }
        }

        /// <summary>
        /// GameObject 名で CommandButtonView を引く。テストコードでのシーン検索は
        /// 00_rules.md の禁止対象（ランタイムコード）ではない。
        /// </summary>
        private static CommandButtonView FindCommandButtonByName(string gameObjectName)
        {
            CommandButtonView[] views = UnityEngine.Object.FindObjectsByType<CommandButtonView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CommandButtonView view in views)
            {
                if (view.gameObject.name == gameObjectName)
                {
                    return view;
                }
            }

            string found = views.Length == 0
                ? "(なし)"
                : string.Join(", ", Array.ConvertAll(views, v => v.gameObject.name));
            Assert.Fail(
                $"CommandButtonView を持つ GameObject '{gameObjectName}' がシーンに見つからない。検出できたのは: {found}");
            return null;
        }

        /// <summary>
        /// [SerializeField] private フィールドの実体を読む。人間の Inspector アサインが
        /// 落ちている場合に、原因を名指しで失敗させるために使う。
        /// </summary>
        private static T GetSerializedField<T>(Component target, string fieldName) where T : UnityEngine.Object
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(
                field != null, $"{target.GetType().Name}.{fieldName} というフィールドが存在しない。");

            T value = field.GetValue(target) as T;
            UnityEngine.Object asObject = value;
            Assert.IsTrue(
                asObject != null,
                $"{target.GetType().Name}.{fieldName} が Inspector で未アサイン、または型が {typeof(T).Name} でない。"
                + " 人間のアサインを確認せよ。");

            return value;
        }
    }
}
```

### 3-A. 実装上の注意（Gemini が勝手に変えてはならない箇所）

| 箇所 | 理由 |
|---|---|
| `ExecuteEvents.Execute(..., ExecuteEvents.pointerClickHandler)` | `_button.onClick.Invoke()` や `CommandButtonView.Execute()` の直接呼び出しに**置き換えるな**。それでは uGUI の経路を通らず、曳光弾にならない。 |
| `RectTransform.anchorMax.x` | `Slider.value` に**置き換えるな**。このシーンに Slider は存在しない（§0-4）。 |
| `Assert.Multiple` | 個別 Assert に分解するな。1 回の実行で全項目の失敗を同時に採取するためにこの形にしている。 |
| リフレクションによる private フィールド読み出し | 「テストのために StatusView に public プロパティを足す」ことは**禁止**。プロダクションコードは変更しない。 |

### 3-B. 完了条件

- Unity のコンパイルエラー 0。
- Test Runner の **PlayMode** タブに
  `Game.Tests.PlayMode > Game.Tests.PlayMode > SmokeTest > MainGame_StudyButtonClick_AdvancesTurnFrom1To2_WithZeroExceptions`
  が 1 件現れる。
- §4 の検証コマンドを実行し、**出力を貼って停止する**（緑でも赤でも停止する）。

---

## 4. 検証コマンド

### 4-A.（推奨）Unity MCP 経由 — Unity エディタを開いたまま実行する

MCP for Unity（`com.coplaydev.unity-mcp` v10.0.0）の `testing` グループを使う。

```
# 1) 起動（job_id が即座に返る。同期実行ではない）
run_tests(
    mode="PlayMode",
    assemblyNames=["Game.Tests.PlayMode"],
    includeDetails=true,
    includeFailedTests=true
)

# 2) 1) が返した job_id をポーリングする。status が "running" の間は繰り返す
get_test_job(
    job_id="<1) が返した job_id>",
    includeDetails=true,
    includeFailedTests=true
)
```

- `run_tests` が `{"error": "tests_running"}` を返したら、5 秒待って再試行する。
  それでも詰まる場合のみ `run_tests(clear_stuck=true)` を **1 度だけ**実行してよい。2 度目は §5 の停止条件に抵触する。
- PlayMode 実行中はエディタが Play に入る。**この間 Unity を手動操作してはならない。**

### 4-B.（代替）Unity CLI バッチモード — **Unity エディタを完全に終了してから**実行する

エディタがプロジェクトを開いたままだと `Library` のロックで必ず失敗する。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" `
  -runTests `
  -batchmode `
  -projectPath "C:\dev\unity-2d-project\Game" `
  -testPlatform PlayMode `
  -assemblyNames Game.Tests.PlayMode `
  -testResults "C:\dev\unity-2d-project\logs\gate4_playmode_results.xml" `
  -logFile "C:\dev\unity-2d-project\logs\gate4_playmode_run.log"
Write-Output "ExitCode=$LASTEXITCODE"
```

- **`-nographics` は付けない。** uGUI / Canvas を伴う PlayMode テストで不安定になる。
- 終了後、結果を取り出す:

```powershell
Select-String -Path "C:\dev\unity-2d-project\logs\gate4_playmode_results.xml" -Pattern '<test-run '
Select-String -Path "C:\dev\unity-2d-project\logs\gate4_playmode_results.xml" -Pattern 'result="Failed"' -Context 0,6
Select-String -Path "C:\dev\unity-2d-project\logs\gate4_playmode_run.log" -Pattern '\[SmokeTest\]'
```

### 4-C. 期待される出力

- 4-A: `get_test_job` の `status` が `"completed"`、`passed = 1` / `failed = 0` / `skipped = 0`。
- 4-B: `ExitCode=0`、`<test-run ... total="1" passed="1" failed="0" ...>`。
- 両方共通で、ログ中に以下 2 行が出ていること（**値まで一致すること**）:

```
[SmokeTest] BEFORE Turn=1 Stamina=100 Skill=0 Mental=50 Fill(Sta/Skl/Mnt)=1.000/0.000/0.500 TurnText="TURN 1 / 24" Phase=WaitingInput
[SmokeTest] AFTER  Turn=2 Stamina=90 Skill=5 Mental=55 Fill(Sta/Skl/Mnt)=0.900/0.050/0.550 TurnText="TURN 2 / 24" Phase=WaitingInput
```

> `logs/` は `.gitignore` 済みである。結果 XML / ログはコミットされない。**コミットしようとするな。**

---

## 5. 停止条件と報告フォーマット

### 5-A. 停止条件（1 つでも該当したら即座に手を止める）

1. §1 の asmdef が未作成、または Test Runner の PlayMode タブに `Game.Tests.PlayMode` が現れない
2. `.asmdef` / `.unity` / `.asset` / `.meta` への書き込みが `guard.js` に拒否された
3. `SmokeTest.cs` が赤になった（**プロダクションコードを修正して緑にすることは禁止**）
4. 同一のエラーを 2 回修正して直らない
5. §0-2 の 3 ファイル以外を変更する必要が生じた
6. 1 タスクで 300 行超の変更、または 3 コミットに到達した
7. 指示書に書かれていない設計判断が必要になった

### 5-B. 報告フォーマット（**自然言語の進捗宣言は無効**）

```
指示書: docs/research/GATE4-01_PlayMode_smoke_instruction.md
実施: §2 / §3 完了（または §N で停止）
コミット: <hash1> <hash2>
--- コマンド出力 ---
（4-A の get_test_job の生 JSON、または 4-B の ExitCode と Select-String の生出力を
  一切要約せずそのまま貼る。[SmokeTest] BEFORE / AFTER の 2 行を必ず含めること）
---
停止条件への抵触: なし / あり（該当番号と内容）
```

「うまくいきました」「改善しました」「動作を確認しました」は報告として受理しない。
**コマンド出力とコミットハッシュのみが進捗である。**
