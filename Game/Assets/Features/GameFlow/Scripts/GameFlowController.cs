// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using UnityEngine;

namespace Game.Features.GameFlow
{
    /// <summary>
    /// ターン進行・状態遷移・Resolver と EventChannelSO のオーケストレーションを担う
    /// MonoBehaviour。自身はパラメータの計算・判定を行わず、GameRulesSO,
    /// CommandResolverSO, EventResolverSO, EndingResolverSO, RelicResolverSO,
    /// AutoBattleResolverSO, MetaPointResolverSO および TurnRules への
    /// 処理の委譲のみを行う。
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
        [SerializeField] private MetaPointResolverSO _metaPointResolver;
        [SerializeField] private MetaUnlockCatalogSO _metaUnlockCatalog;

        private GamePhase _currentPhase = GamePhase.Initializing;
        private GameState _currentState;
        private readonly List<RelicSO> _activeRelics = new List<RelicSO>();
        private MetaProfileState _metaProfile = new MetaProfileState();
        private int _bossDefeatedCount = 0;

        public GamePhase CurrentPhase => _currentPhase;

        public GameState CurrentState => _currentState;

        public IReadOnlyList<RelicSO> ActiveRelics => _activeRelics;

        public MetaProfileState MetaProfile => _metaProfile;

        public int BossDefeatedCount => _bossDefeatedCount;

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
        /// アンロック済みの初期ステータス底上げがあれば適用する。
        /// </summary>
        public void StartGame()
        {
            _activeRelics.Clear();
            _bossDefeatedCount = 0;
            _currentPhase = GamePhase.Initializing;

            GameState baseState = _gameRules.CreateInitialState();
            _currentState = _metaPointResolver != null && _metaUnlockCatalog != null
                ? _metaPointResolver.ApplyUnlockedStatBonuses(baseState, _metaProfile, _metaUnlockCatalog, _gameRules)
                : baseState;

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
                    FinalizeRun(isGameClear: false);
                    break;
                case TerminationKind.NormalEnd:
                    EndingKind ending = _endingResolver.Resolve(_currentState, _endingRules);
                    _endingDecidedChannel.Raise(ending);
                    _currentPhase = GamePhase.GameClear;
                    FinalizeRun(isGameClear: true);
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
                        _bossDefeatedCount++;
                        _currentPhase = GamePhase.ShowingRelicDraft;
                        return;
                    }
                    else
                    {
                        _currentPhase = GamePhase.GameOver;
                        FinalizeRun(isGameClear: false);
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

        private void FinalizeRun(bool isGameClear)
        {
            if (_metaPointResolver != null)
            {
                int earnedPoints = _metaPointResolver.CalculateEarnedPoints(_currentState, isGameClear, _bossDefeatedCount);
                _metaProfile = _metaPointResolver.ApplyRunResult(_metaProfile, earnedPoints);
            }
        }

        /// <summary>
        /// アンロックの購入を試みる。成功時はプロフィールを更新して true を返す。
        /// </summary>
        public bool TryPurchaseMetaUnlock(MetaUnlockSO unlock)
        {
            if (_metaPointResolver == null || unlock == null)
            {
                return false;
            }

            (MetaProfileState newProfile, bool success) = _metaPointResolver.ApplyUnlock(_metaProfile, unlock);
            if (success)
            {
                _metaProfile = newProfile;
            }
            return success;
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
