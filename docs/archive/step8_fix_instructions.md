# Step 8 View クラスの公開メソッド化とテストの確定修正指示

Unity EditMode においては PlayerLoop が常時動作していないため、`SetActive(true)` による `OnEnable()` の自動発火がテスト環境で不安定になる場合があります。
これを解消するため、各 View のイベントハンドラおよびバインド処理を明示的に呼び出せるように設計を整えます。

---

## 1. `Game/Assets/UI/Scripts/StatusView.cs`
- `OnGameStateChanged` メソッドを `public void OnGameStateChanged(GameState state)` に変更。
- `public void Bind(GameStateEventChannelSO channel)` を追加（`_gameStateChannel = channel; if (channel != null) channel.OnEventRaised += OnGameStateChanged;`）。

## 2. `Game/Assets/UI/Scripts/EventDialogView.cs`
- `OnEventFired` メソッドを `public void OnEventFired(int eventId)` に変更。
- `public void Bind(GameEventFiredChannelSO channel, GameEventCatalogSO catalog)` を追加。

## 3. `Game/Assets/UI/Scripts/EndingView.cs`
- `OnEndingDecided` メソッドを `public void OnEndingDecided(EndingKind ending)` に変更。
- `public void Bind(EndingDecidedChannelSO channel)` を追加。

## 4. `Game/Assets/Tests/UIViewTests.cs`
- `StatusView_OnGameStateChanged_UpdatesTextsAndGauges`:
  - `view.Bind(channel);` を呼んでから `channel.Raise(...)` を呼ぶ。
- `EventDialogView_OnEventFired_ShowsPanelWithMatchingEventContent`:
  - `view.Bind(channel, catalog);` を呼んでから `channel.Raise(3);` を呼ぶ。
- `EventDialogView_OnEventDismissed_HidesPanel`:
  - `view.Bind(channel, catalog);` を呼んでから `channel.Raise(3);` を呼び、`view.Dismiss();` を呼ぶ。
- `EndingView_OnEndingDecided_ShowsPanelWithResultText`:
  - `view.Bind(channel);` を呼んでから `channel.Raise(EndingKind.Skill);` を呼ぶ。
