using System.IO;
using Core.Networking.Dto;
using Core.Networking.Messages;
using Core.Networking.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Networking
{
    /// <summary>
    /// JW 서버 로그인 라이브러리
    /// </summary>
    public static class LoginManager
    {
        // PLACEHOLDER: 통합 게임 백엔드서버에서 사용되는 게임의 식별자
        private const string GAME_CODE = "13CD88C90C13FEB2B845994353574EFF";

#if LOCAL
        private const string AUTH_SERVER = "http://localhost:5050";
#elif DEV
        private const string AUTH_SERVER = "https://192.168.0.1"; // PLACEHOLDER: 개발 서버 주소
#elif PROD
        private const string AUTH_SERVER = "https://jwgameserver.com"; // PLACEHOLDER: 운영 서버 주소
#else
        private const string AUTH_SERVER = "http://localhost:5050";
#endif

        // PLACEHOLDER: 인증 정보를 저장할 파일명
        private const string AUTH_FILE = "thunder_veil_auth.json";

        // HTTP통신 데이터 타입 — JSON
        private const string CONTENT_TYPE_JSON = "application/json";

        // 엑세스 토큰 — 메모리에서만 존재
        private static string sAccessToken;

        // 리프레쉬 토큰 — 디스크에 저장됨
        private static string sRefreshToken;

        // `sAccessToken`이 유효한지 보장함. ID/PW를 입력하여 로그인 하였거나 자동 로그인
        // 기능으로 로그인에 성공하였을 때 true.
        //
        // Warning: 이 값이 true일때 토큰이 서버에서도 유효한지 보장하지 않음. 엑세스 토큰은
        //          서버에서 1시간 단위로 초기화 되기 때문에 모든 트랜잭션에 "Access 토큰으로
        //          통신 시도 -> Refresh 토큰으로 통신 재시도"하는 프로세스가 존재해야 함.
        //
        private static bool sIsOnline;

#if LOCAL || DEV
        public static void ResetOnline() {
            sIsOnline = false;
        }
#endif

        // 디스크에 영구히 보존되는 데이터 파일 위치
        // Application.persistentDataPath: OS에서 보장하는 영구 데이터들의 위치를 담고 있음
        private static string sPersistentDataPath => Path.Combine(Application.persistentDataPath, AUTH_FILE);

        private static void SaveAuth()
        {
            string json = JsonUtility.ToJson(
                new GameUserTokenRefreshRequestDTO { accessToken = sAccessToken, refreshToken = sRefreshToken },
                true
            );
            File.WriteAllText(sPersistentDataPath, json);
        }

        private static void LoadAuth()
        {
            if (!File.Exists(sPersistentDataPath))
            {
                return;
            }

            string json = File.ReadAllText(sPersistentDataPath);
            GameUserTokenRefreshRequestDTO gameUserTokenRefreshRequestDto = JsonUtility.FromJson<GameUserTokenRefreshRequestDTO>(json);

            sAccessToken = gameUserTokenRefreshRequestDto.accessToken;
            sRefreshToken = gameUserTokenRefreshRequestDto.refreshToken;
        }

        private static GameUserResponseDTO DeserializeJsonResponse(UnityWebRequest req)
        {
            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                return new GameUserResponseDTO { responseMessage = CommonMessages.CONNECTION_ERROR };
            }

            string responseJson = req.downloadHandler.text;

            return JsonUtility.FromJson<GameUserResponseDTO>(responseJson);
        }

        /// <summary>
        /// 사용자 회원가입
        /// <p/>
        /// 필수 파라미터: memberType, memberId, memberPw (6자리이상), memberName
        /// 조건부 필수 파라미터: snsProviderId, snsProvider (memberType == 1인 경우)
        /// </summary>
        public static async Awaitable<GameUserResponseDTO> SignUp(GameUserRequestDTO request)
        {
            const string gameUserRequestEndpoint = "/api/account/gameuser";

            request.gameCode = GAME_CODE;
            request.actionType = "JOIN";
            string postData = JsonUtility.ToJson(request);

            // 웹 요청 전송 — using-IDisposable 실패시 자동으로 Close
            using (UnityWebRequest req = UnityWebRequest.Post(AUTH_SERVER + gameUserRequestEndpoint, postData, CONTENT_TYPE_JSON))
            {
                // 응답 대기 — 비동기 처리
                await req.SendWebRequest();
                GameUserResponseDTO response = DeserializeJsonResponse(req);

                // 성공 — Access/Refresh 토큰 저장. 앞으로의 서버 통신은 Access토큰을 통해 이루어짐.
                if (ResponseMessageUtil.IsOk(response.responseMessage))
                {
                    sAccessToken = response.accessToken;
                    sRefreshToken = response.refreshToken;
                    sIsOnline = true;
                    SaveAuth();
                }

                return response;
            }
        }

        /// <summary>
        /// 사용자 로그인
        /// <p/>
        /// 필수 파라미터: memberId, memberPw
        /// </summary>
        public static async Awaitable<GameUserResponseDTO> LogIn(GameUserRequestDTO request)
        {
            // 이미 로그인한 경우
            if (sIsOnline)
            {
                return new GameUserResponseDTO { responseMessage = CommonMessages.ALREADY_ONLINE };
            }

            const string gameUserRequestEndpoint = "/api/account/gameuser";

            request.gameCode = GAME_CODE;
            request.actionType = "LOGIN";
            string postData = JsonUtility.ToJson(request);

            using (UnityWebRequest req = UnityWebRequest.Post(AUTH_SERVER + gameUserRequestEndpoint, postData, CONTENT_TYPE_JSON))
            {
                await req.SendWebRequest();
                GameUserResponseDTO response = DeserializeJsonResponse(req);

                if (ResponseMessageUtil.IsOk(response.responseMessage))
                {
                    sAccessToken = response.accessToken;
                    sRefreshToken = response.refreshToken;
                    sIsOnline = true;
                    SaveAuth();
                }

                return response;
            }
        }

        /// <summary>
        /// 저장되어 있는 Access토큰과 Refresh토큰으로 로그인 시도
        /// <p/>
        /// 로그인 성공 시 별도로 ID/PW입력받을 필요 없음
        /// </summary>
        public static async Awaitable<GameUserResponseDTO> AutoLogIn()
        {
            // 이미 로그인한 경우
            if (sIsOnline)
            {
                return new GameUserResponseDTO { responseMessage = CommonMessages.ALREADY_ONLINE };
            }

            LoadAuth();

            if (string.IsNullOrEmpty(sAccessToken) && string.IsNullOrEmpty(sRefreshToken))
            {
                // 저장된 토큰 없으면 로그인 실패
                return new GameUserResponseDTO { responseMessage = CommonMessages.NO_AUTH_INFO_IN_LOCAL };
            }

            GameUserResponseDTO dto = await GameStart();

            if (ResponseMessageUtil.IsOk(dto.responseMessage))
            {
                // 성공 — Access토큰과 Refresh토큰 모두 유효함
                sIsOnline = true;

                Debug.Log("Logged in by Access Token.");

                return dto;
            }

            if (dto.responseMessage == GameStartMessages.ACCESS_TOKEN_EXPIRED
                     || dto.responseMessage == GameStartMessages.INVALID_ACCESS_TOKEN)
            {
                // Access 토큰 만료 — Refresh 토큰으로 재발급 시도
                GameUserResponseDTO response = await RefreshAccessToken();

                if (response.responseMessage == RefreshTokenMessages.OK)
                {
                    Debug.Log("Logged in by Refresh Token.");

                    sAccessToken = response.accessToken;
                    sRefreshToken = response.refreshToken;

                    // 재발급된 토큰으로 재시도
                    GameUserResponseDTO retryDto = await GameStart();

                    if (ResponseMessageUtil.IsOk(retryDto.responseMessage))
                    {
                        sIsOnline = true;
                    }

                    return retryDto;
                }

                return response;
            }

            // 모든 나머지 케이스 — 실패
            return dto;
        }

        // `GameStart` 호출은 `AutoLogIn`에서 자동으로 처리
        private static async Awaitable<GameUserResponseDTO> GameStart()
        {
            // 이미 로그인한 경우
            if (sIsOnline)
            {
                return new GameUserResponseDTO { responseMessage = CommonMessages.ALREADY_ONLINE };
            }

            const string gameUserRequestEndpoint = "/api/game/gamestart";

            using (UnityWebRequest req = UnityWebRequest.Post(AUTH_SERVER + gameUserRequestEndpoint, "", CONTENT_TYPE_JSON))
            {
                req.SetRequestHeader("Authorization", "Bearer " + sAccessToken);
                await req.SendWebRequest();
                GameUserResponseDTO response = DeserializeJsonResponse(req);
                return response;
            }
        }

        /// <summary>
        /// 세션에 저장되어 있는 Refresh토큰으로 Access토큰 재발급
        /// </summary>
        public static async Awaitable<GameUserResponseDTO> RefreshAccessToken()
        {
            const string accessTokenRefreshEndpoint = "/api/account/gameusertokenrefresh";

            if (string.IsNullOrEmpty(sRefreshToken))
            {
                // Refresh토큰이 없는 경우, `LogIn`으로 ID, PW 입력하여 로그인하여야 함
                return new GameUserResponseDTO { responseMessage = "NO_REFRESH_TOKEN" };
            }

            string postData = JsonUtility.ToJson(new GameUserTokenRefreshRequestDTO { refreshToken =  sRefreshToken });

            using (UnityWebRequest req = UnityWebRequest.Post(AUTH_SERVER + accessTokenRefreshEndpoint, postData, CONTENT_TYPE_JSON))
            {
                await req.SendWebRequest();
                GameUserResponseDTO response = DeserializeJsonResponse(req);
                return response;
            }
        }
    }
}