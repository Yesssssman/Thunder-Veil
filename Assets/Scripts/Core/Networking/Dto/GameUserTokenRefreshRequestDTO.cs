using System;

namespace Core.Networking.Dto
{
    [Serializable]
    public struct GameUserTokenRefreshRequestDTO
    {
        public string accessToken;

        public string refreshToken;
    }
}