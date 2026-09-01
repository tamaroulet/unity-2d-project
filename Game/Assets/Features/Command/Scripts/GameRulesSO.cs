using Game.Core;
using UnityEngine;

namespace Game.Features.Command
{
    /// <summary>
    /// パラメータ上下限、ターン範囲、初期値を保持するルール定義。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Command/GameRules", fileName = "GameRules")]
    public class GameRulesSO : ScriptableObject
    {
        [SerializeField] private int _paramMin;
        [SerializeField] private int _paramMax;
        [SerializeField] private int _initialStamina;
        [SerializeField] private int _initialSkill;
        [SerializeField] private int _initialMental;
        [SerializeField] private int _startTurn;
        [SerializeField] private int _maxTurn;

        public int ParamMin => _paramMin;

        public int ParamMax => _paramMax;

        public int InitialStamina => _initialStamina;

        public int InitialSkill => _initialSkill;

        public int InitialMental => _initialMental;

        public int StartTurn => _startTurn;

        public int MaxTurn => _maxTurn;

        /// <summary>
        /// StartTurn / InitialStamina / InitialSkill / InitialMental から
        /// 初期状態の GameState を組み立てて返す。
        /// </summary>
        public GameState CreateInitialState()
        {
            return new GameState
            {
                CurrentTurn = _startTurn,
                Stamina = _initialStamina,
                Skill = _initialSkill,
                Mental = _initialMental
            };
        }
    }
}
