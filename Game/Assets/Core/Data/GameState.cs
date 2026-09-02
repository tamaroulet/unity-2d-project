// SPDX-AI-Disclosure: ai-generated
namespace Game.Core
{
    /// <summary>
    /// ターン制育成シミュレーションの、シリアライズしない実行時の不変状態。
    /// 状態変化は with 式による新しいインスタンスの生成で表現する。
    /// </summary>
    public record GameState
    {
        public int CurrentTurn { get; init; }

        public int Stamina { get; init; }

        public int Skill { get; init; }

        public int Mental { get; init; }
    }
}
