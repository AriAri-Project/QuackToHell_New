/// <summary>
/// 게임에서 사용하는 모든 상수를 중앙 관리하는 클래스
/// </summary>
public static class GameConstants
{
    // 플레이어 관련 상수
    public static class Player
    {
        public const float GhostSpeedMultiplier = 1.5f;
        public const float GhostTransparency = 0.5f;
        public const int DefaultGold = 100;
        public const float DefaultMoveSpeed = 10f;
    }
    
    // UI 관련 상수
    public static class UI
    {
        
        public static class SortingOrder{
            //참고문서: https://ariari-ewha.atlassian.net/wiki/spaces/~712020e9509d1767994750b2ca1d1e408ddb2d/pages/110690307/Sorting+Layer+order+in+layer
            
            //[인게임 UI] 닉네임, 말풍선, 머리 위 상호작용 아이콘
            public const int WorldSpace = -10;
            //[UI 배경] 전체 화면 UI의 뒷배경 (검은 음영 등)
            public const int Background = 0;
            //[HUD] 조이스틱, 스킬 버튼, 미니맵 (항상 떠있는 UI)
            public const int HUD = 10;
            //[패널/메뉴] 인벤토리, 설정 창, 미니게임 창 (HUD를 덮음)
            public const int MenusAndPanels = 20;
            //[팝업] 아이템 획득 알림, 퀘스트 완료 메시지
            public const int Popup=30;
            //[시스템] 로딩 화면, 에러 메시지, 최상위 페이드 효과
            public const int System=100;
        }
    }

    public static class Animation
    {
        public const string VentEnter = "Player_Vent";
    }
    
    // 카드 관련 상수
    public static class Card
    {
        public const float InventoryCardWidth = 200f;
        public const float InventoryCardHeight = 300f;
        public const float SaleCardWidth = 200f;
        public const float SaleCardHeight = 350f;
        public const int maxCardCount = 20;
        public const ulong NOT_DISPLAYING_CLIENT_ID = 9999; //아무도 카드를 진열하고있지 않음을 나타내는 값
    }
    
    // 네트워크 관련 상수
    public static class Network
    {
        public const int MinPlayersToStart = 2;
    }

    public static class Lobby
    {
        public static class Initials
        {
            public const int MaxPlayers = 6;
            public const int FarmerNum = 1;
            public const int SavotageCooltime = 20;
            public const int KillCooltime = 20;
            public const bool IsShowKillerInfo = true;
            public const int InnerEyesight = 29;
            public const int OuterEyesight = 30;
        }

        public static class Max
        {
            public const int MaxPlayers = 16;
        }
    }
        
    
}
