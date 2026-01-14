/// <summary>
/// Input System 관련 상수들
/// Action Map과 Action 이름들을 중앙에서 관리
/// 하드코딩된 문자열을 제거하여 유지보수성 향상
/// </summary>
public static class GameInputs
{
    // Action Map 이름들
    public static class ActionMaps
    {
        public const string Player = "Player";
        public const string UI = "UI";
        public const string Farmer = "Farmer";
        public const string Animal = "Animal";
        public const string Ghost = "Ghost";
    }
    
    // Action 이름들
    public static class Actions
    {
        public const string Move = "Move";
        public const string Interact = "Interact";
        public const string Savotage = "Savotage";
        public const string Vent = "Vent";      
        public const string Report = "Report";
        public const string Kill = "Kill";
        public const string Close = "Close";
    }
}
