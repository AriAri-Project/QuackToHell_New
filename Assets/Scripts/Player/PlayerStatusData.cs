using System;
using Unity.Netcode;
using UnityEngine;

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
    Walk
}

public enum PlayerJob
{
    None,
}

[System.Serializable]
public struct PlayerStatusData : INetworkSerializable
{
    public const int MaxCredibility = 100;
    public const int MaxSpellpower = 100;

    public string Nickname;
    public PlayerJob Job;
    public int Credibility;
    public int Spellpower;
    public int Gold;
    public float MoveSpeed;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (Nickname == null)
            Nickname = "";
        serializer.SerializeValue(ref Nickname);
        serializer.SerializeValue(ref Job);
        serializer.SerializeValue(ref Credibility);
        serializer.SerializeValue(ref Spellpower);
        serializer.SerializeValue(ref Gold);
        serializer.SerializeValue(ref MoveSpeed);
    }
}

[System.Serializable]
public struct PlayerStateData : INetworkSerializable
{
    public PlayerLivingState AliveState;
    public PlayerAnimationState AnimationState;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref AliveState);
        serializer.SerializeValue(ref AnimationState);
    }
}