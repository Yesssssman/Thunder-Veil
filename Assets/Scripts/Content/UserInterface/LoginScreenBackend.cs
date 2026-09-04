using Core.Networking;
using Core.Networking.Dto;
using Core.Networking.Util;
using TMPro;
using UnityEngine;

namespace Content.UserInterface
{
    public sealed class LoginScreenBackend : MonoBehaviour
    {
        [SerializeField]
        private GameObject idInputField;

        [SerializeField]
        private GameObject pwInputField;

        [SerializeField]
        private GameObject mainScreen;

        [SerializeField]
        private GameObject loginScreen;

        [SerializeField]
        private GameObject signUpScreen;

        [SerializeField]
        private GameObject warningMessageScreen;

        public async void OnEnterPress()
        {
            TMP_InputField idTextBox = idInputField.GetComponent<TMP_InputField>();
            TMP_InputField pwTextBox = pwInputField.GetComponent<TMP_InputField>();
            string id = idTextBox.text;
            string pw = pwTextBox.text;

            GameUserResponseDTO result = await LoginManager.LogIn(new GameUserRequestDTO
            {
                memberId = id,
                memberPw = pw
            });

            if (ResponseMessageUtil.IsOk(result.responseMessage))
            {
                mainScreen.SetActive(true);
                loginScreen.SetActive(false);
                return;
            }

            // 경고창 활성화
            warningMessageScreen.SetActive(true);
            var waringScreenBackend = warningMessageScreen.GetComponentInChildren<WarningMessageScreenBackend>();

            if (ResponseMessageUtil.IsInvalidCredential(result.responseMessage))
            {
                waringScreenBackend.SetTitleText("Login Failed");
                waringScreenBackend.setMessageText("Invalid ID or Password!");
            }
            else
            {
                waringScreenBackend.SetTitleText("Login Failed");
                waringScreenBackend.setMessageText(result.responseMessage);
            }
        }

        public void OnSkipPress()
        {

        }

        public void OnSignUpPress()
        {
            loginScreen.gameObject.SetActive(false);
            signUpScreen.gameObject.SetActive(true);
        }
    }
}