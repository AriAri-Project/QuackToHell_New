using UnityEngine;

/// <summary>
/// 게임에서 사용하는 모든 레이어를 중앙 관리하는 클래스
/// </summary>
public static class GameLayers
{
    // 플레이어 관련 레이어
    public const string Player = "Player";
    public const string PlayerGhost = "PlayerGhost";
    
    // 환경 관련 레이어
    public const string Ground = "Ground";
    public const string Wall = "Wall";
    public const string ShadowWall = "ShadowWall";
    public const string Interactable = "Interactable";
    
    // UI 관련 레이어
    public const string UI = "UI";
    
    //ClickableWorldObj레이어
    public const string ClickableWorldObj = "ClickableWorldObj";
    
    /// <summary>
    /// 레이어 이름으로 레이어 인덱스 반환
    /// </summary>
    /// <param name="layerName">레이어 이름</param>
    /// <returns>레이어 인덱스</returns>
    public static int GetLayerIndex(string layerName)
    {
        return LayerMask.NameToLayer(layerName);
    }
    
    /// <summary>
    /// 레이어 마스크 생성
    /// </summary>
    /// <param name="layerName">레이어 이름</param>
    /// <returns>레이어 마스크</returns>
    public static LayerMask GetLayerMask(string layerName)
    {
        return 1 << GetLayerIndex(layerName);
    }
}
