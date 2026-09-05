# 調査報告: イベントが 1 度も発火しない原因の特定 (cycle 2026-09-05-08)

指示書: `docs/cycles/2026-09-05-08/03-instruction.md`
性質: 調査のみ（コード修正なし、コミットにコード差分なし）

---

## 観測値

### 1. `GameEventCatalogSO` に登録された全イベントの一覧
`Game/Assets/Data/Events/GameEventCatalog.asset:15-16`:
```yaml
  _events:
  - {fileID: 11400000, guid: dabf53696171e8c4db76289e22c2217d, type: 2}
```
登録件数は **1 件のみ**。該当アセットの実値（`Game/Assets/Data/Events/Event_MidExam.asset`）:
```yaml
Game/Assets/Data/Events/Event_MidExam.asset:15:  _eventId: 1
Game/Assets/Data/Events/Event_MidExam.asset:16:  _displayName: Midterm Exam
Game/Assets/Data/Events/Event_MidExam.asset:18:  _triggerKind: 0
Game/Assets/Data/Events/Event_MidExam.asset:19:  _triggerTurn: 12
Game/Assets/Data/Events/Event_MidExam.asset:21:  _threshold: 0
Game/Assets/Data/Events/Event_MidExam.asset:22:  _priority: 1
```
- `EventId`: 1
- `DisplayName`: "Midterm Exam"
- `TriggerKind`: 0 (`EventTriggerKind.Turn`)
- **発火ターン (`_triggerTurn`)**: **12**
- `Threshold`: 0
- `Priority`: 1

---

### 2. ボス戦のターン
`Game/Assets/Scenes/MainGame.unity:10231`:
```yaml
Game/Assets/Scenes/MainGame.unity:10231:  _bossBattleTurns: 060000000c0000001200000018000000
```
`Game/Assets/Features/GameFlow/Scripts/GameFlowController.cs:43`:
```csharp
[SerializeField] private List<int> _bossBattleTurns = new List<int> { 6, 12, 18, 24 };
```
実値: **`{ 6, 12, 18, 24 }`**

---

### 3. 1 と 2 の重なり
- `Event_MidExam` の発火ターン: **12**
- ボス戦（Act 2 ボス）のターン: **12**
- **重なり**: **ターン 12 においてイベントとボス戦が完全に衝突している。**

---

### 4. `EventResolverSO.Resolve` が呼ばれた回数
1 ラン（ターン 1〜24）を通した実測生ログ:
```
[OBSERVATION] Calling EventResolver.Resolve at Turn 1
[OBSERVATION] Calling EventResolver.Resolve at Turn 2
[OBSERVATION] Calling EventResolver.Resolve at Turn 3
[OBSERVATION] Calling EventResolver.Resolve at Turn 4
[OBSERVATION] Calling EventResolver.Resolve at Turn 5
[OBSERVATION] Boss battle triggered at Turn 6 (actIndex=0), returning from BeginTurn without calling EventResolver
[OBSERVATION] Calling EventResolver.Resolve at Turn 7
[OBSERVATION] Calling EventResolver.Resolve at Turn 8
[OBSERVATION] Calling EventResolver.Resolve at Turn 9
[OBSERVATION] Calling EventResolver.Resolve at Turn 10
[OBSERVATION] Calling EventResolver.Resolve at Turn 11
[OBSERVATION] Boss battle triggered at Turn 12 (actIndex=1), returning from BeginTurn without calling EventResolver
[OBSERVATION] Calling EventResolver.Resolve at Turn 13
[OBSERVATION] Calling EventResolver.Resolve at Turn 14
[OBSERVATION] Calling EventResolver.Resolve at Turn 15
[OBSERVATION] Calling EventResolver.Resolve at Turn 16
[OBSERVATION] Calling EventResolver.Resolve at Turn 17
[OBSERVATION] Boss battle triggered at Turn 18 (actIndex=2), returning from BeginTurn without calling EventResolver
[OBSERVATION] Calling EventResolver.Resolve at Turn 19
[OBSERVATION] Calling EventResolver.Resolve at Turn 20
[OBSERVATION] Calling EventResolver.Resolve at Turn 21
[OBSERVATION] Calling EventResolver.Resolve at Turn 22
[OBSERVATION] Calling EventResolver.Resolve at Turn 23
[OBSERVATION] Boss battle triggered at Turn 24 (actIndex=3), returning from BeginTurn without calling EventResolver
```
- 1 ラン中の呼び出し総回数: **20 回**（Turn 1〜5, 7〜11, 13〜17, 19〜23）
- ボス戦ターン（Turn 6, 12, 18, 24）での呼び出し回数: **0 回**
- **イベントが設定されているターン 12 での呼び出し回数: 0 回**

