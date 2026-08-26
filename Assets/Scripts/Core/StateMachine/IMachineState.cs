using UnityEngine;

namespace Core.StateMachine
{
    public interface IMachineState
    {
        /// <summary>
        /// 상태 머신에서 State를 식별하기 위한 Name.
        /// </summary>
        string StateName { get; }
    }

    /// <summary>
    /// 유한 상태 머신의 상태 (State).
    ///
    /// 실제 구현과 이 인터페이스에 타입 제한용 인터페이스를 하나 더 두는 것을 권장함.
    /// <p/>
    /// 예시
    /// <code>
    /// IMachineState -> ISoldierState -> SoliderIdleState
    ///                                -> SoliderWalkState
    ///                                -> SoliderAlertState
    /// </code>
    /// </summary>
    ///
    /// <typeparam name="TOwner">         소유자 타입   </typeparam>
    /// <typeparam name="TEvent">         이벤트 타입   </typeparam>
    /// <typeparam name="TBlackboard">    블랙보드 타입 </typeparam>
    /// <typeparam name="TStateCategory"> 카테고리 타입 </typeparam>
    public interface IMachineState<TOwner, TEvent, TBlackboard, TStateCategory> : IMachineState
        where TBlackboard : IStateMachineBlackboard
    {
        /// <summary>
        /// 상태 진입 시 호출. <see cref="AsyncStateMachine.Run()"/>에서
        /// 비동기로 처리해주므로, async-await 키워드로 현재 상태에서 수행할
        /// 작업을 비동기로 설정 가능함.
        /// </summary>
        ///
        /// <param name="owner">
        /// 유한 상태 머신 호출자. 상태를 가져오는데 사용 가능
        /// </param>
        /// <param name="blackboard">
        /// 상태 머신의 변수들을 저장하고 있는 블랙보드.
        /// <see cref="IStateMachineBlackboard"/> 참조.
        /// </param>
        /// <param name="cts">
        /// 종료 토큰. 상태 머신 일시정지나 이벤트 발생으로 인한 중간 종료를
        /// 위해 항상 UniTask생성 시 인자로 넘거야함.
        /// <see cref="CancellationTokenHolder"/> 참조.
        /// </param>
        ///
        /// <returns>
        /// 상태가 종료될 시 전이시킬 이벤트를 리턴함.
        /// </returns>
        Awaitable<TEvent> Enter(TOwner owner, TBlackboard blackboard, CancellationTokenHolder cts);

        /// <summary>
        /// 상태 카테고리를 반환함. 메타데이터 목적으로 사용.
        /// <p/>
        /// 예시: 상태에 따른 애니메이션 재생
        /// </summary>
        TStateCategory GetStateCategory();
    }
}