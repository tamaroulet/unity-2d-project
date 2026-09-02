// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.Relic;
using UnityEngine;

namespace Game.Features.GameFlow
{
    /// <summary>
    /// ターン進行・状態遷移・Resolver と EventChannelSO のオーケストレーションを担う
    /// MonoBehaviour。自身はパラメータの計算・判定を行わず、GameRulesSO,
    /// CommandResolverSO, EventResolverSO, EndingResolverSO, RelicResolverSO および TurnRules への
    /// 処理の委譲のみを行う。状態変化・イベント発生・エンディング確定・レリック獲得は
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
        [SerializeField] private RelicCatalogSO _relicCatalog;
        [SerializeField] private RelicResolverSO _relicResolver;
        [SerializeField] private GameStateEventChannelSO _gameStateChannel;
        [SerializeField] private GameEventFiredChannelSO _eventFiredChannel;
        [SerializeField] private EndingDecidedChannelSO _endingDecidedChannel;
        [SerializeField] private RelicAcquiredChannelSO _relicAcquiredChannel;
        [SerializeField] private BossCatalogSO _bossCatalog;
        [SerializeField] private AutoBattleResolverSO _autoBattleResolver;
        [SerializeField] private int _bossBattleTurn = 12;

        private GamePhase _currentPhase = GamePhase.Initializing;
        private GameState _currentState;
        private readonly List<RelicSO> _activeRelics = new List<RelicSO>();

        public GamePhase CurrentPhase => _currentPhase;

        public GameState CurrentState => _currentState;

        public IReadOnlyList<RelicSO> ActiveRelics => _activeRelics;

        private void OnEnable()
        {
            if (_relicAcquiredChannel != null)
            {
                _relicAcquiredChannel.OnEventRaised += OnRelicAcquired;
            }
        }

        private void OnDisable()
        {
            if (_relicAcquiredChannel != null)
            {
                _relicAcquiredChannel.OnEventRaised -= OnRelicAcquired;
            }
        }

        /// <summary>
        /// GameRulesSO から初期状態を生成して通知し、最初のターンを開始する。
        /// </summary>
        public void StartGame()
        {
            _activeRelics.Clear();
            _currentPhase = GamePhase.Initializing;
            _currentState = _gameRules.CreateInitialState();
            _gameStateChannel.Raise(_currentState);

            BeginTurn();
        }

        /// <summary>
        /// コマンド選択時に呼ばれる。WaitingInput 中でなければ何もしない。
        /// 所持レリックによる効果修飾を適用したうえで CommandResolverSO へ委譲する。
        /// </summary>
        public void ExecuteCommand(CommandDataSO command)
        {
            if (_currentPhase != GamePhase.WaitingInput)
            {
                return;
            }

            _currentPhase = GamePhase.ExecutingCommand;

            CommandEffect effectiveEffect = _relicResolver != null
                ? _relicResolver.ModifyCommandEffect(command.Effect, _activeRelics)
                : command.Effect;

            CommandResult result = _commandResolver.Resolve(_currentState, effectiveEffect, _gameRules);

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
        /// ターン終了時パッシブ効果を適用し、TurnRules の終了判定に従って遷移先を決定する。
        /// </summary>
        public void AdvanceTurn()
        {
            if (_relicResolver != null && _activeRelics.Count > 0)
            {
                _currentState = _relicResolver.ApplyTurnEndRelics(_currentState, _activeRelics, _gameRules);
                _gameStateChannel.Raise(_currentState);
            }

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
        /// ターン開始時にパッシブ効果適用およびイベント発生を判定する。
        /// </summary>
        private void BeginTurn()
        {
            _currentPhase = GamePhase.TurnStart;

            if (_relicResolver != null && _activeRelics.Count > 0)
            {
                _currentState = _relicResolver.ApplyTurnStartRelics(_currentState, _activeRelics, _gameRules);
                _gameStateChannel.Raise(_currentState);
            }

            if (_autoBattleResolver != null && _bossCatalog != null && _currentState.CurrentTurn == _bossBattleTurn)
            {
                BossSO boss = _bossCatalog.FindById(1);
                if (boss != null)
                {
                    _currentPhase = GamePhase.BossBattle;
                    FullBattleResult battleResult = _autoBattleResolver.ResolveFullBattle(
                        _currentState, boss, _activeRelics, _gameRules);

                    _currentState = battleResult.FinalPlayerState;
                    _gameStateChannel.Raise(_currentState);

                    if (battleResult.Outcome == BattleOutcomeKind.Victory)
                    {
                        _currentPhase = GamePhase.ShowingRelicDraft;
                        return;
                    }
                    else
                    {
                        _currentPhase = GamePhase.GameOver;
                        return;
                    }
                }
            }

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
        /// イベントダイアログが閉じられたときに呼ばれる。
        /// </summary>
        public void OnEventDismissed()
        {
            _currentPhase = GamePhase.WaitingInput;
        }

        /// <summary>
        /// レリック獲得チャンネルから呼ばれるハンドラ。所持レリック一覧を更新して入力待ちへ復帰する。
        /// </summary>
        public void OnRelicAcquired(int relicId)
        {
            if (_relicCatalog != null)
            {
                RelicSO relic = _relicCatalog.FindById(relicId);
                if (relic != null && !_activeRelics.Contains(relic))
                {
                    _activeRelics.Add(relic);
                }
            }

            List<int> newIds = new List<int>(_currentState.AcquiredRelicIds);
            if (!newIds.Contains(relicId))
            {
                newIds.Add(relicId);
            }

            _currentState = _currentState with { AcquiredRelicIds = newIds };
            _gameStateChannel.Raise(_currentState);
            _currentPhase = GamePhase.WaitingInput;
        }
    }
}
