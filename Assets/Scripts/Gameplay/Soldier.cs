using System;
using ThunderVeil.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// Solider 유닛의 Lifecycle 스크립트 – 정해진 경로를 따라 순찰하며, Box 시야 범위 내 시체를 탐색,
    /// 총기 발사 이벤트 수신, 저격으로 인한 피격 시 사망.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Soldier : MonoBehaviour
    {
        public enum State { Undefined = 0, Idle, Move, Alert }
        private enum Event { Undefined = 0, Stop, Move, FindDead, HearShooting }

        // 총기 발사음을 감지했을 때의 이벤트 델리게이트
        public static event Action GunshotHeard;

        // `FiringSystem`에 의해 호출. 유닛이 총격음 감지시 (천둥 소리가 안 날때 총을 쏘면) 호출됨.
        public static void RaiseGunshotHeard() => GunshotHeard?.Invoke();

        // 유닛의 정찰 포인트를 정의한 데이터 클래스
        [Serializable]
        public struct Waypoint : IEquatable<Waypoint>
        {
            [FormerlySerializedAs("Point")] [Tooltip("정찰 위치")]
            public Vector2 point;

            [FormerlySerializedAs("StandbyTime")] [Tooltip("정찰 대기 시간 (Fixed tick)")]
            public int standbyTime;

            public Waypoint(in Vector2 point, in int standbyTime)
            {
                this.point = point;
                this.standbyTime = standbyTime;
            }

            bool IEquatable<Waypoint>.Equals(Waypoint other)
            {
                return Equals(other);
            }

            public override bool Equals(object other)
            {
                return other is Waypoint oWaypoint
                       && point.Equals(oWaypoint.point)
                       && standbyTime == oWaypoint.standbyTime;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(point, standbyTime);
            }

            public static bool operator ==(in Waypoint left, in Waypoint right)
            {
                return left.point == right.point && left.standbyTime == right.standbyTime;
            }

            public static bool operator !=(in Waypoint left, in Waypoint right)
            {
                return !(left == right);
            }
        }

        [FormerlySerializedAs("pathPoints")]
        [Header("Patrol")]
        [Tooltip("정찰 위치를 순차적으로 나타낸 배열. 마지막 요소(Z)는 다음 정찰 까지의 대기 시간")]
        public Waypoint[] PatrolWaypoints;

        [Tooltip("FixedUpdate 호출시 이동할 픽셀 단위 거리")]
        public float moveVelocity = 0.45f;

        [Header("Refs")]

        [SerializeField]
        public BoxCollider2D spriteCollider; // Sprite 충돌체

        [SerializeField]
        public BoxCollider2D sightCollider; // 시야 범위

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private GameObject alertSign;

        // 유닛의 생존 여부. 외부 수정 불가 (private set;)
        public bool IsAlive { get; private set; } = true;

        // Sprite의 방향 계산용 Computed property
        public bool FacingRight => !spriteRenderer.flipX;

        // 유한 상태 머신 상태 반환용 Computed property
        public State Current => _fsm.Current;

        // 유한 상태 머신을 기반으로 동작하는 AI
        private FiniteStateMachine<State, Event> _fsm;

        // ******* private fields *******

        private Waypoint[] _bakedWaypoints;   // 빌드된 정찰 경로 (에디터 상의 경로는 단방향, 빌드된 경로는 루핑을 위하여 양방향으로 동작.)
        private float[] _intervalLengths;     // [N] ~ [N+1] 경로 까지의 길이
        private int _pathIndex;               // 현재 경로의 인덱스
        private float _pathProgression;       // 현재 경로의 진행도 (최대값 == _intervalLengths[_pathIndex])
        private int _standbyTimer;            // 대기시간 타이머 (최대값 == _bakedWaypoints[_pathIndex].StandbyTime)
        private int _alertTimer;              // 경고시간 타이머. 0이되면 게임 패배

        private int _layerMaskDead;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            BuildPath();

            // 초기화
            _standbyTimer = _bakedWaypoints[0].standbyTime;
            _pathIndex = 0;
            _alertTimer = 0;
            _pathProgression = 0;

            _fsm = new FiniteStateMachine<State, Event>();
            _fsm.AddTransition(State.Idle, Event.Move, State.Move);
            _fsm.AddTransition(State.Idle, Event.FindDead, State.Alert);
            _fsm.AddTransition(State.Idle, Event.HearShooting, State.Alert);
            _fsm.AddTransition(State.Move, Event.Stop, State.Idle);
            _fsm.AddTransition(State.Move, Event.FindDead, State.Alert);
            _fsm.AddTransition(State.Move, Event.HearShooting, State.Alert);
            _fsm.SetState(State.Idle);

            // spriteCollider는 초기에 비활성화 (죽었을 때만 활성화되며 다른 유닛의 시야에 감지됨)
            if (spriteCollider != null)
            {
                spriteCollider.enabled = false;
            }

            _layerMaskDead = LayerMask.GetMask("Hitbox_Dead");
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

        // 이벤트 델리게이트용 함수. State를 현재 상태와 관련없이 Alert으로 만듬
        private void OnGunshotHeard()
        {
            if (IsAlive) _fsm.IssueEvent(Event.HearShooting);
        }

        // 고정 시간마다 호출되는 Lifecycle 이벤트 함수
        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm is not null && gm.GameEnd) return;
            if (!IsAlive) return;

            // 유닛의 Vision 체크, 시야 내에 죽은 유닛이 있는가? (있으면 Alert 상태로)
            CheckVision();

            switch (_fsm.Current)
            {
                case State.Idle: // 대기 상태
                    if (--_standbyTimer <= 0) // 대기 타이머 체크
                    {
                        // 현재 위치에서 다음 Waypoint로 움직이는 방향 계산
                        float xDelta = _bakedWaypoints[(_pathIndex + 1) % _bakedWaypoints.Length].point.x - _bakedWaypoints[_pathIndex].point.x;

                        // 가로로 움직이는 경우만 방향 변경. 세로로 움직이는 경우, 현재 방향 그대로 사용.
                        if (Math.Abs(xDelta) > 1E-5F)
                        {
                            spriteRenderer.flipX = xDelta < 0.0F;
                        }

                        // Move 이벤트 issue. (다음 상태는 Move)
                        _fsm.IssueEvent(Event.Move);
                    }
                    break;

                case State.Move: // 이동 상태
                    _pathProgression += moveVelocity;

                    // Move 도중 목표 정찰 지점에 다다름. 다시 Idle로 전환.
                    if (_intervalLengths[_pathIndex] <= _pathProgression)
                    {
                        // Path 인덱스 카운트 (+1).
                        _pathIndex = (_pathIndex + 1) % _bakedWaypoints.Length;
                        _pathProgression = 0.0F;
                        _standbyTimer = _bakedWaypoints[_pathIndex].standbyTime;

                        // Stop 이벤트 issue. (다음 상태는 Idle)
                        _fsm.IssueEvent(Event.Stop);
                    }
                    break;

                case State.Alert:
                    if (++_alertTimer > gm.MaxAlarmStandbyTime) gm?.Defeated(); // 알람 타이머 경과 – 패배
                    break;
            }

            // position 샘플링
            transform.position = SamplePath(_pathProgression);
            alertSign?.SetActive(_fsm.Current == State.Alert);
        }

        private void CheckVision()
        {
            // 시야 방향에 따른 Collider Offset 설정
            sightCollider.offset = new Vector2(
                Math.Abs(sightCollider.offset.x) * (FacingRight ? 1F : -1F),
                sightCollider.offset.y
            );

            // size/offset은 콜라이더 로컬 값이므로 `lossyScale`을 반영해 월드 공간 사각형으로 질의해야
            // 에디터에 보이는 콜라이더(및 디버그 오버레이)와 실제 감지 범위가 일치한다.
            Vector2 scale = sightCollider.transform.lossyScale;
            Vector2 centerWorld = (Vector2)sightCollider.transform.position + Vector2.Scale(sightCollider.offset, scale);
            Vector2 worldSize = Vector2.Scale(sightCollider.size, scale);
            var hit = Physics2D.OverlapBox(centerWorld, worldSize, 0f, _layerMaskDead);
            if (hit is not null) _fsm.IssueEvent(Event.FindDead);
        }

        // Bake된 순찰 경로 배열 생성
        private void BuildPath()
        {
            if (PatrolWaypoints != null && PatrolWaypoints.Length >= 2)
            {
                // 웨이포인트가 2개인 경우 두 위치만 반복해서 움직이면 됨
                // e.g. A->B => A->B->...
                if (PatrolWaypoints.Length == 2)
                {
                    _bakedWaypoints = PatrolWaypoints;
                }
                else
                {
                    // 웨이포인트 x 2 - 2 만큼 배열을 생성하고 대칭으로 채움
                    // e.g. A->B->C->D => A->B->C->D->C->B->...
                    _bakedWaypoints = new Waypoint[PatrolWaypoints.Length * 2 - 2];
                    Array.Copy(PatrolWaypoints, _bakedWaypoints, PatrolWaypoints.Length);

                    for (int i = 0; i < PatrolWaypoints.Length - 2; i++)
                    {
                        _bakedWaypoints[PatrolWaypoints.Length + i] = PatrolWaypoints[PatrolWaypoints.Length - i - 2];
                    }
                }
            }
            else if (PatrolWaypoints != null && PatrolWaypoints.Length == 1) // 배열 길이가 1이면 똑같은 Waypoint를 설정하여 2로 늘림 (A => A -> A)
                _bakedWaypoints = new[] { PatrolWaypoints[0], PatrolWaypoints[0] };
            else
            {
                // 순찰 경로가 지정되지 않았을 경우 에디터의 위치에 정지 (크래쉬 막기 위한 용도고 정상적인 초기화는 아님).
                _bakedWaypoints = new Waypoint[]
                {
                    new(transform.position, 120),
                    new(transform.position, 120)
                };
            }

            // 경로 길이를 계산해서 미리 저장해놓음. 나중에 두 위치 사이를 보간할 때 사용.
            _intervalLengths = new float[_bakedWaypoints.Length];

            for (int i = 0; i < _bakedWaypoints.Length; i++)
            {
                _intervalLengths[i] = Vector2.Distance(
                    _bakedWaypoints[i].point,
                    _bakedWaypoints[(i + 1) % _bakedWaypoints.Length].point
                );
            }
        }

        // progression / pathLength 값으로 캐릭터의 XY 벡터 위치를 보간함
        private Vector2 SamplePath(float dist)
        {
            if (dist <= 0.0001f) return _bakedWaypoints[_pathIndex].point;
            float maxLength = _intervalLengths[_pathIndex];
            float delta = Mathf.Clamp(dist / maxLength, 0F, 1F);
            return Vector2.Lerp(_bakedWaypoints[_pathIndex].point, _bakedWaypoints[(_pathIndex + 1) % _bakedWaypoints.Length].point, delta);
        }

        /// <summary>
        /// 캐릭터 Kill. (메모리를 반환하는 Kill이 아닌 오브젝트의 상태를 "죽음"으로 변경)
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;

            IsAlive = false;

            if (spriteCollider != null)
            {
                spriteCollider.enabled = true; // 사망시 Sprite Collider 활성화
                gameObject.layer = LayerMask.NameToLayer("Hitbox_Dead");
            }

            if (alertSign != null) alertSign.SetActive(false); // Alert sign 비활성화

            GameManager.Instance?.NotifySoldierDied();
        }
    }
}
