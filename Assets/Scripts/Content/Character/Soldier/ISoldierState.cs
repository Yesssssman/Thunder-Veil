using Core.StateMachine;

namespace Content.Character.Soldier
{
    /// <summary>
    /// 군인의 상태 인터페이스—다른 오브젝트의 State와 혼동하지 않기
    /// 위하여 모든 군인의 상태는 이 인터페이스를 상속해야 함.
    /// </summary>
    public interface ISoldierState : IMachineState<Soldier, SoldierEvent, SoldierStateBlackboard, SoldierStateCategory>
    {
    }
}