using UnityEngine;
using ThunderVeil.Core;

namespace ThunderVeil.Gameplay
{
    /// <summary>
    /// Thunder timing + the gunfire-mask window + rain. When thunder strikes it plays a
    /// clap and opens a 200-frame window during which a gunshot is "hidden" (soldiers
    /// don't hear it) — the core stealth mechanic.
    /// </summary>
    public class WeatherController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip thunderClip;

        [Header("Rain")]
        [SerializeField] private ParticleSystem rain;

        [Header("Tuning (ported constants)")]
        [SerializeField] private int thunderTimer = 1000;   // frames to first thunder
        [SerializeField] private int fireHiddenFrames = 200;

        private int _fireHiddenTimer;

        /// <summary>True while a recent thunderclap masks the sound of gunfire.</summary>
        public bool IsGunfireMasked => _fireHiddenTimer > 0;

        private void Start()
        {
            if (rain != null) rain.Play();
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.GameEnd)
            {
                if (rain != null && rain.isPlaying) rain.Stop();
                return;
            }

            if (thunderTimer <= 0)
            {
                if (sfxSource != null && thunderClip != null) sfxSource.PlayOneShot(thunderClip);
                thunderTimer = Random.Range(250, 750);   // rand()%500 + 250
                _fireHiddenTimer = fireHiddenFrames;
            }
            else
            {
                thunderTimer--;
            }

            if (_fireHiddenTimer > 0) _fireHiddenTimer--;
        }
    }
}
