using UnityEngine;

namespace Content.ShaderConnector
{
    /// <summary>
    /// 스코프 비네팅을 구현한 포스트 프로세싱 셰이더
    /// </summary>
    public class ScopeVignetteShaderConnector : MonoBehaviour
    {
        [Header("스코프 반지름 (픽셀 단위)")]

        [Tooltip("비네팅 그라데이션 시작 반지름, 이 이하는 완전히 투명")]
        [SerializeField]
        private float innerRadius;

        [Tooltip("비네팅 그라데이션 종료 반지름, 이 이상은 완전히 불투명")]
        [SerializeField]
        private float outerRadius;

        [Tooltip("월드 좌표계 기준 스코프 위치")]
        [SerializeField]
        private Transform scopeTarget;

        [SerializeField]
        private Shader shader;

        // 셰이더 Property IDs
        private static readonly int PCenter = Shader.PropertyToID("_ScopeCenter");
        private static readonly int PInner = Shader.PropertyToID("_InnerRadius");
        private static readonly int POuter = Shader.PropertyToID("_OuterRadius");
        private static readonly int PAspect = Shader.PropertyToID("_Aspect");

        // 머티리얼
        private Material _mat;

        // 메쉬 Renderer
        private MeshRenderer _meshRenderer;

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

            // **************************
            // Mesh 빌드
            // **************************

            // 메쉬 생성
            var mesh = new Mesh
            {
                // 정점 위치로 표현한 메쉬 (전체 화면을 덮는 사각형 메쉬)
                vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                    new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f)
                },

                // 정점 그리는 순서 배열 (삼각형)
                triangles = new[]
                {
                    0, 1, 2,
                    0, 2, 3
                },

                // 메쉬 가시공간의 볼륨 (매우 크게 설정, Frustum Cull 스테이지에서 최적화 되지 않는 효과)
                bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f)
            };

            // 메쉬 필터 가져오기 (없다면 생성)
            var meshFilter = GetComponent<MeshFilter>();

            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            meshFilter.mesh = mesh;

            // 메쉬 렌더러 가져오기 (없다면 생성)
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // 그림자 관련 옵션 off
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            // URP 2D는 투명한 렌더러를 정렬 순서에 따라 정렬함. 스코프 마스크에 적당히 큰 값을 주어
            // 항상 다른 오브젝트 위에 그리도록 함
            _meshRenderer.sortingOrder = 30000;

            // 머티리얼 생성
            _mat = new Material(shader);
            _meshRenderer.sharedMaterial = _mat;

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
            if (_meshRenderer != null) _meshRenderer.enabled = show;
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