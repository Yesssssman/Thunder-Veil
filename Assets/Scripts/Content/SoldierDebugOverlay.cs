using Content.Character.Soldier;
using UnityEngine;

namespace Content
{
    /// <summary>
    /// 시야 체크와 상태 표시를 위한 디버깅 overlay
    ///
    /// LineRenderer + 상태 라벨로 유닛의 상태를 시각화
    /// </summary>
    public class SoldierDebugOverlay : MonoBehaviour
    {
        [SerializeField]
        private Soldier soldier;

        [SerializeField]
        private LineRenderer sightBoxRenderer;

        [SerializeField]
        private LineRenderer hitboxRenderer;

        [SerializeField]
        private TextMesh label;

        private void LateUpdate()
        {
            var gm = GameManager.Instance;
            bool show = gm != null && gm.ShowIndicators;
            const float z = -1f;

            if (sightBoxRenderer != null) sightBoxRenderer.enabled = show;
            if (hitboxRenderer != null) hitboxRenderer.enabled = show;
            if (label != null) label.gameObject.SetActive(show);
            if (!show || soldier == null) return;

            if (sightBoxRenderer != null)
            {
                // BoxCollider2D의 size/offset은 로컬 값이므로 `lossyScale`을 곱해 월드 공간 사각형으로 변환한다.
                // (Soldier 루트의 스케일이 1이 아니므로, 이 변환이 적용되어 있지 않으면 에디터의 콜라이더와 크기가 어긋난다.)
                var c = soldier.sightCollider;
                Vector2 scale = c.transform.lossyScale;
                Vector2 center = (Vector2)c.transform.position + Vector2.Scale(c.offset, scale);
                Vector2 extent = Vector2.Scale(new Vector2(1, 1), scale) * 0.5f;

                // 시야박스 그리기
                sightBoxRenderer.useWorldSpace = true;
                sightBoxRenderer.positionCount = 5;
                sightBoxRenderer.SetPosition(0, new Vector3(center.x - extent.x, center.y - extent.y, z));
                sightBoxRenderer.SetPosition(1, new Vector3(center.x + extent.x, center.y - extent.y, z));
                sightBoxRenderer.SetPosition(2, new Vector3(center.x + extent.x, center.y + extent.y, z));
                sightBoxRenderer.SetPosition(3, new Vector3(center.x - extent.x, center.y + extent.y, z));
                sightBoxRenderer.SetPosition(4, new Vector3(center.x - extent.x, center.y - extent.y, z));
            }

            if (hitboxRenderer != null)
            {
                var c = soldier.spriteCollider;
                Vector2 scale = c.transform.lossyScale;
                Vector2 center = (Vector2)c.transform.position + Vector2.Scale(c.offset, scale);
                Vector2 extent = Vector2.Scale(c.size, scale) * 0.5f;

                // 히트박스 Renderer 설정
                hitboxRenderer.useWorldSpace = true;
                hitboxRenderer.positionCount = 5;
                hitboxRenderer.startColor = new Color(1.0F, 0.0F, 0.0F, 1.0F);
                hitboxRenderer.endColor = new Color(1.0F, 0.0F, 0.0F, 1.0F);
                hitboxRenderer.SetPosition(0, new Vector3(center.x - extent.x, center.y - extent.y, z));
                hitboxRenderer.SetPosition(1, new Vector3(center.x + extent.x, center.y - extent.y, z));
                hitboxRenderer.SetPosition(2, new Vector3(center.x + extent.x, center.y + extent.y, z));
                hitboxRenderer.SetPosition(3, new Vector3(center.x - extent.x, center.y + extent.y, z));
                hitboxRenderer.SetPosition(4, new Vector3(center.x - extent.x, center.y - extent.y, z));
            }

            // 라벨 렌더러 설정
            if (label != null)
            {
                label.text = soldier.GetCurrentStateCategory().ToString();
                Vector2 lp = (Vector2)soldier.transform.position + new Vector2(0f, -0.3f);
                label.transform.position = new Vector3(lp.x, lp.y, z);
            }
        }
    }
}