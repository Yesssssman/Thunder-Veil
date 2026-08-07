using UnityEngine;
using UnityEngine.Serialization;

namespace Content
{
    /// <summary>
    /// 스코프의 움직임; 마우스 커서 트래킹, 반동 구현 등을 담당하는 클래스
    /// </summary>
    public class ScopeController : MonoBehaviour
    {
        [Header("Refs")] [SerializeField] private Transform screenLayer;

        [FormerlySerializedAs("scopeReticle")] [SerializeField]
        private Transform scopeDecal;

        [SerializeField]
        private FiringSystem firing;

        [SerializeField]
        private SpriteRenderer backgroundSprite;

        [FormerlySerializedAs("worldZoom")] [Header("Tuning")] [SerializeField] [Tooltip("스코프 기본 배율")]
        private float scopeZoom = 1.0f;

        [SerializeField]
        [Tooltip("스코프가 포인터를 부드럽게 따라오는 정도")]
        private float scopeEasing;

        [SerializeField]
        [Tooltip("카메라가 포인터를 부드럽게 따라오는 정도")]
        private float cameraEasing;

        // 카메라의 focal point (픽셀 값 기준, background boundary 처리를 위해 픽셀 값으로 잡는게 유리)
        private Vector2 _cameraFocal;

        // 스코프의 focal point (픽셀 값 기준)
        private Vector2 _scopeFocal;

        // 최대, 최소값 of 배경 스프라이트 (in pixels unit)
        private Vector2 _bgSizeInPixel;

        // 최대, 최소값 of 스코프 스프라이트 (in pixels unit)
        private Vector2 _scopeSizeInPixel;

        private Camera _camera;

        // 줌과 무관하게 스코프의 화면상 크기를 고정하기 위한 기준 스케일 (Start에서 캡처)
        private Vector3 _scopeBaseScale = Vector3.one;

        // 카메라의 World 좌표 구하기
        public Vector3 CameraWorldFocalPoint => new(
            backgroundSprite.bounds.min.x + backgroundSprite.bounds.size.x * (_cameraFocal.x / _bgSizeInPixel.x),
            backgroundSprite.bounds.min.y + backgroundSprite.bounds.size.y * (_cameraFocal.y / _bgSizeInPixel.y),
            0
        );

        // 스코프의 World 좌표 구하기
        public Vector3 ScopeWorldFocalPoint => new(
            backgroundSprite.bounds.min.x + backgroundSprite.bounds.size.x * (_scopeFocal.x / _bgSizeInPixel.x),
            backgroundSprite.bounds.min.y + backgroundSprite.bounds.size.y * (_scopeFocal.y / _bgSizeInPixel.y),
            0
        );

        private void Start()
        {
            var background = GameObject.Find("Background");
            if (background == null) return;

            _camera = Camera.main;

            // 스코프 데칼의 기준(줌 1) 스케일을 저장
            if (scopeDecal != null) _scopeBaseScale = scopeDecal.localScale;

            if (screenLayer == null)
            {
                var sl = GameObject.Find("ScreenLayer");

                if (sl != null)
                {
                    screenLayer = sl.transform;
                }
            }

            var scope = screenLayer.Find("ScopeReticle");

            // 스코프 가로, 세로 픽셀 사이즈 구하기
            if (scope != null && scope.TryGetComponent<SpriteRenderer>(out var scopeRenderer))
            {
                var bounds = scopeRenderer.bounds;

                _scopeSizeInPixel = new Vector2(
                    bounds.size.x * 0.5F * scopeRenderer.sprite.pixelsPerUnit,
                    bounds.size.y * 0.5F * scopeRenderer.sprite.pixelsPerUnit
                );
            }

            if (background != null && background.TryGetComponent<SpriteRenderer>(out var bgRenderer))
            {
                backgroundSprite = bgRenderer;
                var bounds = bgRenderer.bounds;

                _bgSizeInPixel = new Vector2(
                    bounds.size.x * bgRenderer.sprite.pixelsPerUnit,
                    bounds.size.y * bgRenderer.sprite.pixelsPerUnit
                );

                _cameraFocal = new(_bgSizeInPixel.x / 2, _bgSizeInPixel.y / 2);
            }
        }

        private void Update()
        {
            if (_camera == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Look == null || gm.Zoom == null) return;

            // 카메라 줌인 & 위치 이동 (스코프 포커싱)
            float zoomDelta = gm.Zoom.ReadValue<float>();
            scopeZoom = Mathf.Clamp(scopeZoom + zoomDelta * 0.25F, 1.0F, 4F);

            // 가시 공간 설정 (orthographic * 2 == Visible area height)
            _camera.orthographicSize = 2.5F / scopeZoom;

            // 스코프 크기 고정: 월드 스프라이트를 줌 배율의 역수로 스케일해 화면상 크기를 일정하게 유지
            if (scopeDecal != null) scopeDecal.localScale = _scopeBaseScale / scopeZoom;

            // PPU (pixelsPerUnit, 윌드 좌표계 기준 1 유닛에 들어가는 픽셀 수)
            float ppu = backgroundSprite.sprite.pixelsPerUnit;

            // 마우스 움직임 입력, 줌값 기준으로 포인터 이동 속도(화면상 x, 월드 상)가 감쇄됨
            Vector2 delta = gm.Look.ReadValue<Vector2>();

            // 스코프 좌표 위치 계산
            float scopeVolumeWidth = _scopeSizeInPixel.x * 0.5F;
            float scopeVolumeHeight = _scopeSizeInPixel.y * 0.5F;

            _scopeFocal += delta * (2.0F / scopeZoom);
            _scopeFocal = new Vector2(
                Mathf.Clamp(_scopeFocal.x, scopeVolumeWidth, _bgSizeInPixel.x - scopeVolumeWidth),
                Mathf.Clamp(_scopeFocal.y, scopeVolumeHeight, _bgSizeInPixel.y - scopeVolumeHeight)
            );

            float viewVolumeWidth = _camera.orthographicSize * ppu * _camera.aspect;
            float viewVolumeHeight = _camera.orthographicSize * ppu;

            _cameraFocal = new(
                Mathf.Clamp(_scopeFocal.x, viewVolumeWidth, _bgSizeInPixel.x - viewVolumeWidth),
                Mathf.Clamp(_scopeFocal.y, viewVolumeHeight, _bgSizeInPixel.y - viewVolumeHeight)
            );

            // 픽셀 좌표 => 월드 좌표 변환
            Vector2 focalPointWorld = CameraWorldFocalPoint;
            Vector2 scopePointWorld = ScopeWorldFocalPoint;

            // 카메라 위치 설정
            Vector3 gap = focalPointWorld - (Vector2)_camera.transform.position;
            gap *= cameraEasing * Time.deltaTime;
            _camera.transform.position += gap;

            // 스코프 위치 설정
            Vector3 scopeGap = scopePointWorld - (Vector2)scopeDecal.transform.position;
            scopeGap *= scopeEasing * Time.deltaTime;
            scopeDecal.transform.position += scopeGap;
        }
    }
}