using UnityEngine;
using UnityEngine.EventSystems;

namespace Content.UserInterface
{
    public sealed class ButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField]
        private AudioClip hoverSound;

        [SerializeField]
        private AudioClip clickSound;

        [SerializeField]
        private AudioSource uiSoundSource;

        public void OnPress()
        {
            uiSoundSource.PlayOneShot(clickSound);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            uiSoundSource.PlayOneShot(hoverSound);
        }
    }
}