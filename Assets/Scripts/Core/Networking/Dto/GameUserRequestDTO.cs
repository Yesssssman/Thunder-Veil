using System;

namespace Core.Networking.Dto
{
    /// <summary>
    /// JW 게임 서버로 요청 보낼때의 DTO
    /// <p/>
    /// Mandatory로 표기한 값들은 반드시 존재해야 하는 컬럼
    /// <p/>
    /// Optional로 표기한 값들은 상황에 따라 존재해야 하거나, 필수적이지 않은 컬럼들
    /// <p/>
    /// JsonUtility로 직렬화되므로 필드명이 곧 JSON 키가 됨 — 서버 스펙과 동일하게 유지할 것
    /// </summary>
    [Serializable]
    public struct GameUserRequestDTO
    {
        /// <summary>
        /// JW 서버에서 발급한 게임 코드 (Mandatory)
        /// </summary>
        public string gameCode;

        /// <summary>
        /// 멤버 유형: 0(게임을 통한 순수가입자) / 1(SNS 로그인) (Mandatory)
        /// </summary>
        public int memberType;

        /// <summary>
        /// 액션 유형: JOIN(회원가입) / LOGIN(로그인) (Mandatory)
        /// </summary>
        public string actionType;

        /// <summary>
        /// OAuth 인증 제공자 ID (Optional)
        /// </summary>
        public string snsProviderId;

        /// <summary>
        /// OAuth 인증 제공자 이름 (Optional)
        /// </summary>
        public string snsProvider;

        /// <summary>
        /// 멤버 ID (Mandatory)
        /// </summary>
        public string memberId;

        /// <summary>
        /// 멤버 비밀번호 (Mandatory, 6글자 이상)
        /// </summary>
        public string memberPw;

        /// <summary>
        /// 멤버 이름 (실명) (Mandatory)
        /// </summary>
        public string memberName;

        /// <summary>
        /// 멤버 닉네임 (Optional)
        /// </summary>
        public string nickName;

        /// <summary>
        /// 이메일 주소 (Optional)
        /// </summary>
        public string email;

        /// <summary>
        /// 연락처1 (Optional)
        /// </summary>
        public string mobilePhone1;

        /// <summary>
        /// 연락처2 (Optional)
        /// </summary>
        public string mobilePhone2;

        /// <summary>
        /// 연락처3 (Optional)
        /// </summary>
        public string mobilePhone3;

        /// <summary>
        /// 본인인증 CI (개발 단계에선 Optional)
        /// </summary>
        public string ci;
    }
}