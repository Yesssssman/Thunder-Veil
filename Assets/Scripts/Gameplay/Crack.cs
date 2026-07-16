using System;
using UnityEngine;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// A single bullet-impact decal. Full opacity for ~120 frames, then a linear fade over
    /// the last 160 (remainTime/160), then it releases itself back to the pool.
    /// Recycled via UnityEngine.Pool.ObjectPool&lt;Crack&gt; owned by FiringSystem.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Crack : MonoBehaviour
    {
        [SerializeField]
        private float lifeFrames = 120f;

        [SerializeField]
        private float fadeFrames = 50f;

        private SpriteRenderer _spriteRenderer;
        private float _remain;
        private bool _live;
        private Action<Crack> _release;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Place and (re)start the decal. onRelease is the pool's Release method.</summary>
        public void Play(Vector3 worldPos, Action<Crack> onRelease)
        {
            transform.position = worldPos;
            _remain = lifeFrames;
            _release = onRelease;
            _live = true;
            SetAlpha(1f);
        }

        private void FixedUpdate()
        {
            if (!_live) return;

            _remain -= 1f;
            SetAlpha(Mathf.Clamp01(_remain / fadeFrames));

            if (_remain < 0f)
            {
                _live = false;
                _release?.Invoke(this);
            }
        }

        private void SetAlpha(float a)
        {
            if (_spriteRenderer == null) return;
            var color = _spriteRenderer.color;
            color.a = a;
            _spriteRenderer.color = color;
        }
    }
}
