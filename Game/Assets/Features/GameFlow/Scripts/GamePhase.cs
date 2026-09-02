// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.GameFlow
{
    /// <summary>
    /// GameFlowController が管理するゲーム進行の状態を表す。
    /// </summary>
    public enum GamePhase
    {
        Initializing,
        TurnStart,
        ShowingEvent,
        WaitingInput,
        ExecutingCommand,
        TurnEnd,
        GameOver,
        GameClear
    }
}
