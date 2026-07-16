using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// 스코프의 움직임; 마우스 커서 트래킹, 반동 구현 등을 담당하는 클래스
    /// </summary>
    public class ScopeController : MonoBehaviour
    {
        [Header("Refs")]

        [SerializeField]
        private Transform screenLayer;

        [FormerlySerializedAs("scopeReticle")]
        [SerializeField]
        private Transform scopeDecal;

        [SerializeField]
        private FiringSystem firing;

        [SerializeField]
        private SpriteRenderer backgroundSprite;

        [FormerlySerializedAs("worldZoom")]
        [Header("Tuning")]

        [SerializeField]
        [Tooltip("스코프 기본 배율")]
        private float scopeZoom = 1.0f;

        // 카메라의 focal point (픽셀 값 기준, background boundary 처리를 위해 픽셀 값으로 잡는게 유리)
        private Vector2 _cameraFocal;

        // 최대, 최소값 of 배경 스프라이트 (in pixels unit)
        private Vector2 _bgSizeInPixel;

        // 최대, 최소값 of 스코프 스프라이트 (in pixels unit)
        private Vector2 _scopeSizeInPixel;

        private Camera _camera;

        private void Start()
        {
            var background = GameObject.Find("Background");
            if (background == null) return;

            _camera = Camera.main;

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
            if (gm == null || gm.Look == null) return;

            // 카메라 줌인 & 위치 이동 (스코프 포커싱)
            float zoom = Mathf.Clamp(scopeZoom, 0.01F, 4F);

            // 가시 공간 설정 (orthographic * 2 == Visible area height)
            _camera.orthographicSize = 2.5F / zoom;

            float PPU = backgroundSprite.sprite.pixelsPerUnit;

            // 가시 부피의 가로, 세로의 절반 길이 (픽셀 단위)
            float viewVolumeWidth = _camera.orthographicSize * PPU * _camera.aspect;
            float viewVolumeHeight = _camera.orthographicSize * PPU;

            // 마우스 움직임 입력, 줌값 기준으로 포인터 이동 속도 줄이기
            _cameraFocal += gm.Look.ReadValue<Vector2>() * (2.0F / scopeZoom);
            _cameraFocal = new Vector2(
                Math.Clamp(_cameraFocal.x, viewVolumeWidth, _bgSizeInPixel.x - viewVolumeWidth),
                Math.Clamp(_cameraFocal.y, viewVolumeHeight, _bgSizeInPixel.y - viewVolumeHeight)
            );

            var bounds = backgroundSprite.bounds;

            // 픽셀 좌표 => 월드 좌표 변환
            float focalWorldX = bounds.min.x + bounds.size.x * (_cameraFocal.x / _bgSizeInPixel.x);
            float focalWorldY = bounds.min.y + bounds.size.y * (_cameraFocal.y / _bgSizeInPixel.y);

            // 카메라 포커스
            _camera.transform.position = new Vector3(
                focalWorldX, focalWorldY, _camera.transform.position.z
            );

            // 스코프 포커스
            scopeDecal.transform.position = new Vector3(
                focalWorldX, focalWorldY, scopeDecal.transform.position.z
            );

            Debug.Log($"WTF {_cameraFocal.x} {_cameraFocal.y}");

            /*
            Vector3 pivot = _cursor;
            Vector3 p = pivot + -pivot / zoom;

            _camera.transform.position = new Vector3(
                p.x,
                p.y,
                _camera.transform.position.z
            );
            */
        }

        /*
        private void FixedUpdate()
        {

            Vector2 scopeCenter = ScopeCenter;
            Vector2 cursor = _cursor;
            Vector2 diff = cursor - scopeCenter;
            scopeCenter += diff.normalized * diff.sqrMagnitude;

            // 사격 반동
            if (firing != null) scopeCenter.y -= Mathf.Max(firing.FireCooldown - 188, 0);

            ScopeCenter = scopeCenter;
        }

        private void LateUpdate()
        {
            // if (scopeDecal != null)
            //    scopeDecal.position = ScopeCenter;
        }
        */
    }
}