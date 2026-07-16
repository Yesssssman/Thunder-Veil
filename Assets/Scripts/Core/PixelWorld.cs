using UnityEngine;

namespace ThunderVeil.Core
{
    /// <summary>
    /// The original game worked in a fixed 1280x720, top-left-origin, y-down pixel space.
    /// We keep all *gameplay* math in that "virtual pixel" space so the original constants
    /// (patrol coords, sight rects, scope easing divisor, hit-test extents) port verbatim,
    /// and convert to Unity world space (y-up) only at the render/physics boundary.
    ///
    /// The virtual space is mapped onto a world-space rectangle (the "play area"). By default
    /// that rectangle matches the original background (0,0)..(12.8,-7.2) at 100 PPU, but
    /// <see cref="ConfigureFromBounds"/> re-points it at the live Background renderer bounds so
    /// the whole game (camera, soldiers, scope) adapts when the background is moved or resized.
    ///
    /// 
    /// 
    /// </summary>
    public static class PixelWorld
    {
        public const float PPU = 100f;      // default pixels per Unity world unit
        public const float ScreenW = 1280f;
        public const float ScreenH = 720f;

        // World rectangle the virtual space maps onto. Defaults to the original layout.
        private static float _minX = 0f;
        private static float _maxX = ScreenW / PPU;   //  12.8
        private static float _minY = -ScreenH / PPU;  //  -7.2
        private static float _maxY = 0f;

        public static Vector2 AreaCenter => new Vector2((_minX + _maxX) * 0.5f, (_minY + _maxY) * 0.5f);
        public static float AreaWidth => _maxX - _minX;
        public static float AreaHeight => _maxY - _minY;

        /// <summary>
        /// Background 바운더리 설정. 인자값은 Unity meter 단위 기준. (Pixel 아님)
        /// </summary>
        public static void ConfigureFromBounds(Bounds b)
        {
            _minX = b.min.x; _maxX = b.max.x;
            _minY = b.min.y; _maxY = b.max.y;
        }

        /// <summary>
        /// 픽셀 좌표계 -> 유니티 월드 좌표계로 변환
        /// Virtual pixel (top-left, y-down) -> Unity world (y-up), within the play area.
        /// </summary>
        public static Vector3 ToWorld(Vector2 px, float z = 0f)
        {
            float u = px.x / ScreenW;   // 0..1 left->right
            float v = px.y / ScreenH;   // 0..1 top->bottom
            
            return new Vector3(
                Mathf.LerpUnclamped(_minX, _maxX, u),
                Mathf.LerpUnclamped(_maxY, _minY, v),   // top (maxY) -> bottom (minY)
                z
            );
        }

        /// <summary>Unity world -> virtual pixel (inverse of <see cref="ToWorld"/>).</summary>
        public static Vector2 WorldToVirtual(Vector3 world)
        {
            float w = Mathf.Max(1e-4f, AreaWidth);
            float h = Mathf.Max(1e-4f, AreaHeight);
            float u = (world.x - _minX) / w;
            float v = (_maxY - world.y) / h;
            return new Vector2(u * ScreenW, v * ScreenH);
        }

        /// <summary>Pixel-space size -> world-space size, scaled by the play area (both axes positive).</summary>
        public static Vector2 SizeToWorld(Vector2 px)
        {
            return new Vector2(
                Mathf.Abs(px.x) / ScreenW * AreaWidth,
                Mathf.Abs(px.y) / ScreenH * AreaHeight);
        }

        /// <summary>
        /// Pixel-space offset/direction -> world-space offset (signed, y flipped, no translation).
        /// Used to place a pixel-authored sight box relative to a world-positioned soldier.
        /// </summary>
        public static Vector2 VectorToWorld(Vector2 px)
        {
            return new Vector2(px.x / ScreenW * AreaWidth, -px.y / ScreenH * AreaHeight);
        }

        /// <summary>
        /// Input System pointer (bottom-left origin, real device pixels) -> virtual space.
        /// Kept as a viewport-fraction fallback; ScopeController prefers Camera.ScreenToWorldPoint
        /// + WorldToVirtual so aiming stays exact under any camera framing.
        /// </summary>
        public static Vector2 ScreenToVirtual(Vector2 screen)
        {
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            float vx = screen.x / w * ScreenW;
            float vy = (1f - screen.y / h) * ScreenH;
            return new Vector2(vx, vy);
        }

        /// <summary>World-space center of the play area (for the camera).</summary>
        public static Vector3 AreaCenterWorld => new Vector3(AreaCenter.x, AreaCenter.y, 0f);

        /// <summary>Orthographic size that fits the whole play area for a given viewport aspect.</summary>
        public static float FitOrthoSize(float aspect)
        {
            float halfH = AreaHeight * 0.5f;
            float halfW = AreaWidth * 0.5f;
            return Mathf.Max(halfH, halfW / Mathf.Max(1e-4f, aspect));
        }
    }
}
