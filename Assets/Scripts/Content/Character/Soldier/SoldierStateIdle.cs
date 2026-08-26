using Core.StateMachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Character.Soldier
{
    // 군인이 정찰 지점에서 대기하는 상태
    public class SoldierStateIdle : ISoldierState
    {
        // 대기시간
        private readonly int _maxWaitCount;

        public SoldierStateIdle(string stateName, int maxWaitCount)
        {
            StateName = stateName;
            _maxWaitCount = maxWaitCount;
        }

        public async Awaitable<SoldierEvent> Enter(Soldier soldier, SoldierStateBlackboard blackboard, CancellationTokenHolder cts)
        {
            // 대기이므로 심플하게 DelayFrame 사용. (cts.Token 필수적으로 넘겨줘야함)
            await UniTask.DelayFrame(_maxWaitCount, PlayerLoopTiming.FixedUpdate, cts.Token);

            // 정상 종료: 지정된 대기 시간이 끝남 => 다음 정찰 포인트로 움직이기 시작
            return SoldierEvent.StartMove;
        }

        public string StateName { get; }

        public SoldierStateCategory GetStateCategory() => SoldierStateCategory.Idle;
    }
}