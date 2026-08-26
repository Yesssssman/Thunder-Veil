using UnityEngine;

namespace Content.System
{
    /// <summary>
    /// 로비 배경의 스코프. 일정 시간마다 배경 안의 임의 지점을 새 조준점으로 잡음.
    /// </summary>
    public sealed class LobbyScopeController : ScopeController
    {
        [Header("Wandering")]

        [SerializeField]
        [Tooltip("초점 변경 최소 대기 시간 (초)")]
        private float minIdleSeconds = 1.0F;

        [SerializeField]
        [Tooltip("초점 변경 최대 대기 시간 (초)")]
        private float maxIdleSeconds = 2.5F;

        [SerializeField]
        [Range(0.0F, 0.5F)]
        [Tooltip("배경 가장자리에서 띄울 여백 비율")]
        private float edgeMargin = 0.15F;

        // 다음 조준점을 뽑기까지 남은 시간 (초)
        private float _idleRemain;

        protected override void Update()
        {
            _idleRemain -= Time.deltaTime;

            if (_idleRemain <= 0.0F)
            {
                _idleRemain = Random.Range(minIdleSeconds, maxIdleSeconds);
                SetFocalPoint(RandomBackgroundPoint());
            }

            base.Update();
        }

        // 배경 안의 임의 지점 (배경 픽셀 좌표)
        private Vector2 RandomBackgroundPoint()
        {
            Vector2 size = BackgroundSizeInPixel;

            return new Vector2(
                Random.Range(size.x * edgeMargin, size.x * (1.0F - edgeMargin)),
                Random.Range(size.y * edgeMargin, size.y * (1.0F - edgeMargin))
            );
        }
    }
}