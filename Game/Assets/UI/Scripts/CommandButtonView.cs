// SPDX-AI-Disclosure: ai-generated
using Game.Features.Command;
using Game.Features.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// コマンド名・コストを表示し、押下時に GameFlowController.ExecuteCommand を
    /// 呼び出す薄いビュー。自身はコマンドの実行可否や効果を判定しない。
    /// </summary>
    public class CommandButtonView : MonoBehaviour
    {
        [SerializeField] private CommandDataSO _command;
        [SerializeField] private GameFlowController _gameFlowController;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;

        /// <summary>
        /// 表示中のコマンド名。TMP Essential Resources 未インポート環境では
        /// TextMeshProUGUI.text が空文字を返すため、テスト等の確認用に公開する。
        /// </summary>
        public string DisplayedName => _command != null ? _command.CommandName : string.Empty;

        /// <summary>
        /// 表示中のコマンドコスト。
        /// </summary>
        public int DisplayedCost => _command != null ? _command.Effect.StaminaCost : 0;

        private void OnEnable()
        {
            RefreshLabel();

            if (_button != null)
            {
                _button.onClick.AddListener(OnCommandClick);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnCommandClick);
            }
        }

        /// <summary>
        /// _command のフィールドからコマンド名とコストの表示を更新する。
        /// </summary>
        private void RefreshLabel()
        {
            if (_command == null)
            {
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = _command.CommandName;
            }

            if (_costText != null)
            {
                _costText.text = _command.Effect.StaminaCost.ToString();
            }
        }

        /// <summary>
        /// ボタン押下時に呼ばれ、GameFlowController へコマンドの実行を委譲する。
        /// </summary>
        private void OnCommandClick()
        {
            if (_command == null || _gameFlowController == null)
            {
                return;
            }

            _gameFlowController.ExecuteCommand(_command);
        }

        /// <summary>
        /// EditMode では Button.onClick.Invoke() が動的リスナーを発火しない場合があるため、
        /// テスト等から OnCommandClick を直接実行するための公開メソッド。
        /// </summary>
        public void Execute()
        {
            OnCommandClick();
        }
    }
}