---

### 5. そのうち `HasFired == true` になった回数
実測生ログ:
```
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 1
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 2
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 3
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 4
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 5
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 7
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 8
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 9
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 10
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 11
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 13
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 14
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 15
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 16
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 17
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 19
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 20
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 21
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 22
[OBSERVATION] EventResolver.Resolve returned HasFired=False at Turn 23
```
- `HasFired == true` になった回数: **0 回**
（カタログ内の唯一のイベント `Event_MidExam` の発火条件が `_triggerTurn == 12` のため、上記 20 回の呼び出しでは全て条件不一致で false）

---

### 6. `_eventResolver` / `_eventCatalog` がシーンで結線されているか
`docs/snapshot/scene_bindings.txt` 該当行生出力:
```
Canvas/UIViews  [active=true]
  EventDialogView
    _bodyText -> EventDescriptionText/TextMeshProUGUI (TextMeshProUGUI)
    _eventCatalog -> GameEventCatalog (GameEventCatalogSO)
    _eventFiredChannel -> EventFiredChannel (GameEventFiredChannelSO)
    _gameFlowController -> GameFlowController/GameFlowController (GameFlowController)
    _okButton -> OkButton/Button (Button)
    _panelRoot -> EventDialogPanel (GameObject)
    _titleText -> EventTitleText/TextMeshProUGUI (TextMeshProUGUI)
GameFlowController  [active=true]
  GameFlowController
    _eventCatalog -> GameEventCatalog (GameEventCatalogSO)
    _eventFiredChannel -> EventFiredChannel (GameEventFiredChannelSO)
    _eventResolver -> EventResolver (EventResolverSO)
```
- 結線状態: **完全結線（unbound 0 件）**

---

## どの枝に落ちたか

**「仮説 H が濃厚（発火ターン 12 において Resolve が 1 度も呼ばれていない）」**

### 根拠
1. `GameFlowController.BeginTurn()` のコード（`GameFlowController.cs:256-350`）:
   ```csharp
   int actIndex = _autoBattleResolver != null && _bossCatalog != null && _bossBattleTurns != null && _currentState.CurrentTurn >= 6
       ? _bossBattleTurns.IndexOf(_currentState.CurrentTurn)
       : -1;

   if (actIndex >= 0)
   {
       // ボス戦処理を実行
       ...
       return; // ← ここで無条件に return し、後続のイベント判定に到達しない
   }

   if (_eventResolver != null && _eventCatalog != null)
   {
       EventResult eventResult = _eventResolver.Resolve(_currentState, _eventCatalog);
       ...
   }
   ```
2. ボス戦ターン `{6, 12, 18, 24}` では `actIndex >= 0` となり、ボス戦処理を実行した後に必ず `return;` で関数を抜ける。
3. 登録されている唯一のイベント `Event_MidExam` は `_triggerTurn = 12` であるため、**発火すべきターン 12 ではボス戦判定の先行 return により `_eventResolver.Resolve` に到達すらしていない**。
4. 一方、`_eventResolver.Resolve` が呼ばれているターン（1〜5, 7〜11, 13〜17, 19〜23）では、ターン番号が 12 でないため `EventTriggerKind.Turn` の条件が成立せず `HasFired` は常に false となる。

---

## 観測中に気づいた他の事実

1. **カタログ内のイベント総数は 1 件のみ**:
   `GameEventCatalog.asset` の `_events` にアサインされているのは `Event_MidExam.asset` の 1 件のみであり、他のイベントアセットはプロジェクト内に存在しない。
2. **構造的制約**:
   現行の `GameFlowController.BeginTurn()` の実装では、ボス戦ターン `{6, 12, 18, 24}` に設定されたイベントはすべてボス戦の `return` により発火不能となる。イベントとボス戦を同一ターンに共存させる設計にするのか、あるいはイベント発火ターンをボス戦と被らないターン（例: ターン 10 や 11 など）にするのかは、アーキテクチャ・ゲームデザイン上の判断となる。
