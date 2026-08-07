using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Core.StateMachine
{
    /// <summary>
    /// `UniTask`기반 비동기로 동작하는 상태 머신.
    ///
    /// 사용방법
    ///
    /// `Builder`를 통해 각 상태와 전이 이벤트를 정의하고 `Build()`를 호출해서
    /// 상태 머신을 만듦.
    ///
    /// `AsyncStateMachine.Run()`으로 상태 머신 구동 시작
    ///
    /// `AsyncStateMachine.Stop()`으로 상태 머신 구동 중지. 이후 `Run()`으로 재구동 가능.
    /// </summary>
    ///
    /// <typeparam name="TState">         State 인스턴스 타입    </typeparam>
    /// <typeparam name="TEvent">         Event 타입            </typeparam>
    /// <typeparam name="TOwner">         상태 머신의 소유자 타입 </typeparam>
    /// <typeparam name="TBlackboard">    블랙보드 타입          </typeparam>
    /// <typeparam name="TStateCategory"> 상태 카테고리          </typeparam>
    public sealed class AsyncStateMachine<TState, TEvent, TOwner, TBlackboard, TStateCategory>
        where TOwner : MonoBehaviour
        where TBlackboard : IStateMachineBlackboard, new()
        where TState : IMachineState<TOwner, TEvent, TBlackboard, TStateCategory>
    {
        // 상태 머신의 소유자
        private readonly TOwner _owner;

        // 상태 머신 변수들을 저장하는 블랙보드
        private readonly TBlackboard _blackboard;

        // 비동기 작업 종료를 위한 토큰. 활성화 된 Task는 유일하므로, 하나만 존재해도 됨.
        private readonly CancellationTokenHolder _cancellationTokenHolder = new();

        // 상태 목록
        private readonly Dictionary<string, TState> _states;

        // 상태 전이 목록 (이전 상태 => 이벤트 => 전이 후 상태)
        private readonly Dictionary<string, Dictionary<TEvent, string>> _transitions;

        // 전역 상태 전이 목록 (모든 상태 => 이벤트 => 전이 후 상태)
        // 상태별 전이, `_transitions`와 중복되는 경우 상태별 전이가 먼저 적용됨
        private readonly Dictionary<TEvent, string> _transitionsAny;

        // 현재 State
        private TState _currentState;

        // private: `Builder`를 사용하세요
        private AsyncStateMachine(
            TOwner owner,
            string initialStateName,
            Dictionary<string, TState> states,
            Dictionary<string, Dictionary<TEvent, string>> transitions,
            Dictionary<TEvent, string> transitionsAny
        ) {
            _owner = owner;
            _states = states;
            _transitions = transitions;
            _transitionsAny = transitionsAny;
            _blackboard = new TBlackboard();

            Assert.True(_states.ContainsKey(initialStateName), $"초기 상태명 {initialStateName}에 해당하는 상태가 없습니다.");

            _currentState = states[initialStateName];
        }

        /// <summary>
        /// 상태 머신 시작.
        /// </summary>
        public void Run()
        {
            ActivateState(_currentState).Forget();
        }

        /// <summary>
        /// 상태 머신 종료, 현재 상태와 블랙보드에 남은 정보는 유지됨.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenHolder.CancellationToken.Cancel();
        }

        /// <summary>
        /// Event 발생 — 현재 상태에서 다음 상태로 전이하는 Event가 정의되어 있을 경우 전이 발생
        /// </summary>
        ///
        /// <seealso cref="Builder.Transition"/>
        /// <seealso cref="Builder.TransitionAny"/>
        public void DispatchEvent(TEvent eventType)
        {
            if (_transitions.TryGetValue(_currentState.StateName, out var currentTransitions))
            {
                if (currentTransitions.TryGetValue(eventType, out string nextStateName))
                {
                    // 설계 결함—트랜지션에 정의된 상태가 없음.
                    Assert.True(_states.ContainsKey(nextStateName));
                    ActivateState(_states[nextStateName]).Forget();

                    return;
                }
            }

            if (_transitionsAny.TryGetValue(eventType, out string anyTransitNextStateName))
            {
                if (!_currentState.StateName.Equals(anyTransitNextStateName))
                {
                    ActivateState(_states[anyTransitNextStateName]).Forget();
                }
            }
        }

        /// <summary>
        /// 현재 상태의 `카테고리` 반환
        /// </summary>
        public TStateCategory GetCurrentStateCategory() => _currentState.GetStateCategory();

        private async UniTaskVoid ActivateState(TState state)
        {
            // 현재 비동기 작업 취소
            _cancellationTokenHolder.Refresh();
            _currentState = state;

            // 현재 State의 정상 종료까지 대기함—정상 종료된다면 State는 다음 상태로 전이할 Event를 반환
            TEvent endEvent = await _currentState.Enter(_owner, _blackboard, _cancellationTokenHolder);

            // Recursive한 함수 호출: Trigger => RunState로 반복적으로 돌다가 Transition이 없으면 종료됨
            DispatchEvent(endEvent);
        }

        public static Builder CreateBuilder(TOwner owner) => new (owner);

        /// <summary>
        /// 상태 머신을 정의하는 빌더
        /// </summary>
        public sealed class Builder
        {
            private readonly TOwner _owner;
            private string _initStateName;
            private readonly Dictionary<string, TState> _states = new ();
            private readonly Dictionary<string, Dictionary<TEvent, string>> _transitions = new ();
            private readonly Dictionary<TEvent, string> _transitionsAny = new ();

            public Builder(TOwner owner)
            {
                _owner = owner;
            }

            // 시작 상태 설정. 상태는 자동으로 추가되기 때문에 `State`를 호출하지 않아도 됨
            public Builder InitState(string stateName)
            {
                _initStateName = stateName;
                return this;
            }

            // 상태 추가
            public Builder AddState(TState state)
            {
                // 상태명이 같은 상태가 포함되어 있으면 안됨
                Assert.False(_states.ContainsKey(state.StateName),
                    $" 상태머신 빌드 에러: {state.StateName} 가 이미 존재합니다.");

                _states.Add(state.StateName, state);

                return this;
            }

            // 상태 전이 추가.
            public Builder Transition(TEvent eventType, string fromState, string toState)
            {
                if (!_transitions.ContainsKey(fromState))
                {
                    _transitions.Add(fromState, new Dictionary<TEvent, string>());
                }

                var transitions = _transitions[fromState];

                // 이벤트가 중복되면 안 됨
                Assert.False(transitions.ContainsKey(eventType),
                    $" 상태머신 빌드 에러: {fromState}에서 {eventType}로 인한 전이가 이미 선언되어 있습니다.");

                transitions.Add(eventType, toState);

                return this;
            }

            // 전역 상태 전이 추가. 현재 상태가 어떤 상태이든 `toState`로의 전이가 일어남
            public Builder TransitionAny(TEvent eventType, string toState)
            {
                // 이벤트가 중복되면 안 됨
                Assert.False(_transitionsAny.ContainsKey(eventType),
                    $" 상태머신 빌드 에러: {eventType}로 인한 전역 상태 전이가 이미 선언되어 있습니다.");

                _transitionsAny.Add(eventType, toState);

                return this;
            }

            public AsyncStateMachine<TState, TEvent, TOwner, TBlackboard, TStateCategory> Build()
            {
                return new AsyncStateMachine<TState, TEvent, TOwner, TBlackboard, TStateCategory>(
                    _owner, _initStateName, _states, _transitions, _transitionsAny);
            }
        }
    }
}