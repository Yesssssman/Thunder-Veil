using UnityEngine;
using UnityEngine.EventSystems;

namespace Content.UserInterface
{
    public sealed class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            GetComponentInParent<RectTransform>().localScale = new Vector3(1.1F, 1.1F, 1.1F);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            GetComponentInParent<RectTransform>().localScale = new Vector3(1.0F, 1.0F, 1.0F);
        }
    }
}