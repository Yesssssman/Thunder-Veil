using Content.Character.Soldier;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace Content
{
    /// <summary>
    /// 총기 발사 액션 수행
    ///
    /// 쿨다운 컨트롤, 피격 효과 데칼 생성, 효과음 출력, 피격 테스트, 총소리 listen 이벤트 broadcast
    /// (천둥 소리에 가려지지 않을 시)
    /// </summary>
    public class FiringSystem : MonoBehaviour
    {
        [Header("Refs")]

        [SerializeField]
        private ScopeController scope;

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

        [Header("Tuning (ported constants)")]
        [SerializeField]
        private int cooldownFrames = 120;

        // FixedUpdate 에서 카운트다운됨
        public int FireCooldown { get; private set; }

        // Out buffer for collision result
        private static Collider2D[] _hitResults = new Collider2D[10];

        // Crack 데칼을 위한 오브젝트 풀
        private ObjectPool<Crack> _pool;

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
        }

        private InputAction _fire;

        private void Start()
        {
            // Subscribe here (not OnEnable): by Start all Awakes have run, so
            // GameManager.Instance and its actions are guaranteed to exist.
            _fire = GameManager.Instance != null ? GameManager.Instance.Fire : null;
            if (_fire != null) _fire.performed += OnFire;
        }

        private void OnEnable()
        {
            // Re-subscribe only on a later re-enable (first enable is handled by Start).
            if (_fire != null) _fire.performed += OnFire;
        }

        private void OnDisable()
        {
            if (_fire != null) _fire.performed -= OnFire;
        }

        private void FixedUpdate()
        {
            if (FireCooldown > 0) FireCooldown--;
        }

        private void OnFire(InputAction.CallbackContext _)
        {
            if (FireCooldown != 0 || scope == null) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.GameEnd) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 focalPoint = scope.ScopeWorldFocalPoint;

            // 피격 테스트: 피격 지점이 Box Collider 2D와 겹친 유닛을 죽임,
            // _hitResults(배열)에 결과를 받아오므로 한 발에 여러 유닛 kill 가능
            int count = Physics2D.OverlapPoint(focalPoint, ContactFilter2D.noFilter, _hitResults);

            for (int i = 0; i < count; i++)
            {
                Soldier soldier = _hitResults[i].GetComponentInParent<Soldier>();

                if (!soldier) continue;
                if (!soldier.IsAlive) continue;

                Vector2 p = soldier.transform.position;
                Vector2 half = soldier.spriteCollider.size * 0.5F;

                if (Mathf.Abs(p.x - focalPoint.x) < half.x && Mathf.Abs(p.y - focalPoint.y) < half.y)
                    soldier.Kill();
            }

            FireCooldown = cooldownFrames;

            // 오브젝트 풀링: 크랙 오브젝트 재사용, 스코프 가운데 생성
            if (crackPrefab != null)
            {
                var crackDecal = _pool.Get();
                crackDecal.Play(focalPoint, _pool.Release);
            }

            // 사운드 재생
            if (fireSource != null && fireClip != null) fireSource.PlayOneShot(fireClip);

            // 천둥 외 시간에 총기 발사 -> Scene의 모든 유닛이 소리를 들고 경계 상태 돌입
            if (weather == null || !weather.IsGunfireMasked)
                Soldier.RaiseGunshotHeard();
        }
    }
}