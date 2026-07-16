using UnityEngine;

namespace ThunderVeil.Gameplay
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
        private LineRenderer sightBox;

        [SerializeField]
        private TextMesh label;

        private void LateUpdate()
        {
            var gm = GameManager.Instance;
            bool show = gm != null && gm.ShowIndicators;

            if (sightBox != null) sightBox.enabled = show;
            if (label != null) label.gameObject.SetActive(show);
            if (!show || soldier == null) return;

            // BoxCollider2D의 size/offset은 로컬 값이므로 `lossyScale`을 곱해 월드 공간 사각형으로 변환한다.
            // (Soldier 루트의 스케일이 1이 아니므로, 이 변환이 적용되어 있지 않으면 에디터의 콜라이더와 크기가 어긋난다.)
            var sight = soldier.sightCollider;
            Vector2 scale = sight.transform.lossyScale;
            Vector2 center = (Vector2)sight.transform.position + Vector2.Scale(sight.offset, scale);
            Vector2 extent = Vector2.Scale(sight.size, scale) * 0.5f;
            const float z = -1f;

            // 시야박스 Renderer 설정
            if (sightBox != null)
            {
                sightBox.useWorldSpace = true;
                sightBox.positionCount = 5;
                sightBox.SetPosition(0, new Vector3(center.x - extent.x, center.y - extent.y, z));
                sightBox.SetPosition(1, new Vector3(center.x + extent.x, center.y - extent.y, z));
                sightBox.SetPosition(2, new Vector3(center.x + extent.x, center.y + extent.y, z));
                sightBox.SetPosition(3, new Vector3(center.x - extent.x, center.y + extent.y, z));
                sightBox.SetPosition(4, new Vector3(center.x - extent.x, center.y - extent.y, z));
            }

            // 라벨 렌더러 설정
            if (label != null)
            {
                label.text = soldier.Current.ToString();
                Vector2 lp = (Vector2)soldier.transform.position + new Vector2(0f, -0.3f);
                label.transform.position = new Vector3(lp.x, lp.y, z);
            }
        }
    }
}
