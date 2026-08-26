using UnityEngine;

namespace Content.ShaderConnector
{
    public class PanoramaShaderConnector : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer meshRenderer;

        [Tooltip("파노라마 이동 속도")]
        [SerializeField]
        private float speed = 0.2F;

        // 쉐이더 Property IDs
        private static readonly int PDeltaTime = Shader.PropertyToID("_DeltaTime");

        private float _deltaTime;

        public void Update()
        {
            _deltaTime += Time.deltaTime * speed;
            meshRenderer.material.SetFloat(PDeltaTime, _deltaTime);
        }
    }
}