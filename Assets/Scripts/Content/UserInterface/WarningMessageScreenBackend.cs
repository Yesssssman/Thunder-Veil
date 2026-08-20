using TMPro;
using UnityEngine;

namespace Content.UserInterface
{
    public sealed class WarningMessageScreenBackend : MonoBehaviour
    {
        [SerializeField]
        private GameObject titleText;

        [SerializeField]
        private GameObject messageText;

        [SerializeField]
        private GameObject selfScreen;

        public void SetTitleText(string title)
        {
            TMP_Text textMeshPro = titleText.GetComponent<TMP_Text>();
            textMeshPro.SetText(title);
            textMeshPro.ForceMeshUpdate();
        }

        public void setMessageText(string message)
        {
            TMP_Text textMeshPro = messageText.GetComponent<TMP_Text>();
            textMeshPro.SetText(message);
            textMeshPro.ForceMeshUpdate();
        }

        public void OnExitPressed()
        {
            selfScreen.SetActive(false);
        }
    }
}