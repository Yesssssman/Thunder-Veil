using Core.Networking.Messages;

namespace Core.Networking.Messages
{
    public static class RefreshTokenMessages
    {
        public const string OK = "OK";
        public const string MANDATORY_PARAM_MISSING = "MANDATORY_PARAM_MISSING";
        public const string INVALID_PARAMETER = "INVALID_PARAMETER";
        public const string UNSUPPORTED_MEMBER_TYPE = "UNSUPPORTED_MEMBER_TYPE";
        public const string UNSUPPORTED_ACTION_TYPE = "UNSUPPORTED_ACTION_TYPE";
        public const string UNSUPPORTED_SNS_PROVIDER = "UNSUPPORTED_SNS_PROVIDER";
        public const string INVALID_JWT_TOKEN = "INVALID_JWT_TOKEN";
        public const string EXPIRED_JWT_TOKEN = "EXPIRED_JWT_TOKEN";
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string USER_ACCESS_DENIED = "USER_ACCESS_DENIED";
        public const string GAME_CODE_NOT_FOUND = "GAME_CODE_NOT_FOUND";
        public const string GAME_ACCOUNT_NOT_FOUND = "GAME_ACCOUNT_NOT_FOUND";
        public const string ACCOUNT_ALREADY_EXISTS = "ACCOUNT_ALREADY_EXISTS";
        public const string EXCEPTION_OCCURRED = "EXCEPTION_OCCURRED";
        public const string SERVICE_UNDER_MAINTENANCE = "SERVICE_UNDER_MAINTENANCE";
        public const string SERVICE_BLOCKED = "SERVICE_BLOCKED";
    }

    public static class GameStartMessages
    {
        public const string SUCCESS = "SUCCESS";
        public const string MANDATORY_PARAM_MISSING = "MANDATORY_PARAM_MISSING";
        public const string INVALID_ACCESS_TOKEN = "INVALID_ACCESS_TOKEN";
        public const string ACCESS_TOKEN_EXPIRED = "ACCESS_TOKEN_EXPIRED";
        public const string GAME_NOT_FOUND = "GAME_NOT_FOUND";
        public const string GAME_NOT_AVAILABLE = "GAME_NOT_AVAILABLE";
        public const string GAME_ACCOUNT_NOT_FOUND = "GAME_ACCOUNT_NOT_FOUND";
        public const string GAME_ACCOUNT_NOT_ACTIVE = "GAME_ACCOUNT_NOT_ACTIVE";
        public const string USER_NOT_ACTIVE = "USER_NOT_ACTIVE";
        public const string EXCEPTION_OCCURRED = "EXCEPTION_OCCURRED";
    }

    public static class CommonMessages
    {
        public const string ALREADY_ONLINE = "ALREADY_ONLINE";
        public const string NO_AUTH_INFO_IN_LOCAL = "NO_AUTH_INFO_IN_LOCAL";
        public const string CONNECTION_ERROR = "CONNECTION_ERROR"; // Server downed, or offline device.
    }
}

namespace Core.Networking.Util
{
    public static class ResponseMessageUtil
    {
        /// <summary>
        /// 서버 응답이 긍정적 응답(e.g. 로그인 성공, 토큰 재발급 성공 등)인지를 반환함
        /// </summary>
        public static bool IsOk(string msg)
        {
            return msg is RefreshTokenMessages.OK or GameStartMessages.SUCCESS;
        }

        /// <summary>
        /// 사용자 정보 불일치에 의한 오류(아이디, 비밀번호 오입력)인지를 반환함
        /// </summary>
        public static bool IsInvalidCredential(string msg)
        {
            return msg is RefreshTokenMessages.INVALID_CREDENTIALS;
        }

        /// <summary>
        /// 서버 연결에 실패했는지 여부 반환 — 서버가 다운됐거나 디바이스가 오프라인
        /// </summary>
        public static bool IsServerUnreachable(string msg)
        {
            return msg is CommonMessages.CONNECTION_ERROR;
        }

        /// <summary>
        /// 서버 연결에 실패했는지 여부 반환 — 서버가 다운됐거나 디바이스가 오프라인
        /// </summary>
        public static bool IsAccessTokenExpired(string msg)
        {
            return msg is GameStartMessages.INVALID_ACCESS_TOKEN or GameStartMessages.ACCESS_TOKEN_EXPIRED;
        }
    }
}