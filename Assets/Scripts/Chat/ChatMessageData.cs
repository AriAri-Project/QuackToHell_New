using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 상태 (채팅 권한 관리용)
/// </summary>
public enum PlayerChatState
{
    Alive,      // 생존자
    Dead,       // 사망자
    Observer,   // 관전자
}

/// <summary>
/// 채팅 메시지 데이터 구조체
/// NetworkSerializable을 구현하여 네트워크 동기화 가능
/// </summary>
[System.Serializable]
public struct ChatMessageData : INetworkSerializable
{
    public ulong senderId;              // 보낸 사람 ID
    public string senderName;           // 보낸 사람 이름
    public string message;              // 메시지 내용
    public PlayerChatState senderState; // 보낸 사람의 상태 (필요시 사용)
    public float timestamp;             // 메시지 전송 시간

    public ChatMessageData(ulong senderId, string senderName, string message, 
                      PlayerChatState senderState = PlayerChatState.Alive)
    {
        this.senderId = senderId;
        this.senderName = senderName;
        this.message = message;
        this.senderState = senderState;
        this.timestamp = Time.time;
    }

    /// <summary>
    /// 네트워크 직렬화 구현
    /// </summary>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref senderId);
        serializer.SerializeValue(ref senderName);
        serializer.SerializeValue(ref message);
        serializer.SerializeValue(ref senderState);
        serializer.SerializeValue(ref timestamp);
    }
}

