using UnityEngine;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// Builds and drives the fullscreen scope-mask pass at runtime (no scene wiring needed).
    /// A single [-1,1] quad rendered with the ThunderVeil/ScopeMask shader, which emits its
    /// verts straight to clip space so it always covers the whole screen. The reveal hole
    /// follows the scope's screen position.
    /// </summary>
    public class ScopeOverlay : MonoBehaviour
    {
        private static readonly int PCenter = Shader.PropertyToID("_ScopeCenter");
        private static readonly int PInner = Shader.PropertyToID("_InnerRadius");
        private static readonly int POuter = Shader.PropertyToID("_OuterRadius");
        private static readonly int PAspect = Shader.PropertyToID("_Aspect");

        private Material _material;
        private MeshRenderer _meshRenderer;

        public static ScopeOverlay Create(string name)
        {
            var go = new GameObject(name);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = BuildFullscreenQuad();

            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            var shader = Shader.Find("ThunderVeil/ScopeMask");
            var mat = new Material(shader);

            var ov = go.AddComponent<ScopeOverlay>();
            ov._material = mat;
            ov._meshRenderer = mr;
            mr.sharedMaterial = mat;

            // Original reveal radii, as a fraction of the 720px-tall virtual screen.
            mat.SetFloat(PInner, 152f / 720f);
            mat.SetFloat(POuter, 190f / 720f);
            return ov;
        }

        private static Mesh BuildFullscreenQuad()
        {
            var m = new Mesh();

            m.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f)
            };

            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f); // never frustum-cull

            return m;
        }

        /// <summary>uv = scope centre in 0..1 screen space (y up).</summary>
        public void SetScope(Vector2 uv)
        {
            if (_material == null) return;
            _material.SetVector(PCenter, new Vector4(uv.x, uv.y, 0f, 0f));
            _material.SetFloat(PAspect, (float)Screen.width / Mathf.Max(1, Screen.height));
        }

        public void SetVisible(bool visible)
        {
            if (_meshRenderer != null) _meshRenderer.enabled = visible;
        }
    }
}
