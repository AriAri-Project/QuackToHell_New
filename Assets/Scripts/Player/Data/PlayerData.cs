using Unity.Netcode;
using UnityEngine.Serialization;

/// <summary>  
/// 플레이어 데이터 구조  
/// </summary>  
public enum PlayerLivingState
{
    Alive,
    Dead
}

public enum PlayerAnimationState
{
    Idle,
    Walk,
    VentEnter
}

public enum PlayerJob
{
    None,
    Farmer,     // 농장주
    Animal,     // 동물
    Ghost       // 유령 (사망한 플레이어)
}

public enum EPlayerColorIndex
{
    Default,
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple
}

[System.Serializable]
public struct PlayerAppearanceData : INetworkSerializable
{
    public int ColorIndex;
    public float AlphaValue;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ColorIndex);
        serializer.SerializeValue(ref AlphaValue);
    }
}


[System.Serializable]
public struct PlayerStatusData : INetworkSerializable
{
    public const int MaxCredibility = 100;
    public const int MaxSpellpower = 100;

    public string nickname;
    public PlayerJob job;
    public int credibility;
    public int spellpower;
    public int gold;
    public float moveSpeed;
    private bool isReady;

    // Properties for compatibility with existing code
    public string Nickname 
    { 
        get => nickname; 
        set => nickname = value; 
    }
    public float MoveSpeed 
    { 
        get => moveSpeed; 
        set => moveSpeed = value; 
    }
    public bool IsReady{
        get => isReady;
        set => isReady = value;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (nickname == null)
            nickname = "";
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref job);
        serializer.SerializeValue(ref credibility);
        serializer.SerializeValue(ref spellpower);
        serializer.SerializeValue(ref gold);
        serializer.SerializeValue(ref moveSpeed);
        serializer.SerializeValue(ref isReady); 
    }
}

[System.Serializable]
public struct PlayerStateData : INetworkSerializable
{
    [FormerlySerializedAs("AliveState")] public PlayerLivingState aliveState;
    [FormerlySerializedAs("AnimationState")] public PlayerAnimationState animationState;
    
    // Properties for compatibility with existing code
    public PlayerLivingState AliveState 
    { 
        get => aliveState; 
        set => aliveState = value; 
    }
    public PlayerAnimationState AnimationState 
    { 
        get => animationState; 
        set => animationState = value; 
    }
    
    public bool IsDead => aliveState == PlayerLivingState.Dead;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref aliveState);
        serializer.SerializeValue(ref animationState);
    }
}