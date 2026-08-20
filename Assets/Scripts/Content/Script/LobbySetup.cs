using Content.UserInterface;
using Core.Networking;
using Core.Networking.Dto;
using Core.Networking.Util;
using UnityEngine;

namespace Content.Script
{
    public class LobbySetup : MonoBehaviour
    {
        [SerializeField]
        private GameObject mainScreen;

        [SerializeField]
        private GameObject loadingScreen;

        [SerializeField]
        private GameObject loginScreen;

        [SerializeField]
        private GameObject warningScreen;

        private void Awake()
        {
#if LOCAL || DEV
            LoginManager.ResetOnline(); // 프로그램 실행 시마다 온라인 여부 초기화
#endif
        }

        private async void Start()
        {
            loadingScreen.SetActive(true);

            GameUserResponseDTO result = await LoginManager.AutoLogIn();

            loadingScreen.SetActive(false);

            if (ResponseMessageUtil.IsOk(result.responseMessage))
            {
                mainScreen.SetActive(true);
            }
            else if (ResponseMessageUtil.IsServerUnreachable(result.responseMessage))
            {
                // 서버 연결 실패시 Offline모드로 플레이
                mainScreen.SetActive(true);
                warningScreen.SetActive(true);
                var waringScreenBackend = warningScreen.GetComponentInChildren<WarningMessageScreenBackend>();

                waringScreenBackend.SetTitleText("Connection Failed");
                waringScreenBackend.setMessageText("Failed to connect to the server! You still can play in offline, but leaderboard system is unavailable.");
            }
            else
            {
                loginScreen.SetActive(true);
            }
        }
    }
}