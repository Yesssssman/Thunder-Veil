namespace Core.StateMachine
{
    /// <summary>
    /// 비동기 상태 머신의 "공용 데이터"를 담고 있는 오브젝트.
    /// 상태 머신에 할당된 변수들은 상태 머신을 소유하고 있는
    /// MonoBehavior 스크립트에 할당된 값으로 취급할 수 있음.
    /// <p/>
    /// <see cref="IMachineState"/>를 상속받는 모든 상태 인스턴스들
    /// 에서 접근 가능하며 변수 읽기/수정 가능.
    /// </summary>
    public interface IStateMachineBlackboard
    {
    }
}