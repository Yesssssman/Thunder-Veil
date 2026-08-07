using Core.StateMachine;

namespace Content.Character.Soldier
{
    public class SoldierStateBlackboard : IStateMachineBlackboard
    {
        // 정찰 지점 보간용 프로퍼티
        public float Progression { get; set; }
    }
}