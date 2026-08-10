using System;
using Core.StateMachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.Character.Soldier
{
    /// <summary>
    /// Solider 유닛 Lifecycle 스크립트
    ///
    /// 주요 기능
    /// - 정해진 경로를 따라 순찰
    /// - Box 시야 범위 내 시체를 탐색,
    /// - 총기 발사 이벤트 수신, 피격 시 사망.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Soldier : MonoBehaviour
    {
        // 총기 발사음을 감지했을 때의 이벤트 델리게이트
        public static event Action GunshotHeard;

        // `FiringSystem`에 의해 호출. 유닛이 총격음 감지시 (천둥 소리가 안 날때 총을 쏘면) 호출됨.
        public static void RaiseGunshotHeard() => GunshotHeard?.Invoke();

        // 시야 내 '죽은 유닛'을 감지하기 위한 필터
        private static ContactFilter2D _deadUnitFilter;

        // Overlap 함수 결과를 저장하기 위한 임시 pool
        private static readonly Collider2D[] ColliderPool = new Collider2D[10];

        // 유니티의 static initializer
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _deadUnitFilter.SetLayerMask(LayerMask.GetMask("Hitbox_Dead"));
        }

        [FormerlySerializedAs("PatrolWaypoints")]
        [Header("Patrol")]
        [Tooltip("정찰 위치를 순차적으로 나타낸 배열. 마지막 요소(Z)는 다음 정찰 까지의 대기 시간. 순찰 규칙을 보장하기 위해 _bakedWaypoints로 빌드됨.")]
        public Waypoint[] patrolWaypoints;

        [Tooltip("FixedUpdate 호출시 이동할 픽셀 단위 거리")]
        public float moveVelocity = 0.45f;

        [Header("Refs")]

        [SerializeField]
        public BoxCollider2D spriteCollider; // Sprite 충돌체

        [SerializeField]
        public Collider2D sightCollider; // 시야 범위

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private GameObject alertSign;

        // 유닛의 생존 여부. 외부 수정 불가 (private set;)
        public bool IsAlive { get; private set; } = true;

        // 유닛 AI를 동작시키는 유한 상태 머신
        private AsyncStateMachine<ISoldierState, SoldierEvent, Soldier, SoldierStateBlackboard, SoldierStateCategory> _stateMachine;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            Waypoint[] bakedWaypoints;

            if (patrolWaypoints is { Length: >= 2 })
            {
                // 웨이포인트가 2개인 경우 두 위치만 반복해서 움직이면 됨
                // e.g. A->B => A->B->...
                if (patrolWaypoints.Length == 2)
                {
                    bakedWaypoints = patrolWaypoints;
                }
                else
                {
                    // 웨이포인트 x 2 - 2 만큼 배열을 생성하고 대칭으로 채움
                    // e.g. A->B->C->D => A->B->C->D->C->B->...
                    bakedWaypoints = new Waypoint[patrolWaypoints.Length * 2 - 2];
                    Array.Copy(patrolWaypoints, bakedWaypoints, patrolWaypoints.Length);

                    for (int i = 0; i < patrolWaypoints.Length - 2; i++)
                    {
                        bakedWaypoints[patrolWaypoints.Length + i] = patrolWaypoints[patrolWaypoints.Length - i - 2];
                    }
                }
            }
            else if (patrolWaypoints != null && patrolWaypoints.Length == 1) // 배열 길이가 1이면 똑같은 Waypoint를 설정하여 2로 늘림 (A => A -> A)
            {
                bakedWaypoints = new[] { patrolWaypoints[0], patrolWaypoints[0] };
            }
            else
            {
                // 순찰 경로가 지정되지 않았을 경우 에디터의 위치에 정지 (크래쉬 막기 위한 용도고 정상적인 초기화는 아님).
                bakedWaypoints = new Waypoint[]
                {
                    new(transform.position, 120),
                    new(transform.position, 120)
                };
            }

            var builder = AsyncStateMachine<ISoldierState, SoldierEvent, Soldier, SoldierStateBlackboard, SoldierStateCategory>.CreateBuilder(this);

            for (int i = 0; i < bakedWaypoints.Length; i++)
            {
                int nextIdx = (i + 1) % bakedWaypoints.Length;

                builder.AddState(new SoldierStateIdle($"Idle{i}", bakedWaypoints[i].standbyTime))
                    .AddState(
                        new SoldierStateWalk(
                            $"Walk{i}", moveVelocity, bakedWaypoints[i].point, bakedWaypoints[nextIdx].point
                        )
                    )
                    .Transition(SoldierEvent.StartMove, $"Idle{i}", $"Walk{i}")
                    .Transition(SoldierEvent.StopAndWait, $"Walk{i}", $"Idle{nextIdx}");
            }

            builder.AddState(new SoldierStateAlert("Alert", GameManager.Instance.MaxAlarmStandbyTime))
                .TransitionAny(SoldierEvent.Alert, "Alert")
                .InitState("Idle0");

            _stateMachine = builder.Build();
            _stateMachine.Run();
        }

        // 객체 활성화 시 호출되는 Lifecycle 이벤트 함수
        private void OnEnable()
        {
            GunshotHeard += OnGunshotHeard;
            GameManager.Instance?.Register(this);
        }

        // 객체 비활성화 시 호출되는 Lifecycle 이벤트 함수
        private void OnDisable()
        {
            GunshotHeard -= OnGunshotHeard;
            GameManager.Instance?.Unregister(this);
        }

        // 고정 시간마다 호출되는 Lifecycle 이벤트 함수
        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm is not null && gm.GameEnd) return;
            if (!IsAlive) return;

            // 유닛의 Vision 체크, 시야 내에 죽은 유닛이 있는가? (있으면 Alert 상태로)
            CheckVision();
        }

        public void FaceLeftOrRight(bool facingRight)
        {
            spriteRenderer.flipX = facingRight;
        }

        public void SetAlertSignActive(bool active)
        {
            alertSign?.SetActive(active);
        }

        private void CheckVision()
        {
            // 시야 방향에 따른 Collider Offset 설정
            sightCollider.offset = new Vector2(
                Math.Abs(sightCollider.offset.x) * (!spriteRenderer.flipX ? 1F : -1F),
                sightCollider.offset.y
            );

            int deadInSight = sightCollider.Overlap(_deadUnitFilter, ColliderPool);

            // 시야 내 죽은 유닛이 있으면 (충돌 테스트 결과가 0 초과면) Alert 상태로 돌입
            if (deadInSight > 0) _stateMachine.DispatchEvent(SoldierEvent.Alert);
        }

        // 이벤트 델리게이트용 함수. State를 현재 상태와 관련없이 Alert으로 만듬
        private void OnGunshotHeard()
        {
            if (IsAlive) _stateMachine.DispatchEvent(SoldierEvent.Alert);
        }

        /// <summary>
        /// 캐릭터 Kill. 메모리를 반환하는 개념이 아닌 오브젝트의 상태를 "죽음"으로 변경함
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;

            IsAlive = false;

            if (spriteCollider != null)
            {
                GameObject obj = transform.Find("SpriteCollider").gameObject;

                int layer = LayerMask.NameToLayer("Hitbox_Dead");
                obj.layer = layer;
            }

            alertSign?.SetActive(false);

            GameManager.Instance?.NotifySoldierDied();

            _stateMachine.Stop();
        }

        public SoldierStateCategory GetCurrentStateCategory() => _stateMachine.GetCurrentStateCategory();
    }
}