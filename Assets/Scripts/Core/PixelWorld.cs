using UnityEngine;

namespace ThunderVeil.Core
{
    public static class PixelWorld
    {
        public const float PPU = 100f; // default pixels per Unity world unit
        public const float ScreenW = 1280f;
        public const float ScreenH = 720f;

        // World rectangle the virtual space maps onto. Defaults to the original layout.
        private static float _minX = 0f;
        private static float _maxX = ScreenW / PPU;   //  12.8
        private static float _minY = -ScreenH / PPU;  //  -7.2
        private static float _maxY = 0f;

        public static Vector2 AreaCenter => new ((_minX + _maxX) * 0.5f, (_minY + _maxY) * 0.5f);
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

        public static Vector3 AreaCenterWorld => new (AreaCenter.x, AreaCenter.y, 0f);

        /// <summary>Orthographic size that fits the whole play area for a given viewport aspect.</summary>
        public static float FitOrthoSize(float aspect)
        {
            float halfH = AreaHeight * 0.5f;
            float halfW = AreaWidth * 0.5f;
            return Mathf.Max(halfH, halfW / Mathf.Max(1e-4f, aspect));
        }
    }
}