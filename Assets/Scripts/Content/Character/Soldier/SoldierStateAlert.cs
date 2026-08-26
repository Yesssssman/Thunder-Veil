using Core.StateMachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Content.Character.Soldier
{
    public class SoldierStateAlert : ISoldierState
    {
        // 경보 카운트
        private readonly int _alertCount;

        public SoldierStateAlert(string stateName, int alertCount)
        {
            StateName = stateName;
            _alertCount = alertCount;
        }

        public async Awaitable<SoldierEvent> Enter(Soldier owner, SoldierStateBlackboard blackboard, CancellationTokenHolder cts)
        {
            owner.SetAlertSignActive(true);

            // 경보 카운트만큼 대기
            await UniTask.DelayFrame(_alertCount, PlayerLoopTiming.FixedUpdate, cts.Token);

            // 정상 종료: 게임 패배
            GameManager.Instance.Defeated();

            // 게임이 끝났으므로 어떤 이벤트를 넘겨주던 상관 없음
            return SoldierEvent.StopAndWait;
        }

        public string StateName { get; }

        public SoldierStateCategory GetStateCategory() => SoldierStateCategory.Alarmed;
    }
}