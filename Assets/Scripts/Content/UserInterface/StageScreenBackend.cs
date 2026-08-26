using System;
using UnityEngine;

namespace Content.UserInterface
{
    public sealed class StageScreenBackend : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainScreen;

        [SerializeField]
        private GameObject stageScreen;

        public void Awake()
        {

        }

        public void OnPressStageButton(int stageIndex)
        {

        }

        public void OnPressBack()
        {
            stageScreen.SetActive(false);
            mainScreen.SetActive(true);
        }
    }
}