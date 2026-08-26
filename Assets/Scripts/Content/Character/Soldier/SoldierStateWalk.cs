using System;
using Core.StateMachine;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Content.Character.Soldier
{
    // 군인이 다음 정찰 지점으로 이동하는 상태
    public class SoldierStateWalk : ISoldierState
    {
        // 정찰 시작 지점
        private readonly Vector2 _from;

        // 정찰 종료 지점
        private readonly Vector2 _to;

        // 두 정찰 지점간의 길이
        private readonly float _distance;

        // 1초당 이동 속도
        private readonly float _moveSpeed;

        public SoldierStateWalk(string stateName, float moveSpeed, Vector2 from, Vector2 to)
        {
            StateName = stateName;
            _from = from;
            _to = to;
            _moveSpeed = moveSpeed;

            // 두 지점 간 길이 설정 — 이동 속도는 거리에 관계없이 공평하게 적용됨
            _distance = Vector2.Distance(from, to);
            Assert.AreNotEqual(_distance, 0.0F); // Divide by zero 방지
        }

        // 다음 지점까지 _moveSpeed만큼 이동한다
        public async Awaitable<SoldierEvent> Enter(Soldier owner, SoldierStateBlackboard blackboard, CancellationTokenHolder cts)
        {
            blackboard.Progression = 0.0F;

            // 스프라이트 방향 설정
            float xDelta = _to.x - _from.x;

            if (Math.Abs(xDelta) > 1E-5F)
            {
                owner.FaceLeftOrRight(xDelta < 0.0F);
            }

            while (blackboard.Progression <= _distance)
            {
                // (moveSpeed x deltaTime)만큼 다음 위치로 이동
                blackboard.Progression += _moveSpeed * Time.fixedDeltaTime;

                float delta = Mathf.Clamp(blackboard.Progression / _distance, 0F, 1F);
                owner.transform.position = Vector2.Lerp(_from, _to, delta);

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cts.Token);
            }

            // 정상 종료: 목표 포인트에 도달함 => 지정된 시간만큼 대기
            return SoldierEvent.StopAndWait;
        }

        public string StateName { get; }

        public SoldierStateCategory GetStateCategory() => SoldierStateCategory.Move;
    }
}