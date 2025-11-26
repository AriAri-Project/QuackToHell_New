using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 전역 채팅 모델
/// </summary>
public class ChatModel : NetworkBehaviour
{
    // note cba0898: const나 SO(Scriptable Object), json(시트쓸때) 등으로 별도 분리 추천: 값들이 다 분리되어있으니까 const클래스라든지.. 하나로 모으기. 컴포넌트마다 달라지는거면 인스펙터에서 조절 / 모두 동일해야하면 const로 빼는식으로
    [Header("Chat Settings")]
    [SerializeField] private int maxMessageLength = 500; // 최대 메시지 길이
    [SerializeField] private int maxMessages = 300; // 최대 메시지 수
    
    // 모든 메시지 저장소
    private List<ChatMessageData> allMessages = new List<ChatMessageData>();
    public List<ChatMessageData> AllMessages => allMessages;
    
    // 이벤트 (싱글톤 일관성을 위해 instance event 사용)
    public event Action<ChatMessageData> OnMessageReceived;

    #region Singleton
    public static ChatModel Instance => SingletonHelper<ChatModel>.Instance;
    private void Awake()
    {
        SingletonHelper<ChatModel>.InitializeSingleton(this);
    }
    #endregion

    /// <summary>
    /// 메시지 전송 (클라이언트에서 호출)
    /// </summary>
    public void SendMessage(string message, PlayerChatState playerState = PlayerChatState.Alive)
    {            
        if (!IsClient)
            return;
        if (!CanSendMessage(message))
            return;
        // 현재 플레이어 정보 가져오기 (임시)
        ulong playerId = NetworkManager.Singleton.LocalClientId;
        // TODO 플레이어 이름 가져오기 (Helper 등)
        string playerName = $"Player_{playerId}";
        // 하나의 구조체로 서버에 전송
        SendMessageServerRpc(playerId, playerName, message, playerState);
    }

    private bool CanSendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning($"[ChatModel] Empty message.");
            return false;
        }
        if (message.Length > maxMessageLength)
        {
            Debug.LogWarning($"[ChatModel] Message is too long.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 서버에서 메시지 처리
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(ulong playerId, string playerName, string message, PlayerChatState playerState, ServerRpcParams rpcParams = default)
    {
        ChatMessageData messageData = new ChatMessageData(playerId, playerName, message, playerState);
        // 서버에 저장
        AddMessageToStorage(messageData);
        Debug.Log($"Server: Added message to storage: {messageData.message}");
        
        // 모든 클라이언트에 전송
        ReceiveMessageClientRpc(messageData);
    }
    
    /// <summary>
    /// 클라이언트에서 메시지 수신
    /// </summary>
    [ClientRpc]
    private void ReceiveMessageClientRpc(ChatMessageData messageData)
    {
        // 클라이언트에도 저장
        if (!IsServer) // 서버는 이미 저장했으므로 중복 방지
        {
            AddMessageToStorage(messageData);
            Debug.Log($"Client: Added message to storage: {messageData.message}");
        }
    
        // 다른 Presenter들이 채팅 메시지를 처리할 수 있도록 이벤트 발생
        OnMessageReceived?.Invoke(messageData);
    }

    /// <summary>
    /// 메시지를 저장소에 추가
    /// </summary>
    private void AddMessageToStorage(ChatMessageData messageData)
    {
        allMessages.Add(messageData);
        
        // 최대 메시지 수 제한
        if (allMessages.Count > maxMessages)
        {
            Debug.Log($"Client: Removed oldest message from storage: {allMessages[0].message}");
            // TODO 추후 기획에 따라 오래된 메시지 정리 로직 수정
            allMessages.RemoveAt(0);
        }
    }

    /// <summary>
    /// 최근 메시지들 가져오기
    /// </summary>
    public List<ChatMessageData> GetRecentMessages(int count = 50)
    {
        int startIndex = Mathf.Max(0, allMessages.Count - count);
        return allMessages.GetRange(startIndex, allMessages.Count - startIndex);
    }

    /// <summary>
    /// 모든 메시지 정리
    /// </summary>
    public void ClearAllMessages()
    {
        // 서버에서 한 번만 ClientRpc 호출하여 모든 클라이언트에서 메시지 정리
        if (IsServer)
        {
            ClearAllMessagesClientRpc();
        }
    }

    [ClientRpc]
    private void ClearAllMessagesClientRpc()
    {
        allMessages.Clear();
    }
}

