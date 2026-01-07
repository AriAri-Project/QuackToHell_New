namespace Data
{
    public struct LobbyData
    {
        public string lobbyName;
        public string lobbyCode;
        public bool isPrivateRoom;
        public int maxPlayerNum;
        public int FarmerNum;
        public int savotageCooltime;
        public int killCooltime;
        public bool isShowKillerInfo;
        public float innerEyesightValue;
        public float outerEyesightValue;

        public static LobbyData CreateDefault()
        {
            return new LobbyData
            {
                maxPlayerNum = GameConstants.Lobby.Initials.MaxPlayers,
                FarmerNum = GameConstants.Lobby.Initials.FarmerNum,
                savotageCooltime = GameConstants.Lobby.Initials.SavotageCooltime,
                killCooltime = GameConstants.Lobby.Initials.KillCooltime,
                isShowKillerInfo = GameConstants.Lobby.Initials.IsShowKillerInfo,
                innerEyesightValue = GameConstants.Lobby.Initials.InnerEyesight,
                outerEyesightValue = GameConstants.Lobby.Initials.OuterEyesight
            };
        }
    }
}