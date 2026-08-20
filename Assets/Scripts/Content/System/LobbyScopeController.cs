using System;
using UnityEngine;
using Random = System.Random;

namespace Content.System
{
    /// <summary>
    /// 로비에서 자동으로 움직이는 스코프에 대한 컨트롤러
    /// </summary>
    public sealed class LobbyScopeController : MonoBehaviour
    {
        [SerializeField]
        private Transform scopeReticle;

        [SerializeField]
        private Camera mainCamera;

        // 카메라의 focal point (픽셀 값 기준, background boundary 처리를 위해 픽셀 값으로 잡는게 유리)
        private Vector2 _cameraFocal;

        // 스코프의 focal point (픽셀 값 기준)
        private Vector2 _scopeFocal;

        // 최대, 최소값 of 배경 스프라이트 (in pixels unit)
        private Vector2 _bgSizeInPixel;

        // 최대, 최소값 of 스코프 스프라이트 (in pixels unit)
        private Vector2 _scopeSizeInPixel;

        private int _nextFocusMove;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void FixedUpdate()
        {
            if (--_nextFocusMove < 0)
            {
                _nextFocusMove = new Random().Next(60, 120);
            }
        }

        private void Update()
        {
            if (mainCamera == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Look == null || gm.Zoom == null) return;

            // 카메라 줌인 & 위치 이동 (스코프 포커싱)
            float zoomDelta = gm.Zoom.ReadValue<float>();

            /*
            scopeZoom = Mathf.Clamp(scopeZoom + zoomDelta * 0.25F, 1.0F, 4F);

            // 가시 공간 설정 (orthographic * 2 == Visible area height)
            mainCamera.orthographicSize = 2.5F / scopeZoom;

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

            float viewVolumeWidth = mainCamera.orthographicSize * ppu * mainCamera.aspect;
            float viewVolumeHeight = mainCamera.orthographicSize * ppu;

            _cameraFocal = new(
                Mathf.Clamp(_scopeFocal.x, viewVolumeWidth, _bgSizeInPixel.x - viewVolumeWidth),
                Mathf.Clamp(_scopeFocal.y, viewVolumeHeight, _bgSizeInPixel.y - viewVolumeHeight)
            );

            // 픽셀 좌표 => 월드 좌표 변환
            Vector2 focalPointWorld = CameraWorldFocalPoint;
            Vector2 scopePointWorld = ScopeWorldFocalPoint;

            // 카메라 위치 설정
            Vector3 gap = focalPointWorld - (Vector2)mainCamera.transform.position;
            gap *= cameraEasing * Time.deltaTime;
            mainCamera.transform.position += gap;

            // 스코프 위치 설정
            Vector3 scopeGap = scopePointWorld - (Vector2)scopeDecal.transform.position;
            scopeGap *= scopeEasing * Time.deltaTime;
            scopeDecal.transform.position += scopeGap;
            */
        }
    }
}