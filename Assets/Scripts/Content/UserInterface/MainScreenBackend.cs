using UnityEngine;

namespace Content.UserInterface
{
    public sealed class MainScreenBackend : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainScreen;

        [SerializeField]
        private GameObject stageScreen;

        public void OnPressStart()
        {
            mainScreen.SetActive(false);
            stageScreen.SetActive(true);
        }

        public void OnPressOptions()
        {

        }

        public void OnPressExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}