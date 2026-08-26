using UnityEngine;
using UnityEngine.Assertions;

namespace Content.System
{
    /// <summary>
    /// 스코프 이동의 공통 규칙: 배경 경계 클램프, 줌 배율, 카메라/십자선 보간을 담당.
    ///
    /// 조준점을 실제로 움직이는 주체는 파생 클래스. (마우스 입력, 랜덤 이동 등)
    /// </summary>
    public abstract class ScopeController : MonoBehaviour
    {
        [Header("Refs")]

        // 스코프 십자선 오브젝트
        [SerializeField]
        private Transform scopeReticle;

        // 스코프가 이동 가능한 공간
        [SerializeField]
        [Tooltip("배경 Sprite")]
        private SpriteRenderer backgroundSprite;

        [SerializeField]
        [Tooltip("카메라. 비워두면 줌과 카메라 추적을 하지 않음")]
        private Camera mainCamera;

        [Header("Tuning")]

        [SerializeField]
        [Tooltip("스코프 기본 배율")]
        private float scopeZoom = 1.0F;

        [SerializeField]
        [Tooltip("스코프 최대 배율")]
        private float maxScopeZoom = 4.0F;

        [SerializeField]
        [Tooltip("배율 1 기준 가시 공간 높이의 절반 (월드 유닛)")]
        private float baseViewHeight = 2.5F;

        [SerializeField]
        [Tooltip("스코프 => 목표 지점 보간 속도")]
        private float scopeEasing = 8.0F;

        [SerializeField]
        [Tooltip("카메라 => 목표 지점 보간 속도. 0이면 카메라 고정")]
        private float cameraEasing = 4.0F;

        // 카메라와 스코프의 목표 지점이 `_cameraFocal`와 `_scopeFocal`로 분리된 이유:
        // 모서리 부분에서 배경 공간을 너머 카메라가 빈 공간을 렌더링하지 않기 위해 두 위치가 다를 수 있음.
        //
        // 화면 좌표계 기준으로 경계 처리를 위해 두 목표 지점은 픽셀 단위로 저장함.

        // 카메라 focal point
        private Vector2 _cameraFocal;

        // 스코프 focal point
        private Vector2 _scopeFocal;

        // 배경 스프라이트의 가로, 세로 크기 (in pixels unit)
        private Vector2 _bgSizeInPixel;

        // 스코프 스프라이트의 가로, 세로 크기의 절반 (in pixels unit)
        private Vector2 _scopeHalfSizeInPixel;

        // 줌과 무관하게 스코프의 화면상 크기를 고정하기 위한 기준 스케일 (Start에서 캡처)
        private Vector3 _scopeBaseScale = Vector3.one;

        // 배경 스프라이트의 픽셀 크기. 파생 클래스가 조준점 범위를 계산할 때 사용
        protected Vector2 BackgroundSizeInPixel => _bgSizeInPixel;

        // 카메라의 World 좌표 구하기
        public Vector3 CameraWorldFocalPoint => PixelToWorld(_cameraFocal);

        // 스코프의 World 좌표 구하기
        public Vector3 ScopeWorldFocalPoint => PixelToWorld(_scopeFocal);

        protected virtual void Start()
        {
            Assert.IsNotNull(scopeReticle);
            Assert.IsNotNull(backgroundSprite);

            // 스코프 십자선 Scale 저장
            _scopeBaseScale = scopeReticle.localScale;

            SpriteRenderer scopeRenderer = scopeReticle.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(scopeRenderer);

            // 스코프 가로, 세로 픽셀 사이즈 저장
            var scopeBounds = scopeRenderer.bounds;

            _scopeHalfSizeInPixel = new Vector2(
                scopeBounds.size.x * 0.5F * scopeRenderer.sprite.pixelsPerUnit,
                scopeBounds.size.y * 0.5F * scopeRenderer.sprite.pixelsPerUnit
            );

            var bgBounds = backgroundSprite.bounds;

            _bgSizeInPixel = new Vector2(
                bgBounds.size.x * backgroundSprite.sprite.pixelsPerUnit,
                bgBounds.size.y * backgroundSprite.sprite.pixelsPerUnit
            );

            // 조준점, 카메라 모두 배경 중앙에서 시작
            _scopeFocal = _bgSizeInPixel * 0.5F;
            _cameraFocal = _scopeFocal;
        }

