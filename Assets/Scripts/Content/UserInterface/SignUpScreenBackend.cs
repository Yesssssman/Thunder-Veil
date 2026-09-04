using Core.Networking;
using Core.Networking.Dto;
using Core.Networking.Util;
using TMPro;
using UnityEngine;

namespace Content.UserInterface
{
    public sealed class SignUpScreenBackend : MonoBehaviour
    {
        // ID 입력 필드
        [SerializeField]
        private GameObject idInputField;

        // 패스워드 입력 필드
        [SerializeField]
        private GameObject pwInputField;

        // 패스워드 확인 입력 필드
        [SerializeField]
        private GameObject pwValidationInputField;

        // 아이디 경고 메세지: 아이디 생성 규칙을 만족하지 않습니다
        [SerializeField]
        private GameObject idWarningMessage;

        // 패스워드 경고 메세지: 비밀번호 생성 규칙을 만족하지 않습니다
        [SerializeField]
        private GameObject pwWarnMessage;

        // 패스워드 확인 경고 메세지: 비밀번호와 비밀번호 확인이 일치하지 않습니다
        [SerializeField]
        private GameObject pwValidationWarnMessage;

        [SerializeField]
        private GameObject loginScreen;

        [SerializeField]
        private GameObject signUpScreen;

        [SerializeField]
        private GameObject loadingScreen;

        [SerializeField]
        private GameObject messageScreen;

        public void OnEnterPress()
        {
            TMP_InputField idTextBox = idInputField.GetComponent<TMP_InputField>();
            TMP_InputField pwTextBox = pwInputField.GetComponent<TMP_InputField>();
            TMP_InputField pwValidationTextBox = pwValidationInputField.GetComponent<TMP_InputField>();
            string id = idTextBox.text;
            string pw = pwTextBox.text;
            string pwCheck = pwValidationTextBox.text;

            // 패스워드, 패스워드 확인에 입력된 값이 다를 때
            if (!string.Equals(pw, pwCheck))
            {
                pwValidationWarnMessage.SetActive(true);
                return;
            }

            loadingScreen.SetActive(true);

            Awaitable<GameUserResponseDTO> result = LoginManager.SignUp(
                new GameUserRequestDTO
                {
                    memberType = 0,
                    memberId = id,
                    memberPw = pw,
                    memberName = "테스트 유저",
                }
            );

            Awaitable<GameUserResponseDTO>.Awaiter awaiter = result.GetAwaiter();

            awaiter.OnCompleted(() => {
                loadingScreen.SetActive(false);
                messageScreen.SetActive(true);
                var waringScreenBackend = messageScreen.GetComponentInChildren<WarningMessageScreenBackend>();
                GameUserResponseDTO response = awaiter.GetResult();

                if (ResponseMessageUtil.IsOk(response.responseMessage))
                {
                    waringScreenBackend.SetTitleText("Sign Up Succeeded");
                    waringScreenBackend.setMessageText("Log in with your ID and Password");
                }
                else
                {
                    waringScreenBackend.SetTitleText("Sign Up Failed");
                    waringScreenBackend.setMessageText(response.responseMessage);
                }
            });
        }

        public void OnCancelPress()
        {
            // 화면 전환: 로그인 창으로
            loginScreen.gameObject.SetActive(true);
            signUpScreen.gameObject.SetActive(false);
        }
    }
}