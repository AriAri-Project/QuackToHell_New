using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 채팅 관련 유틸리티 메서드들을 제공하는 정적 헬퍼 클래스
/// 데이터 변환, 포맷팅 등의 공통 기능 제공
/// </summary>
public static class ChatHelper
{
    /// <summary>
    /// 타임스탬프를 MM:SS 형식으로 포맷
    /// </summary>
    /// <param name="timestamp">변환할 타임스탬프 (초)</param>
    /// <returns>포맷된 타임스탬프 문자열</returns>
    public static string GetFormatTimestamp(float timestamp)
    {
        int minutes = Mathf.FloorToInt(timestamp / 60);
        int seconds = Mathf.FloorToInt(timestamp % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 메시지 데이터를 포맷된 문자열로 변환
    /// </summary>
    /// <param name="messageData">변환할 메시지 데이터</param>
    /// <param name="showTimestamp">타임스탬프 표시 여부</param>
    /// <returns>포맷된 메시지 문자열</returns>
    public static string GetFormattedMessage(ChatMessageData messageData, bool showTimestamp = false)
    {
        if (showTimestamp)
        {
            return $"[{GetFormatTimestamp(messageData.timestamp)}] {messageData.senderName}: {messageData.message}";
        }
        
        return $"{messageData.senderName}: {messageData.message}";
    }
}
