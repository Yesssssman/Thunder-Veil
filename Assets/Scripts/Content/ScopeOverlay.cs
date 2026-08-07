using UnityEngine;

namespace Content
{
    /// <summary>
    /// 스코프 비네팅을 구현한 포스트 프로세싱 셰이더
    /// </summary>
    public class ScopeOverlay : MonoBehaviour
    {
        [Header("Reveal (fraction of screen height)")]
        [SerializeField]
        private float innerRadius;

        [SerializeField]
        private float outerRadius;

        [Tooltip("World-space scope object the hole follows. Auto-finds ScreenLayer/ScopeReticle if empty.")]
        [SerializeField]
        private Transform scopeTarget;

        // 셰이더 Uniform IDs
        private static readonly int PCenter = Shader.PropertyToID("_ScopeCenter");
        private static readonly int PInner = Shader.PropertyToID("_InnerRadius");
        private static readonly int POuter = Shader.PropertyToID("_OuterRadius");
        private static readonly int PAspect = Shader.PropertyToID("_Aspect");

        // 머티리얼
        private Material _mat;
        // 메쉬 Renderer
        private MeshRenderer _mr;
        // 메인 카메라
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;

            if (scopeTarget == null)
            {
                var sl = GameObject.Find("ScreenLayer");
                if (sl != null)
                {
                    var r = sl.transform.Find("ScopeReticle");
                    if (r != null) scopeTarget = r;
                }
            }

            BuildQuad();
        }

        private void BuildQuad()
        {
            // 메쉬 생성
            var mesh = new Mesh
            {
                // 버텍스 (전체화면)
                vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                    new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f)
                },
                // 버텍스 그리는 순서 (삼각형)
                triangles = new[] { 0, 1, 2, 0, 2, 3 },

                // 화면 전체를 덮는 거대 바운드 생성. (Frustum Cull로 최적화 되지 않음)
                bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f)
            };

            // 메쉬 필터 가져오기 (없다면 생성)
            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            // 메쉬 렌더러 가져오기 (없다면 생성)
            _mr = GetComponent<MeshRenderer>();
            if (_mr == null) _mr = gameObject.AddComponent<MeshRenderer>();

            // 그림자 관련 옵션 off
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            // URP 2D는 투명한 렌더러를 정렬 순서에 따라 정렬함. 스코프 마스크에 적당히 큰 값을 주어
            // 항상 다른 오브젝트 위에 그리도록 함
            _mr.sortingOrder = 30000;

            // 머티리얼 생성
            _mat = new Material(Shader.Find("ThunderVeil/ScopeMask"));
            _mr.sharedMaterial = _mat;

            // Uniform값 설정
            _mat.SetFloat(PInner, innerRadius);
            _mat.SetFloat(POuter, outerRadius);
        }

        private void LateUpdate()
        {
            if (_mat == null) return;
            if (_cam == null) _cam = Camera.main;

            // 게임 종료시 (미션 성공 또는 실패) 마스크 렌더링하지 않음
            var gm = GameManager.Instance;
            bool show = !(gm != null && gm.GameEnd);
            if (_mr != null) _mr.enabled = show;
            if (!show) return;

            Vector4 uv = new Vector4(0.5F, 0.5F, 0.0F, 0.0F);

            if (_cam != null && scopeTarget != null)
            {
                // 카메라의 뷰포트 상 위치 (가시 공간 좌표계)
                Vector3 camScreenPos = _cam.WorldToViewportPoint(scopeTarget.position);
                uv.x = camScreenPos.x;
                uv.y = 1.0F - camScreenPos.y; // 중요: 쉐이더의 y좌표계는 유니티 뷰포트 좌표계와 반대
            }

            _mat.SetVector(PCenter, uv);

            // 종횡비 계산
            _mat.SetFloat(PAspect, (float)Screen.width / Mathf.Max(1, Screen.height));
        }
    }
}