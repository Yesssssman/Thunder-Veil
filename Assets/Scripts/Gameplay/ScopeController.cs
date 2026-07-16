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

        [FormerlySerializedAs("scopeReticle")] [SerializeField]
        private Transform scopeDecal;

        [SerializeField]
        private FiringSystem firing;

        [FormerlySerializedAs("worldZoom")]
        [Header("Tuning")]

        [SerializeField]
        [Tooltip("스코프 기본 배율")]
        private float scopeZoom = 1.5f;

        // 스크린 대각선 거리 제곱 (스코프->커서 위치 거리 기반 보간에 사용됨)
        private float _diagonalLengthSquare;

        // 마우스 커서의 위치 in pixel coordinate (정확한 픽셀 값을 추적하기 위함)
        private Vector2 _cursor;

        // 스코프 위치 (보간)
        public Vector2 ScopeCenter { get; private set; }

        private Camera _camera;
        private int _widthPixel;
        private int _heightPixel;

        private void Start()
        {
            _cursor = new(0.0F, 0.0F);
            ScopeCenter = _cursor;
            _camera = Camera.main;

            if (screenLayer == null)
            {
                var sl = GameObject.Find("ScreenLayer");

                if (sl != null)
                {
                    screenLayer = sl.transform;
                }
            }

            var background = GameObject.Find("Background");

            if (background != null)
            {
                var spriteRenderer = background.GetComponent<SpriteRenderer>();

                // 스프라이트의 가로/세로 픽셀 크기
                Vector2 worldSize = spriteRenderer.sprite.bounds.size;
                _widthPixel = (int)Math.Round(worldSize.x * spriteRenderer.sprite.pixelsPerUnit * background.transform.lossyScale.x);
                _heightPixel = (int)Math.Round(worldSize.y * spriteRenderer.sprite.pixelsPerUnit * background.transform.lossyScale.y);
            }
            else
            {
                // 기본 사이즈 (1280^2 * 720^2, 비정상 실행)
                _widthPixel = 1280;
                _heightPixel = 720;
            }
        }

        private void Update()
        {
            if (_camera == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Look == null) return;

            // 카메라 줌인 & 위치 이동 (스코프 포커싱)
            float zoom = Mathf.Clamp(scopeZoom, 0.01F, 4F);

            // 가시 공간 높이(orthographic * 2) 설정 by zoom-in scale
            _camera.orthographicSize = 1F / zoom;

            // 마우스 델타값(움직임)으로 화면 상에 foucs된 픽셀 위치를 설정
            _cursor += gm.Look.ReadValue<Vector2>() * (1 / scopeZoom);
            _cursor.x = Math.Clamp(_cursor.x, 200, _widthPixel - 200); // scope vignette pixel estimated 200px.
            _cursor.y = Math.Clamp(_cursor.y, 200, _heightPixel - 200);

            _camera.ScreenToWorldPoint(new Vector3(_cursor.x, _cursor.y));

            Vector3 pivot = _cursor;

            Vector3 p = pivot + -pivot / zoom;
            _camera.transform.position = new Vector3(
                p.x,
                p.y,
                _camera.transform.position.z
            );
        }

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
            if (scopeDecal != null)
                scopeDecal.position = ScopeCenter;
        }
    }
}