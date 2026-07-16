using System.Collections.Generic;

namespace ThunderVeil.Core
{
    /// <summary>
    /// 유한 상태 머신(FSM). 유니티 엔진에 독립적인 모듈
    ///
    /// 유니티 내장 FSM은 애니메이션 상태 전환만 지원
    ///
    /// 구현된 상태 머신은 모듈에 제약받지 않고 모든 상황에 사용 가능
    /// </summary>
    public class FiniteStateMachine<TState, TEvent>
    {
        /// <summary>
        /// 각 State별로 변환 가능한 상태를 정의한 Dictionary
        /// TState --> TEvent 발생 --> TState
        /// </summary>
        private readonly Dictionary<TState, Dictionary<TEvent, TState>> _transitions = new();

        /// <summary>
        /// 현재 상태 (외부 수정 불가능, private set;)
        /// </summary>
        public TState Current { get; private set; }

        /// <summary>
        /// 트랜지션 추가. 주로 construct시 호출되고, update도중에는 호출하지 않음. (Initializer)
        /// </summary>
        /// <param name="from">전환 이전 상태</param>
        /// <param name="evt">전환 이벤트 종류</param>
        /// <param name="to">전환 후 상태</param>
        public void AddTransition(TState from, TEvent evt, TState to)
        {
            // 이전 트랜지션 `from`에 대한 트랜지션이 미리 정의되어 있나 체크
            if (!_transitions.TryGetValue(from, out var map))
            {
                // 없으면 새로운 Dictionary를 할당하고 `_transitions`에 저장함
                map = new Dictionary<TEvent, TState>();
                _transitions[from] = map;
            }
            
            // evt -> to 매핑 (이벤트가 발생하면 어떤 상태로 넘어가는지)
            map[evt] = to;
        }

        public void SetState(TState state)
        {
            Current = state;
        }

        /// <summary>
        /// Follow the transition registered for (Current, evt) if one exists; otherwise no-op.
        /// Mirrors FiniteStateMachine::issueEvent.
        /// </summary>
        public void IssueEvent(TEvent evt)
        {
            if (_transitions.TryGetValue(Current, out var map) && map.TryGetValue(evt, out var next))
            {
                Current = next;
            }
        }
    }
}