        protected virtual void Update()
        {
            if (mainCamera != null)
            {
                // 가시 공간 설정 (orthographicSize * 2 == 가시 공간 높이)
                mainCamera.orthographicSize = baseViewHeight / scopeZoom;

                // 스코프 상대 크기 유지: 월드 스프라이트를 줌 배율의 역수로 스케일해 화면상 크기를 일정하게 유지
                scopeReticle.localScale = _scopeBaseScale / scopeZoom;

                UpdateCameraFocal();

                // 보간값 기준으로 카메라 이동
                Vector3 cameraGap = (Vector2)CameraWorldFocalPoint - (Vector2)mainCamera.transform.position;
                mainCamera.transform.position += cameraGap * (cameraEasing * Time.deltaTime);
            }

            // 보간값 기준으로 스코프 이동
            Vector3 scopeGap = (Vector2)ScopeWorldFocalPoint - (Vector2)scopeReticle.position;
            scopeReticle.position += scopeGap * (scopeEasing * Time.deltaTime);
        }

        // ***************************************************************
        // 파생 클래스용 조작 API
        // ***************************************************************

        // 조준점을 delta(픽셀)만큼 이동. 배율이 높을수록 화면상 이동 거리가 커지므로 배율로 나눠 감쇄
        protected void MoveFocalPoint(Vector2 delta) => SetFocalPoint(_scopeFocal + delta / scopeZoom);

        // 조준점을 배경 픽셀 좌표로 직접 지정. 십자선이 배경 밖으로 나가지 않도록 클램프됨
        protected void SetFocalPoint(Vector2 pixel)
        {
            _scopeFocal = new Vector2(
                Mathf.Clamp(pixel.x, _scopeHalfSizeInPixel.x, _bgSizeInPixel.x - _scopeHalfSizeInPixel.x),
                Mathf.Clamp(pixel.y, _scopeHalfSizeInPixel.y, _bgSizeInPixel.y - _scopeHalfSizeInPixel.y)
            );
        }

        // 스코프 줌 강도 변경
        protected void ZoomScope(float delta) => scopeZoom = Mathf.Clamp(scopeZoom + delta, 1.0F, maxScopeZoom);

        // ***************************************************************
        // Helpers
        // ***************************************************************

        // 카메라 목표 지점을 조준점에서 다시 계산. 배경 바깥의 빈 공간이 보이지 않도록 가시 공간의 절반만큼 안쪽으로 클램프
        private void UpdateCameraFocal()
        {
            // PPU (pixelsPerUnit, 월드 좌표계 기준 1 유닛에 들어가는 픽셀 수)
            float ppu = backgroundSprite.sprite.pixelsPerUnit;
            float halfViewWidth = mainCamera.orthographicSize * mainCamera.aspect * ppu;
            float halfViewHeight = mainCamera.orthographicSize * ppu;

            _cameraFocal = new Vector2(
                Mathf.Clamp(_scopeFocal.x, halfViewWidth, _bgSizeInPixel.x - halfViewWidth),
                Mathf.Clamp(_scopeFocal.y, halfViewHeight, _bgSizeInPixel.y - halfViewHeight)
            );
        }

        // 배경 픽셀 좌표 => World 좌표
        private Vector3 PixelToWorld(Vector2 pixel)
        {
            var bounds = backgroundSprite.bounds;

            return new Vector3(
                bounds.min.x + bounds.size.x * (pixel.x / _bgSizeInPixel.x),
                bounds.min.y + bounds.size.y * (pixel.y / _bgSizeInPixel.y),
                0.0F
            );
        }
    }
}
