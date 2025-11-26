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
            //참고문서: https://ariari-ewha.atlassian.net/wiki/spaces/~712020e9509d1767994750b2ca1d1e408ddb2d/pages/107773956/UI+order+in+layer
            public const int WorldSpace = -10;
            public const int Background = 0;
            public const int HUD = 10;
            //인벤, 설정창, ..
            public const int MenusAndPanels = 20;
            public const int Popup=30;
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

  
        
    
}
