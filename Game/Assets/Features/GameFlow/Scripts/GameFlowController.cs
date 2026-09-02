// SPDX-AI-Disclosure: ai-generated
using Game.Core;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using UnityEngine;

namespace Game.Features.GameFlow
{
    /// <summary>
    /// ターン進行・状態遷移・Resolver と EventChannelSO のオーケストレーションを担う
    /// MonoBehaviour。自身はパラメータの計算・判定を行わず、GameRulesSO,
    /// CommandResolverSO, EventResolverSO, EndingResolverSO および TurnRules への
    /// 処理の委譲のみを行う。状態変化・イベント発生・エンディング確定は
    /// EventChannelSO 経由で通知する。
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameRulesSO _gameRules;
        [SerializeField] private CommandResolverSO _commandResolver;
        [SerializeField] private GameEventCatalogSO _eventCatalog;
        [SerializeField] private EventResolverSO _eventResolver;
        [SerializeField] private EndingRulesSO _endingRules;
        [SerializeField] private EndingResolverSO _endingResolver;
        [SerializeField] private GameStateEventChannelSO _gameStateChannel;
        [SerializeField] private GameEventFiredChannelSO _eventFiredChannel;
        [SerializeField] private EndingDecidedChannelSO _endingDecidedChannel;

        private GamePhase _currentPhase = GamePhase.Initializing;
        private GameState _currentState;

        public GamePhase CurrentPhase => _currentPhase;

        public GameState CurrentState => _currentState;

        /// <summary>
        /// GameRulesSO から初期状態を生成して通知し、最初のターンを開始する。
        /// </summary>
        public void StartGame()
        {
            _currentPhase = GamePhase.Initializing;
            _currentState = _gameRules.CreateInitialState();
            _gameStateChannel.Raise(_currentState);

            BeginTurn();
        }

        /// <summary>
        /// コマンド選択時に呼ばれる。WaitingInput 中でなければ何もしない。
        /// CommandResolverSO へ適用を委譲し、実行可能であれば状態を更新・通知したうえで
        /// AdvanceTurn によってターン終了後の遷移先を決定する。
        /// </summary>
        public void ExecuteCommand(CommandDataSO command)
        {
            if (_currentPhase != GamePhase.WaitingInput)
            {
                return;
            }

            _currentPhase = GamePhase.ExecutingCommand;

            CommandResult result = _commandResolver.Resolve(_currentState, command.Effect, _gameRules);

            if (!result.IsExecutable)
            {
                _currentPhase = GamePhase.WaitingInput;
                return;
            }

            _currentState = result.State;
            _gameStateChannel.Raise(_currentState);

            _currentPhase = GamePhase.TurnEnd;

            AdvanceTurn();
        }

        /// <summary>
        /// TurnRules の終了判定に従って、ターン終了後の遷移先を決定する。
        /// 続行なら次のターンを開始し、通常終了なら EndingResolverSO へ判定を委譲して
        /// エンディングを確定し、ゲームオーバーであればそのまま停止する。
        /// </summary>
        public void AdvanceTurn()
        {
            TerminationKind termination = TurnRules.EvaluateTermination(_currentState, _gameRules.MaxTurn);

            switch (termination)
            {
                case TerminationKind.GameOver:
                    _currentPhase = GamePhase.GameOver;
                    break;
                case TerminationKind.NormalEnd:
                    EndingKind ending = _endingResolver.Resolve(_currentState, _endingRules);
                    _endingDecidedChannel.Raise(ending);
                    _currentPhase = GamePhase.GameClear;
                    break;
                default:
                    BeginTurn();
                    break;
            }
        }

        /// <summary>
        /// ターン開始時にイベント発生を判定する。EventResolverSO へ判定を委譲し、
        /// イベントが発火した場合は状態と発火通知を行ったうえで入力待ちへ遷移する。
        /// </summary>
        private void BeginTurn()
        {
            _currentPhase = GamePhase.TurnStart;

            EventResult eventResult = _eventResolver.Resolve(_currentState, _eventCatalog);

            if (eventResult.HasFired)
            {
                _currentState = eventResult.State;
                _currentPhase = GamePhase.ShowingEvent;
                _gameStateChannel.Raise(_currentState);
                _eventFiredChannel.Raise(eventResult.FiredEvent.EventId);
            }
            else
            {
                _currentPhase = GamePhase.WaitingInput;
            }
        }

        /// <summary>
        /// イベントダイアログが閉じられたときに呼ばれる。ShowingEvent での表示待機を解除し、
        /// コマンド入力待ちへ遷移する。
        /// </summary>
        public void OnEventDismissed()
        {
            _currentPhase = GamePhase.WaitingInput;
        }
    }
}
