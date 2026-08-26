using Content.Character.Soldier;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace Content.System
{
    /// <summary>
    /// 게임 플레이 씬의 스코프. 마우스 입력으로 조준점과 배율을 조작함.
    ///
    /// 발사 처리(쿨다운 컨트롤, 피격 테스트, 피격 효과 데칼 생성, 효과음 출력, 천둥 소리에 가려지지
    /// 않았을 시 총소리 listen 이벤트 broadcast)도 담당.
    /// </summary>
    public sealed class StageScopeController : ScopeController
    {
        [Header("Input")]

        [Tooltip("Look(Vector2), Zoom(Axis), Fire(Button) 액션을 가진 에셋")]
        [SerializeField]
        private InputActionAsset inputActions;

        [Header("Refs")]

        [SerializeField]
        private WeatherController weather;

        [SerializeField]
        private Crack crackPrefab;

        [SerializeField]
        private Transform crackParent;

        [SerializeField]
        private AudioSource fireSource;

        [SerializeField]
        private AudioClip fireClip;

        [Header("Tuning")]

        [SerializeField]
        [Tooltip("마우스 delta 1픽셀 당 조준점 이동량 (배경 픽셀)")]
        private float lookSensitivity = 2.0F;

        [SerializeField]
        [Tooltip("휠 입력 1당 배율 변화량")]
        private float zoomSensitivity = 0.25F;

        [SerializeField]
        [Tooltip("발사 쿨다운 (frames)")]
        private int cooldownFrames = 120;

        // FixedUpdate 에서 카운트다운됨
        private int _fireCooldown;

        // Out buffer for collision result
        private static readonly Collider2D[] HitResults = new Collider2D[10];

        // Crack 데칼을 위한 오브젝트 풀
        private ObjectPool<Crack> _pool;

        private InputAction _look;
        private InputAction _zoom;
        private InputAction _fire;

        private void Awake()
        {
            _pool = new ObjectPool<Crack>(
                createFunc:      () => Instantiate(crackPrefab, crackParent),
                actionOnGet:     c => c.gameObject.SetActive(true),
                actionOnRelease: c => c.gameObject.SetActive(false),
                actionOnDestroy: c => Destroy(c.gameObject),
                collectionCheck: false,
                defaultCapacity: 8,
                maxSize:         32
            );

            Assert.NotNull(inputActions);

            var actionMap = inputActions.FindActionMap("Gameplay", true);
            if (actionMap == null) return;

            _look = actionMap.FindAction("Look", true);
            _zoom = actionMap.FindAction("Zoom", true);
            _fire = actionMap.FindAction("Fire", true);
        }

        private void OnEnable()
        {
            inputActions?.Enable();
            if (_fire != null) _fire.performed += OnFire;
        }

        private void OnDisable()
        {
            if (_fire != null) _fire.performed -= OnFire;
        }

        protected override void Update()
        {
            var gm = GameManager.Instance;

            // 게임 종료(미션 성공 또는 실패) 후에는 조준을 잠금
            if (gm == null || !gm.GameEnd)
            {
                if (_zoom != null) ZoomScope(_zoom.ReadValue<float>() * zoomSensitivity);
                if (_look != null) MoveFocalPoint(_look.ReadValue<Vector2>() * lookSensitivity);
            }

            base.Update();
        }

        private void FixedUpdate()
        {
            if (_fireCooldown > 0) _fireCooldown--;
        }

        private void OnFire(InputAction.CallbackContext _)
        {
            if (_fireCooldown != 0) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.GameEnd) return;

            Vector3 focalPoint = ScopeWorldFocalPoint;

            // 피격 테스트: 피격 지점이 Box Collider 2D와 겹친 유닛을 죽임,
            // HitResults(배열)에 결과를 받아오므로 한 발에 여러 유닛 kill 가능
            int count = Physics2D.OverlapPoint(focalPoint, ContactFilter2D.noFilter, HitResults);

            for (int i = 0; i < count; i++)
            {
                Soldier soldier = HitResults[i].GetComponentInParent<Soldier>();

                if (!soldier) continue;
                if (!soldier.IsAlive) continue;

                Vector2 p = soldier.transform.position;
                Vector2 half = soldier.spriteCollider.size * 0.5F;

                if (Mathf.Abs(p.x - focalPoint.x) < half.x && Mathf.Abs(p.y - focalPoint.y) < half.y)
                    soldier.Kill();
            }

            _fireCooldown = cooldownFrames;

            // 오브젝트 풀링: 크랙 오브젝트 재사용, 스코프 가운데 생성
            if (crackPrefab != null)
            {
                var crackDecal = _pool.Get();
                crackDecal.Play(focalPoint, _pool.Release);
            }

            // 사운드 재생
            if (fireSource != null && fireClip != null) fireSource.PlayOneShot(fireClip);

            // 천둥 외 시간에 총기 발사 -> Scene의 모든 유닛이 소리를 듣고 경계 상태 돌입
            if (weather == null || !weather.IsGunfireMasked)
            {
                Soldier.RaiseGunshotHeard();
            }
        }
    }
}