using System;

namespace Core.Networking.Dto
{
    /// <summary>
    /// JW 게임 서버에서 응답을 받을 때의 DTO
    /// <p/>
    /// JsonUtility로 역직렬화되므로 필드명이 곧 JSON 키가 됨 — 서버 스펙과 동일하게 유지할 것
    /// </summary>
    [Serializable]
    public class GameUserResponseDTO
    {
        /// <summary>
        /// 응답 메세지
        /// </summary>
        public string responseMessage;

        /// <summary>
        /// 엑세스 토큰
        /// </summary>
        public string accessToken;

        /// <summary>
        /// 리프레쉬 토큰
        /// </summary>
        public string refreshToken;

        /// <summary>
        /// 토큰 타입 — BEARER
        /// </summary>
        public string tokenType;

        /// <summary>
        /// Access Token 만료 시간 — 1시간부터 카운트
        /// </summary>
        public long expiresIn;

        /// <summary>
        /// Refresh Token 만료 시간 — 1달부터 카운트
        /// </summary>
        public long refreshExpiresIn;

        /// <summary>
        /// 신규 계정 여부
        /// </summary>
        public bool isNewAccount;

        /// <summary>
        /// 유저 고유 ID
        /// </summary>
        public string userId;

        /// <summary>
        /// 게임 계정 해시코드
        /// </summary>
        public string gameAccountHashcode;
    }
}